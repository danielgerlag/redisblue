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
    internal class OrderByResolver : IMethodResolver
    {
        private readonly IResolverProvider _resolverProvider;
        private readonly IKeyResolver _keyResolver;

        public OrderByResolver(IResolverProvider resolverProvider, IKeyResolver keyResolver)
        {
            _resolverProvider = resolverProvider;
            _keyResolver = keyResolver;
        }
        public string MethodName => "OrderBy";

        public async Task<ResolverResult> Resolve(ExpressionContext context, MethodCallExpression expression)
        {
            if (expression.Arguments.Count < 2)
                throw new ArgumentException();
                        
            var memberResolver = _resolverProvider.GetExpressionResolver(expression.Arguments[1]);
            var memberResult = await memberResolver.Resolve(context, expression.Arguments[1]);
            if (memberResult is not MemberResult)
                throw new NotImplementedException();

            var member = (MemberResult)memberResult;
            var orderKey = $"{_keyResolver.GetPartitionKey(context.CollectionName, context.PartitionKey)}:$index:{member.Path}:$range";

            var sourceResolver = _resolverProvider.GetExpressionResolver(expression.Arguments[0]);
            var sourceResult = await sourceResolver.Resolve(context, expression.Arguments[0]);
            if (sourceResult is not SetKeyResult)
                throw new NotImplementedException();
            var sourceKey = (SetKeyResult)sourceResult;

            var destKey = _keyResolver.GetTempKey(context.CollectionName, context.PartitionKey);

            try
            {
                await context.Db.SortedSetCombineAndStoreAsync(SetOperation.Intersect, destKey, new RedisKey[] { sourceKey.Key, orderKey }, new double[] { 0, 1 });
                return new SetKeyResult(destKey, true);
            }
            finally
            {
                if (sourceKey.IsTemp)
                    await _keyResolver.DiscardTempKey(context.Db, new RedisKey[] { sourceKey.Key });
            }
        }
    }
}
