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
    internal class ParameterResolver : IExpressionResolver
    {
        private readonly ConcurrentDictionary<Type, TypeCacheInfo> _typeCache;
        private readonly IKeyResolver _keyResolver;

        public ParameterResolver(ConcurrentDictionary<Type, TypeCacheInfo> typeCache, IKeyResolver keyResolver)
        {
            _typeCache = typeCache;
            _keyResolver = keyResolver;
        }

        public ExpressionType[] NodeTypes => new ExpressionType[]
        {
            ExpressionType.Parameter,
        };

        public async Task<ResolverResult> Resolve(ExpressionContext context, Expression expression)
        {
            if (expression is not ParameterExpression)
                throw new NotImplementedException();

            var parameter = (ParameterExpression)expression;
            var elementType = parameter.Type.GenericTypeArguments.FirstOrDefault();
            if (elementType == null)
                throw new NotSupportedException();

            var typeInfo = _typeCache.GetOrAdd(elementType, t => new TypeCacheInfo(t));
            if (string.IsNullOrEmpty(typeInfo.KeyPropertyName))
                throw new NotSupportedException($"No key defined for {elementType}");

            var defaultIndex = $"{_keyResolver.GetPartitionKey(context.CollectionName, context.PartitionKey)}:$index:{typeInfo.KeyPropertyName}:$range";

            return new SetKeyResult(defaultIndex, false);
        }
    }
}
