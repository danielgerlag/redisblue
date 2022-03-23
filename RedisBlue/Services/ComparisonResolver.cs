using RedisBlue.Interfaces;
using RedisBlue.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class ComparisonResolver : IOperandResolver
    {
        private readonly IScoreCalculator _scoreCalculator;
        private readonly IKeyResolver _keyResolver;

        public ComparisonResolver(IScoreCalculator scoreCalculator, IKeyResolver keyResolver)
        {
            _scoreCalculator = scoreCalculator;
            _keyResolver = keyResolver;
        }

        public Type OperandType => typeof(ComparisonOperand);

        public async Task<RedisKey> Resolve(IDatabaseAsync db, string collectionName, string partitionKey, Operand operand)
        {
            if (operand is not ComparisonOperand)
                throw new ArgumentException();

            var comparision = (ComparisonOperand)operand;

            if (comparision.Left is not MemberOperand)
                throw new NotImplementedException();

            if (comparision.Right is not ConstantOperand)
                throw new NotImplementedException();

            var left = (MemberOperand)comparision.Left;
            var right = (ConstantOperand)comparision.Right;

            var rangeKey = $"{_keyResolver.GetPartitionKey(collectionName, partitionKey)}:$index:{left.Path}:$range";
            var destKey = _keyResolver.GetTempKey(collectionName, partitionKey);
            var score = _scoreCalculator.Calculate(right.Value);

            switch (comparision.Operator)
            {
                case ComparisonOperator.Equal:
                    await db.SortedSetRangeByScoreStoreAsync(destKey, rangeKey, score, score);
                    break;
                case ComparisonOperator.NotEqual:
                    var beforeKey = _keyResolver.GetTempKey(collectionName, partitionKey);
                    var afterKey = _keyResolver.GetTempKey(collectionName, partitionKey);
                    try
                    {
                        await db.SortedSetRangeByScoreStoreAsync(beforeKey, rangeKey, "-inf", $"({score}");
                        await db.SortedSetRangeByScoreStoreAsync(afterKey, rangeKey, $"({score}", "+inf");
                        await db.SortedSetCombineAndStoreAsync(SetOperation.Union, destKey, beforeKey, afterKey);
                    }
                    finally
                    {
                        await _keyResolver.DiscardTempKey(db, new RedisKey[] { beforeKey, afterKey });
                    }
                    break;
                case ComparisonOperator.GreaterThanEqual:
                    await db.SortedSetRangeByScoreStoreAsync(destKey, rangeKey, score, "+inf");
                    break;
                case ComparisonOperator.GreaterThan:
                    await db.SortedSetRangeByScoreStoreAsync(destKey, rangeKey, $"({score}", "+inf");
                    break;
                case ComparisonOperator.LessThanEqual:
                    await db.SortedSetRangeByScoreStoreAsync(destKey, rangeKey, "-inf", score);
                    break;
                case ComparisonOperator.LessThan:
                    await db.SortedSetRangeByScoreStoreAsync(destKey, rangeKey, "-inf", $"({score}");
                    break;
            }

            return destKey;
        }
    }
}
