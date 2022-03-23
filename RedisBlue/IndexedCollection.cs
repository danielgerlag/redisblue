using RedisBlue.Interfaces;
using RedisBlue.Models;
using RedisBlue.Services;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisBlue
{
    public partial class IndexedCollection
    {
        private readonly string _collectionName;
        private const string ETagField = "ETag";
        private const string DataField = "Data";
        private const int BatchSize = 100;
        
        private readonly IDatabase _redis;
        private readonly IIndexer _indexer;
        private readonly IKeyResolver _keyResolver;
        private readonly ConcurrentDictionary<Type, TypeCacheInfo> _typeCache;
        private readonly IResolverProvider _resolverProvider;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        internal IndexedCollection(IDatabase database, string collectionName, IIndexer indexer, IKeyResolver keyResolver, IResolverProvider resolverProvider, ConcurrentDictionary<Type, TypeCacheInfo> typeCache)
        {
            _redis = database;
            _collectionName = collectionName;
            _indexer = indexer;
            _keyResolver = keyResolver;
            _resolverProvider = resolverProvider;
            _typeCache = typeCache;
            _jsonSerializerOptions = new JsonSerializerOptions();
            _jsonSerializerOptions.Converters.Add(new ObjectConverter());
        }

        public async Task CreateItem<T>(T item)
        {
            var typeInfo = _typeCache.GetOrAdd(typeof(T), t => new TypeCacheInfo(t));

            var partitionKey = Convert.ToString(typeInfo.PartitionKeyGetMethod.Invoke(item, null));
            var itemKey = Convert.ToString(typeInfo.KeyGetMethod.Invoke(item, null));
            var indexKey = _keyResolver.GetEntityKey(_collectionName, partitionKey, itemKey);

            var txn = _redis.CreateTransaction();
            txn.AddCondition(Condition.KeyNotExists(indexKey));
            _ = WriteItem(txn, item, partitionKey, itemKey);

            if (!await txn.ExecuteAsync())
                throw new ConflictException();
        }

        public async Task ReplaceItem<T>(T item)
        {
            var typeInfo = _typeCache.GetOrAdd(typeof(T), t => new TypeCacheInfo(t));

            var partitionKey = Convert.ToString(typeInfo.PartitionKeyGetMethod.Invoke(item, null));
            var itemKey = Convert.ToString(typeInfo.KeyGetMethod.Invoke(item, null));
            var indexKey = _keyResolver.GetEntityKey(_collectionName, partitionKey, itemKey);

            var (raw, etag) = await ReadWithETag(indexKey);
            if (raw.IsEmpty)
                throw new NotFoundException();

            var txn = _redis.CreateTransaction();
            txn.AddCondition(Condition.HashEqual(indexKey, ETagField, etag));
                        
            var oldItem = JsonSerializer.Deserialize<T>(raw.Span, _jsonSerializerOptions);

            _ = _indexer.RemoveIndexes(txn, _collectionName, partitionKey, "$index", itemKey, oldItem, 0);
            _ = WriteItem(txn, item, partitionKey, itemKey);

            if (!await txn.ExecuteAsync())
                throw new ConflictException();
        }

        public async Task DeleteItem<T>(string partitionKey, string itemKey)
        {
            var typeInfo = _typeCache.GetOrAdd(typeof(T), t => new TypeCacheInfo(t));
            var indexKey = _keyResolver.GetEntityKey(_collectionName, partitionKey, itemKey);

            var (raw, etag) = await ReadWithETag(indexKey);
            if (raw.IsEmpty)
                throw new NotFoundException();

            var txn = _redis.CreateTransaction();
            txn.AddCondition(Condition.HashEqual(indexKey, ETagField, etag));

            var oldItem = JsonSerializer.Deserialize<T>(raw.Span, _jsonSerializerOptions);

            _ = _indexer.RemoveIndexes(txn, _collectionName, partitionKey, "$index", itemKey, oldItem, 0);
            _ = txn.KeyDeleteAsync(indexKey);

            if (!await txn.ExecuteAsync())
                throw new ConflictException();
        }

        public async Task<T> ReadItem<T>(string partitionKey, string itemKey)
        {
            var indexKey = _keyResolver.GetEntityKey(_collectionName, partitionKey, itemKey);

            var (raw, etag) = await ReadWithETag(indexKey);
            if (raw.IsEmpty)
                throw new NotFoundException();

            var result = JsonSerializer.Deserialize<T>(raw.Span, _jsonSerializerOptions);

            return result;
        }

        public Task<bool> Exists(string partitionKey, string itemKey)
        {
            var indexKey = _keyResolver.GetEntityKey(_collectionName, partitionKey, itemKey);
            return _redis.KeyExistsAsync(indexKey);
        }

        private Task WriteItem<T>(IDatabaseAsync batch, T item, string partitionKey, string itemKey)
        {
            var serializedObject = JsonSerializer.SerializeToUtf8Bytes(item, _jsonSerializerOptions);

            var indexKey = _keyResolver.GetEntityKey(_collectionName, partitionKey, itemKey);

            var tasks = new List<Task>();            
            tasks.Add(_indexer.WriteIndexes(batch, _collectionName, partitionKey, "$index", itemKey, item, 0));            
            
            tasks.Add(batch.HashSetAsync(indexKey, new HashEntry[]
            {
                new HashEntry(DataField, serializedObject),
                new HashEntry(ETagField, BuildEtag(serializedObject)),
            }));
            
            return Task.WhenAll(tasks);
        }

        public IAsyncQueryable<T> AsQueryable<T>(string partitionKey)
        {
            return new RedisQueryContext<T>(this, partitionKey, _serviceProvider);
        }

        internal async IAsyncEnumerable<T> Query<T>(string partitionKey, Expression expression, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var resolver = _resolverProvider.GetExpressionResolver(expression);
            var resultKey = await resolver.Resolve(new ExpressionContext(_redis, _collectionName, partitionKey), expression);
            if (resultKey is not SetKeyResult)
                throw new NotImplementedException();
            var tempKey = (SetKeyResult)resultKey;
            var hasResults = true;
            try
            {
                long index = 0;

                while (hasResults && !cancellationToken.IsCancellationRequested)
                {
                    var results = await _redis.SortedSetRangeByRankAsync(tempKey.Key, index, index + BatchSize);
                    hasResults = results.Length > 0;
                    foreach (var itemKey in results)
                    {
                        var item = await ReadItem<T>(partitionKey, itemKey);
                        yield return item;
                    }

                    index = index + BatchSize + 1;
                }
            }
            finally
            {
                if (tempKey.IsTemp)
                    await _keyResolver.DiscardTempKey(_redis, new RedisKey[] { tempKey.Key });
            }
        }

        private byte[] BuildEtag(byte[] data)
        {
            return SHA1.HashData(data);
        }

        private async Task<(ReadOnlyMemory<byte> Data, byte[] ETag)> ReadWithETag(string key)
        {
            var vals = await _redis.HashGetAsync(key, new RedisValue[] { DataField, ETagField });
            return (vals[0], vals[1]);
        }
    }
}
