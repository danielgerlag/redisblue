using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Models
{
    internal class ExpressionContext
    {
        public ExpressionContext(IDatabaseAsync db, string collectionName, string partitionKey)
        {
            Db = db;
            CollectionName = collectionName;
            PartitionKey = partitionKey;
        }

        public IDatabaseAsync Db { get; set; }
        public string CollectionName { get; set; }
        public string PartitionKey { get; set; }
    }
}
