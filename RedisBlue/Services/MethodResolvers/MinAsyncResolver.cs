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
    internal class MinAsyncResolver : IMethodResolver
    {
        private readonly IResolverProvider _resolverProvider;
        private readonly IKeyResolver _keyResolver;

        public MinAsyncResolver(IResolverProvider resolverProvider, IKeyResolver keyResolver)
        {
            _resolverProvider = resolverProvider;
            _keyResolver = keyResolver;
        }
        public string MethodName => "MinAsync";

        public async Task<ResolverResult> Resolve(ExpressionContext context, MethodCallExpression expression)
        {
            if (expression.Arguments.Count < 2)
                throw new ArgumentException();
                        
            var memberResolver = _resolverProvider.GetExpressionResolver(expression.Arguments[1]);
            var memberResult = await memberResolver.Resolve(context, expression.Arguments[1]);
            if (memberResult is not MemberResult)
                throw new NotImplementedException();

            var member = (MemberResult)memberResult;
            var memberKey = $"{_keyResolver.GetPartitionKey(context.CollectionName, context.PartitionKey)}:$index:{member.Path}:$range";

            var sourceResolver = _resolverProvider.GetExpressionResolver(expression.Arguments[0]);
            var sourceResult = await sourceResolver.Resolve(context, expression.Arguments[0]);
            if (sourceResult is not SetKeyResult)
                throw new NotImplementedException();
            var sourceKey = (SetKeyResult)sourceResult;

            var destKey = _keyResolver.GetTempKey(context.CollectionName, context.PartitionKey);
            try
            {
                await context.Db.SortedSetCombineAndStoreAsync(SetOperation.Intersect, destKey, new RedisKey[] { sourceKey.Key, memberKey }, new double[] { 0, 1 });
                var range = await context.Db.SortedSetRangeByRankAsync(destKey, 0, 0);
                if (range.Count() == 0)
                    return new ValueResult(null);
                var itemKey = range.First();
                var lookupKey = $"{_keyResolver.GetPartitionKey(context.CollectionName, context.PartitionKey)}:$index:{member.Path}:$lookup";
                var result = await context.Db.HashGetAsync(lookupKey, itemKey);
                
                return new ValueResult(result.ToString());
            }
            finally
            {
                await _keyResolver.DiscardTempKey(context.Db, sourceKey);
                await _keyResolver.DiscardTempKey(context.Db, new RedisKey[] { destKey });
            }
        }
    }
}
