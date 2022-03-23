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

        public async Task<ResolverResult> Resolve(IDatabaseAsync db, string collectionName, string partitionKey, Expression expression)
        {
            if (expression is not BinaryExpression)
                throw new NotImplementedException();

            var binary = (BinaryExpression)expression;

            var destKey = _keyResolver.GetTempKey(collectionName, partitionKey);
            
            var weights = new List<double>();
            var tempKeys = new List<RedisKey>();

            try
            {
                var leftResolver = _resolverProvider.GetExpressionResolver(binary.Left);
                var rightResolver = _resolverProvider.GetExpressionResolver(binary.Right);

                var leftResult = await leftResolver.Resolve(db, collectionName, partitionKey, binary.Left);
                if (leftResult is not SetKeyResult)
                    throw new NotImplementedException();
                tempKeys.Add(((SetKeyResult)leftResult).Key);
                weights.Add(0);

                var rightResult = await rightResolver.Resolve(db, collectionName, partitionKey, binary.Right);
                if (rightResult is not SetKeyResult)
                    throw new NotImplementedException();
                tempKeys.Add(((SetKeyResult)rightResult).Key);
                weights.Add(0);

                switch (expression.NodeType)
                {
                    case ExpressionType.AndAlso:
                        await db.SortedSetCombineAndStoreAsync(SetOperation.Intersect, destKey, tempKeys.ToArray(), weights.ToArray());
                        break;
                    case ExpressionType.OrElse:
                        await db.SortedSetCombineAndStoreAsync(SetOperation.Union, destKey, tempKeys.ToArray(), weights.ToArray());
                        break;
                }

                return new SetKeyResult(destKey);
            }
            finally
            {
                await _keyResolver.DiscardTempKey(db, tempKeys);
            }
        }
    }
}
