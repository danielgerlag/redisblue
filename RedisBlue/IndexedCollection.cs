using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedisBlue
{
    public class IndexedCollection
    {
        private readonly string _collectionName;
        private const string ETagField = "ETag";
        private const string DataField = "Data";
        private readonly IDatabase _redis;
        private static ConcurrentDictionary<Type, TypeCacheInfo> _typeCache = new();


        public IndexedCollection(IConnectionMultiplexer connectionMultiplexer, string collectionName)
        {
            _redis = connectionMultiplexer.GetDatabase();
            _collectionName = collectionName;
        }

        public async Task CreateItem<T>(T item)
        {
            var typeInfo = _typeCache.GetOrAdd(typeof(T), t => new TypeCacheInfo(t));

            var partitionKey = Convert.ToString(typeInfo.PartitionKeyGetMethod.Invoke(item, null));
            var itemKey = Convert.ToString(typeInfo.KeyGetMethod.Invoke(item, null));
            var indexKey = BuildEntityIndexKey(partitionKey, itemKey);

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
            var indexKey = BuildEntityIndexKey(partitionKey, itemKey);

            var (raw, etag) = await ReadWithETag(indexKey);
            if (raw.IsEmpty)
                throw new NotFoundException();

            var txn = _redis.CreateTransaction();
            txn.AddCondition(Condition.HashEqual(indexKey, ETagField, etag));
                        
            var oldItem = JsonSerializer.Deserialize<T>(raw.Span);

            _ = RemoveIndexes(txn, partitionKey, "$", itemKey, oldItem, 0);
            _ = WriteItem(txn, item, partitionKey, itemKey);

            if (!await txn.ExecuteAsync())
                throw new ConflictException();
        }

        public async Task DeleteItem<T>(string partitionKey, string itemKey)
        {
            var typeInfo = _typeCache.GetOrAdd(typeof(T), t => new TypeCacheInfo(t));

            var indexKey = BuildEntityIndexKey(partitionKey, itemKey);

            var (raw, etag) = await ReadWithETag(indexKey);
            if (raw.IsEmpty)
                throw new NotFoundException();

            var txn = _redis.CreateTransaction();
            txn.AddCondition(Condition.HashEqual(indexKey, ETagField, etag));

            var oldItem = JsonSerializer.Deserialize<T>(raw.Span);

            _ = RemoveIndexes(txn, partitionKey, "$", itemKey, oldItem, 0);
            _ = txn.KeyDeleteAsync(indexKey);

            if (!await txn.ExecuteAsync())
                throw new ConflictException();
        }


        private Task WriteItem<T>(IDatabaseAsync batch, T item, string partitionKey, string itemKey)
        {
            var serializedObject = JsonSerializer.SerializeToUtf8Bytes(item);            

            var indexKey = BuildEntityIndexKey(partitionKey, itemKey);

            var tasks = new List<Task>();            
            tasks.Add(WriteIndexes(batch, partitionKey, "$", itemKey, item, 0));            
            
            tasks.Add(batch.HashSetAsync(indexKey, new HashEntry[]
            {
                new HashEntry(DataField, serializedObject),
                new HashEntry(ETagField, BuildEtag(serializedObject)),
            }));
            
            //tasks.Add(batch.SetAddAsync(BuildSetKey(resourceId), queueId));

            return Task.WhenAll(tasks);
        }


        private Task WriteIndexes(IDatabaseAsync batch, string partitionKey, string path, string itemKey, object item, int depth)
        {
            if (item == null || depth > 32)
                return Task.CompletedTask;

            var typeInfo = _typeCache.GetOrAdd(item.GetType(), t => new TypeCacheInfo(t));

            var tasks = new List<Task>();
            foreach (var (name, meta) in typeInfo.IndexProperties)
            {
                var value = meta.GetMethod.Invoke(item, null);

                if (meta.IsCollection)
                {

                    continue;
                }

                if (meta.IsObject)
                {
                    tasks.Add(WriteIndexes(batch, partitionKey, $"{path}:{name}", itemKey, value, depth + 1));
                    continue;
                }

                var score = CalculateIndexScore(value);

                tasks.Add(batch.SortedSetAddAsync($"{BuildPartitionIndexKey(partitionKey)}:{path}:{name}", itemKey, score));
            }

            return Task.WhenAll(tasks);
        }

        private Task RemoveIndexes(IDatabaseAsync batch, string partitionKey, string path, string itemKey, object item, int depth)
        {
            if (item == null || depth > 32)
                return Task.CompletedTask;

            var typeInfo = _typeCache.GetOrAdd(item.GetType(), t => new TypeCacheInfo(t));

            var tasks = new List<Task>();
            foreach (var (name, meta) in typeInfo.IndexProperties)
            {
                var value = meta.GetMethod.Invoke(item, null);

                if (meta.IsCollection)
                {

                    continue;
                }

                if (meta.IsObject)
                {
                    tasks.Add(WriteIndexes(batch, partitionKey, $"{path}:{name}", itemKey, value, depth + 1));
                    continue;
                }

                
                tasks.Add(batch.SortedSetRemoveAsync($"{BuildPartitionIndexKey(partitionKey)}:{path}:{name}", itemKey));
            }

            return Task.WhenAll(tasks);
        }


        private string BuildEntityIndexKey(string partitionKey, string key)
        {
            return $"{BuildPartitionIndexKey(partitionKey)}:{key}";
        }

        private string BuildPartitionIndexKey(string partitionKey)
        {
            return $"{_collectionName}:{{{partitionKey}}}";
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

        private static double CalculateIndexScore(object value)
        {
            return value switch
            {
                bool => (bool)value ? 1 : 0,
                int or long or decimal or float or double => Convert.ToDouble(value),
                DateTimeOffset => Convert.ToDouble(((DateTimeOffset)value).ToUnixTimeMilliseconds()),
                _ => Get64BitHash(Convert.ToString(value))
            };
        }

        private static double Get64BitHash(string? strText)
        {
            if (string.IsNullOrEmpty(strText))
                return 0;

            byte[] byteContents = Encoding.Unicode.GetBytes(strText);
            byte[] hashText = MD5.HashData(byteContents);
            var hashCode = BitConverter.ToInt64(hashText, 0);

            for (var start = 8; start < hashText.Length; start += 8)
                hashCode ^= BitConverter.ToInt64(hashText, start);

            return Convert.ToDouble(hashCode);
        }
    }

    internal class TypeCacheInfo
    {
        public readonly Dictionary<string, PropertyCacheInfo> IndexProperties = new();
        public readonly MethodInfo KeyGetMethod;
        public readonly MethodInfo PartitionKeyGetMethod;

        public TypeCacheInfo(Type type)
        {
            foreach (var prop in type.GetProperties())
            {
                var indexAttr = prop.GetCustomAttribute<IndexAttribute>();
                if (indexAttr != null)
                {
                    IndexProperties.Add(prop.Name, new PropertyCacheInfo(prop));
                    //if ()
                }

                var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
                if (keyAttr != null)
                {
                    KeyGetMethod = prop.GetGetMethod();
                }

                var partitionKeyAttr = prop.GetCustomAttribute<PartitionKeyAttribute>();
                if (partitionKeyAttr != null)
                {
                    PartitionKeyGetMethod = prop.GetGetMethod();
                }

            }
        }
    }

    internal class PropertyCacheInfo
    {
        public readonly MethodInfo GetMethod;
        public readonly bool IsCollection;
        public readonly bool IsObject;

        public PropertyCacheInfo(PropertyInfo info)
        {
            GetMethod = info.GetMethod;
            var ifaces = info.PropertyType.GetInterfaces();

            IsCollection = ifaces.Contains(typeof(IEnumerable)) && info.PropertyType != typeof(string);
            IsObject = !info.PropertyType.IsValueType && info.PropertyType != typeof(string);
        }
    }
}
