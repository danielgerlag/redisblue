using StackExchange.Redis;
using System.Threading.Tasks;

namespace RedisBlue.Interfaces
{
    internal interface IIndexer
    {
        Task RemoveIndexes(IDatabaseAsync batch, string collectionName, string partitionKey, string path, string itemKey, object item, int depth);
        Task WriteIndexes(IDatabaseAsync batch, string collectionName, string partitionKey, string path, string itemKey, object item, int depth);
    }
}