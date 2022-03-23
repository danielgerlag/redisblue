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

        public WhereResolver(IResolverProvider resolverProvider)
        {
            _resolverProvider = resolverProvider;
        }
        public string MethodName => "Where";

        public Task<ResolverResult> Resolve(IDatabaseAsync db, string collectionName, string partitionKey, MethodCallExpression expression)
        {
            var resolver = _resolverProvider.GetExpressionResolver(expression.Arguments[1]);
            return resolver.Resolve(db, collectionName, partitionKey, expression.Arguments[1]);
        }
    }
}
