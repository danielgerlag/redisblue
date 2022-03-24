using RedisBlue.Interfaces;
using RedisBlue.Models;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class NotResolver : IExpressionResolver
    {
        private readonly IResolverProvider _resolverProvider;
        private readonly IKeyResolver _keyResolver;

        public NotResolver(IResolverProvider resolverProvider, IKeyResolver keyResolver)
        {
            _resolverProvider = resolverProvider;
            _keyResolver = keyResolver;
        }

        public ExpressionType[] NodeTypes => new ExpressionType[]
        {
            ExpressionType.Not,
        };

        public async Task<ResolverResult> Resolve(ExpressionContext context, Expression expression)
        {
            if (expression is not UnaryExpression)
                throw new NotImplementedException();

            var unary = (UnaryExpression)expression;           
                        
            if (string.IsNullOrEmpty(context.TypeInfo.KeyPropertyName))
                throw new NotSupportedException($"No key defined");

            var defaultIndexKey = $"{_keyResolver.GetPartitionKey(context.CollectionName, context.PartitionKey)}:$index:{context.TypeInfo.KeyPropertyName}:$range";
            var destKey = _keyResolver.GetTempKey(context.CollectionName, context.PartitionKey);

            var sourceResolver = _resolverProvider.GetExpressionResolver(unary.Operand);
            var sourceResult = await sourceResolver.Resolve(context, unary.Operand);

            if (sourceResult is not SetKeyResult)
                throw new NotImplementedException();
            var sourceKey = (SetKeyResult)sourceResult;

            try
            {
                await context.Db.SortedSetDiffStoreAsync(destKey, defaultIndexKey, sourceKey.Key);
            }
            finally
            {
                await _keyResolver.DiscardTempKey(context.Db, sourceKey);
            }

            return new SetKeyResult(destKey, true);
        }
    }
}
