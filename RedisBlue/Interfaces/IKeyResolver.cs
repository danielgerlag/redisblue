using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisBlue.Interfaces
{
    internal interface IKeyResolver
    {
        string GetEntityKey(string collection, string partitionKey, string key);
        string GetPartitionKey(string collection, string partitionKey);
        string GetTempKey(string collection, string partitionKey);
        Task DiscardTempKey(IDatabaseAsync db, IEnumerable<RedisKey> keys);
    }
}