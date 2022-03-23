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
    internal class LogicalResolver : IExpressionResolver
    {
        private readonly IKeyResolver _keyResolver;
        private readonly IResolverProvider _resolverProvider;

        public LogicalResolver(IKeyResolver keyResolver, IResolverProvider resolverProvider)
        {
            _keyResolver = keyResolver;
            _resolverProvider = resolverProvider;
        }

        public ExpressionType[] NodeTypes => new ExpressionType[]
        {
            ExpressionType.AndAlso,
            ExpressionType.OrElse,
        };

        public async Task<ResolverResult> Resolve(ExpressionContext context, Expression expression)
        {
            if (expression is not BinaryExpression)
                throw new NotImplementedException();

            var binary = (BinaryExpression)expression;

            var destKey = _keyResolver.GetTempKey(context.CollectionName, context.PartitionKey);
            
            var weights = new List<double>();
            var tempKeys = new List<RedisKey>();

            try
            {
                var leftResolver = _resolverProvider.GetExpressionResolver(binary.Left);
                var rightResolver = _resolverProvider.GetExpressionResolver(binary.Right);

                var leftResult = await leftResolver.Resolve(context, binary.Left);
                if (leftResult is not SetKeyResult)
                    throw new NotImplementedException();
                if (((SetKeyResult)leftResult).IsTemp)
                    tempKeys.Add(((SetKeyResult)leftResult).Key);
                weights.Add(0);

                var rightResult = await rightResolver.Resolve(context, binary.Right);
                if (rightResult is not SetKeyResult)
                    throw new NotImplementedException();
                if (((SetKeyResult)rightResult).IsTemp)
                    tempKeys.Add(((SetKeyResult)rightResult).Key);
                weights.Add(0);

                switch (expression.NodeType)
                {
                    case ExpressionType.AndAlso:
                        await context.Db.SortedSetCombineAndStoreAsync(SetOperation.Intersect, destKey, tempKeys.ToArray(), weights.ToArray());
                        break;
                    case ExpressionType.OrElse:
                        await context.Db.SortedSetCombineAndStoreAsync(SetOperation.Union, destKey, tempKeys.ToArray(), weights.ToArray());
                        break;
                }

                return new SetKeyResult(destKey, true);
            }
            finally
            {
                await _keyResolver.DiscardTempKey(context.Db, tempKeys);
            }
        }
    }
}
