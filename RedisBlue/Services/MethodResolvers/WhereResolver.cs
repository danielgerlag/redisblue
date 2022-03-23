using RedisBlue.Interfaces;
using RedisBlue.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services.MethodResolvers
{
    internal class WhereResolver : IMethodResolver
    {
        private readonly IResolverProvider _resolverProvider;
        private readonly IKeyResolver _keyResolver;

        public WhereResolver(IResolverProvider resolverProvider, IKeyResolver keyResolver)
        {
            _resolverProvider = resolverProvider;
            _keyResolver = keyResolver;
        }
        public string MethodName => "Where";

        public async Task<ResolverResult> Resolve(ExpressionContext context, MethodCallExpression expression)
        {
            if (expression.Arguments.Count < 2)
                throw new ArgumentException();

            var predicateResolver = _resolverProvider.GetExpressionResolver(expression.Arguments[1]);
            var predicateResult = await predicateResolver.Resolve(context, expression.Arguments[1]);
            if (predicateResult is not SetKeyResult)
                throw new NotImplementedException();
            var predicateKey = (SetKeyResult)predicateResult;

            if (expression.Arguments[0].NodeType == ExpressionType.Parameter)
                return predicateKey;

            try
            {
                var sourceResolver = _resolverProvider.GetExpressionResolver(expression.Arguments[0]);
                var sourceResult = await sourceResolver.Resolve(context, expression.Arguments[0]);

                if (sourceResult is not SetKeyResult)
                    throw new NotImplementedException();
                var sourceKey = (SetKeyResult)sourceResult;

                var destKey = _keyResolver.GetTempKey(context.CollectionName, context.PartitionKey);

                try
                {
                    await context.Db.SortedSetCombineAndStoreAsync(SetOperation.Intersect, destKey, new RedisKey[] { sourceKey.Key, predicateKey.Key }, new double[] { 0, 1 });
                    return new SetKeyResult(destKey, true);
                }
                finally
                {   
                    if (sourceKey.IsTemp)
                        await _keyResolver.DiscardTempKey(context.Db, new RedisKey[] { sourceKey.Key });
                }
            }
            finally
            {
                if (predicateKey.IsTemp)
                    await _keyResolver.DiscardTempKey(context.Db, new RedisKey[] { predicateKey.Key });
            }
        }
    }
}
