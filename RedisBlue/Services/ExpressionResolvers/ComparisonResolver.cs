using RedisBlue.Interfaces;
using RedisBlue.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class ComparisonResolver : IExpressionResolver
    {
        private readonly IScoreCalculator _scoreCalculator;
        private readonly IKeyResolver _keyResolver;
        private readonly IResolverProvider _resolverProvider;

        public ComparisonResolver(IScoreCalculator scoreCalculator, IKeyResolver keyResolver, IResolverProvider resolverProvider)
        {
            _scoreCalculator = scoreCalculator;
            _keyResolver = keyResolver;
            _resolverProvider = resolverProvider;
        }

        public ExpressionType[] NodeTypes => new ExpressionType[]
        {
            ExpressionType.Equal,
            ExpressionType.NotEqual,
            ExpressionType.GreaterThan,
            ExpressionType.GreaterThanOrEqual,
            ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual,
        };

        public async Task<ResolverResult> Resolve(ExpressionContext context, Expression expression)
        {
            if (expression is not BinaryExpression)
                throw new NotImplementedException();

            var binary = (BinaryExpression)expression;

            var leftResolver = _resolverProvider.GetExpressionResolver(binary.Left);
            var rightResolver = _resolverProvider.GetExpressionResolver(binary.Right);

            var leftResult = await leftResolver.Resolve(context, binary.Left);
            if (leftResult is not MemberResult)
                throw new NotImplementedException();
            var left = (MemberResult)leftResult;

            var rightResult = await rightResolver.Resolve(context, binary.Right);
            if (rightResult is not ValueResult)
                throw new NotImplementedException();
            var right = (ValueResult)rightResult;

            var rangeKey = $"{_keyResolver.GetPartitionKey(context.CollectionName, context.PartitionKey)}:$index:{left.Path}:$range";
            var destKey = _keyResolver.GetTempKey(context.CollectionName, context.PartitionKey);
            var score = _scoreCalculator.Calculate(right.Value);

            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                    var hash = _scoreCalculator.Hash(right.Value);
                    var hashKey = $"{_keyResolver.GetPartitionKey(context.CollectionName, context.PartitionKey)}:$index:{left.Path}:$hash:{hash}";
                    return new SetKeyResult(hashKey, false);
                case ExpressionType.NotEqual:
                    var beforeKey = _keyResolver.GetTempKey(context.CollectionName, context.PartitionKey);
                    var afterKey = _keyResolver.GetTempKey(context.CollectionName, context.PartitionKey);
                    try
                    {
                        await context.Db.SortedSetRangeByScoreStoreAsync(beforeKey, rangeKey, "-inf", $"({score}");
                        await context.Db.SortedSetRangeByScoreStoreAsync(afterKey, rangeKey, $"({score}", "+inf");
                        await context.Db.SortedSetCombineAndStoreAsync(SetOperation.Union, destKey, beforeKey, afterKey);
                    }
                    finally
                    {
                        await _keyResolver.DiscardTempKey(context.Db, new RedisKey[] { beforeKey, afterKey });
                    }
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    await context.Db.SortedSetRangeByScoreStoreAsync(destKey, rangeKey, score, "+inf");
                    break;
                case ExpressionType.GreaterThan:
                    await context.Db.SortedSetRangeByScoreStoreAsync(destKey, rangeKey, $"({score}", "+inf");
                    break;
                case ExpressionType.LessThanOrEqual:
                    await context.Db.SortedSetRangeByScoreStoreAsync(destKey, rangeKey, "-inf", score);
                    break;
                case ExpressionType.LessThan:
                    await context.Db.SortedSetRangeByScoreStoreAsync(destKey, rangeKey, "-inf", $"({score}");
                    break;
            }

            return new SetKeyResult(destKey, true);
        }
    }
}
