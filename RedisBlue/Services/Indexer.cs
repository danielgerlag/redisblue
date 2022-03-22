using RedisBlue.Interfaces;
using RedisBlue.Models;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class Indexer : IIndexer
    {
        private const int MaxDepth = 32;
        private readonly IScoreCalculator _scoreCalculator;
        private readonly IKeyResolver _keyResolver;
        private readonly ConcurrentDictionary<Type, TypeCacheInfo> _typeCache;

        public Indexer(IScoreCalculator scoreCalculator, IKeyResolver keyResolver, ConcurrentDictionary<Type, TypeCacheInfo> typeCache)
        {
            _scoreCalculator = scoreCalculator;
            _keyResolver = keyResolver;
            _typeCache = typeCache;
        }

        public Task WriteIndexes(IDatabaseAsync batch, string collectionName, string partitionKey, string path, string itemKey, object item, int depth)
        {
            if (item == null || depth > MaxDepth)
                return Task.CompletedTask;

            var tasks = new List<Task>();
            var typeInfo = _typeCache.GetOrAdd(item.GetType(), t => new TypeCacheInfo(t));

            if (typeInfo.IsLeafValue)
            {
                var score = _scoreCalculator.Calculate(item);
                tasks.Add(batch.SortedSetAddAsync($"{_keyResolver.GetPartitionKey(collectionName, partitionKey)}:{path}:$range", itemKey, score));
            }

            if (item is IDictionary dictItem)
            {
                foreach (var k in dictItem.Keys)
                {
                    tasks.Add(WriteIndexes(batch, collectionName, partitionKey, $"{path}:{k}", itemKey, dictItem[k], depth + 1));
                }

                return Task.WhenAll(tasks);
            }

            if (typeInfo.IsCollection && item is IEnumerable collection)
            {
                var k = 0;
                foreach (var colItem in collection)
                {
                    tasks.Add(WriteIndexes(batch, collectionName, partitionKey, $"{path}:{k}", itemKey, colItem, depth + 1));
                    k++;
                }

                return Task.WhenAll(tasks);
            }

            foreach (var (name, meta) in typeInfo.IndexProperties)
            {
                var value = meta.GetMethod.Invoke(item, null);
                tasks.Add(WriteIndexes(batch, collectionName, partitionKey, $"{path}:{name}", itemKey, value, depth + 1));
            }

            return Task.WhenAll(tasks);
        }

        public Task RemoveIndexes(IDatabaseAsync batch, string collectionName, string partitionKey, string path, string itemKey, object item, int depth)
        {
            if (item == null || depth > MaxDepth)
                return Task.CompletedTask;

            var typeInfo = _typeCache.GetOrAdd(item.GetType(), t => new TypeCacheInfo(t));

            var tasks = new List<Task>();

            if (typeInfo.IsLeafValue)
            {
                tasks.Add(batch.SortedSetRemoveAsync($"{_keyResolver.GetPartitionKey(collectionName, partitionKey)}:{path}:$range", itemKey));
            }

            if (item is IDictionary dictItem)
            {
                foreach (var k in dictItem.Keys)
                {
                    tasks.Add(RemoveIndexes(batch, collectionName, partitionKey, $"{path}:{k}", itemKey, dictItem[k], depth + 1));
                }

                return Task.WhenAll(tasks);
            }

            if (typeInfo.IsCollection && item is IEnumerable collection)
            {
                var k = 0;
                foreach (var colItem in collection)
                {
                    tasks.Add(RemoveIndexes(batch, collectionName, partitionKey, $"{path}:{k}", itemKey, colItem, depth + 1));
                    k++;
                }

                return Task.WhenAll(tasks);
            }

            foreach (var (name, meta) in typeInfo.IndexProperties)
            {
                var value = meta.GetMethod.Invoke(item, null);
                tasks.Add(RemoveIndexes(batch, collectionName, partitionKey, $"{path}:{name}", itemKey, value, depth + 1));
            }

            return Task.WhenAll(tasks);
        }
    }
}
