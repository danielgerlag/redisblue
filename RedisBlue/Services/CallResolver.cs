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
    internal class CallResolver : IExpressionResolver
    {
        private readonly IResolverProvider _resolverProvider;

        public CallResolver(IResolverProvider resolverProvider)
        {
            _resolverProvider = resolverProvider;
        }

        public ExpressionType[] NodeTypes => new ExpressionType[]
        {
            ExpressionType.Call,
        };

        public async Task<ResolverResult> Resolve(IDatabaseAsync db, string collectionName, string partitionKey, Expression expression)
        {
            if (expression is not MethodCallExpression)
                throw new NotImplementedException();

            var method = (MethodCallExpression)expression;
            var methodResolver = _resolverProvider.GetMethodResolver(method);

            return await methodResolver.Resolve(db, collectionName, partitionKey, method);
        }
    }
}
