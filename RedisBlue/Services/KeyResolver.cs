using RedisBlue.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class KeyResolver : IKeyResolver
    {
        public Task DiscardTempKey(IDatabaseAsync db, IEnumerable<RedisKey> keys)
        {
            _ = db.KeyDeleteAsync(keys.ToArray(), CommandFlags.FireAndForget);
            return Task.CompletedTask;
        }

        public string GetEntityKey(string collection, string partitionKey, string key)
        {
            return $"{GetPartitionKey(collection, partitionKey)}:{key}";
        }

        public string GetPartitionKey(string collection, string partitionKey)
        {
            return $"{collection}:{{{partitionKey}}}";
        }

        public string GetTempKey(string collection, string partitionKey)
        {
            return $"{collection}:{{{partitionKey}}}:$temp:{Guid.NewGuid():N}";
        }
    }
}
