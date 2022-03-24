using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue
{
    internal static class RedisExtensions
    {
        /// <summary>
        /// Stores the result of ZRANGE BYSCORE in a new set.
        /// </summary>
        /// <returns>Number of items stored</returns>
        public static async Task<long> SortedSetRangeByScoreStoreAsync(this IDatabaseAsync redis, RedisKey destKey, RedisKey sourceKey, RedisValue min, RedisValue max)
        {   
            var result = await redis.ExecuteAsync("ZRANGESTORE", destKey, sourceKey, min, max, "BYSCORE");
            return (long)result;
        }

        public static async Task<long> SortedSetDiffStoreAsync(this IDatabaseAsync redis, RedisKey destKey, params RedisKey[] sourceKeys)
        {
            var parameters = new List<object> { destKey, sourceKeys.Count() };
            parameters.AddRange(sourceKeys.Cast<object>());
            var result = await redis.ExecuteAsync("ZDIFFSTORE", parameters);
            return (long)result;
        }

        /// <summary>
        /// Stores the result of ZRANGE BYLEX in a new set.
        /// </summary>
        /// <returns>Number of items stored</returns>
        public static async Task<long> SortedSetRangeByLexStoreAsync(this IDatabaseAsync redis, RedisKey destKey, RedisKey sourceKey, RedisValue min, RedisValue max)
        {
            var result = await redis.ExecuteAsync("ZRANGESTORE", destKey, sourceKey, min, max, "BYLEX");
            return (long)result;
        }
    }
}
