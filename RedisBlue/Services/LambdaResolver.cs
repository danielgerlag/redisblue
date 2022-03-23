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
    internal class LambdaResolver : IExpressionResolver
    {
        private readonly IResolverProvider _resolverProvider;

        public LambdaResolver(IResolverProvider resolverProvider)
        {
            _resolverProvider = resolverProvider;
        }

        public ExpressionType[] NodeTypes => new ExpressionType[]
        {
            ExpressionType.Lambda,
        };

        public async Task<ResolverResult> Resolve(IDatabaseAsync db, string collectionName, string partitionKey, Expression expression)
        {
            if (expression is not LambdaExpression)
                throw new NotImplementedException();

            var lambda = (LambdaExpression)expression;
            var bodyResolver = _resolverProvider.GetExpressionResolver(lambda.Body);

            return await bodyResolver.Resolve(db, collectionName, partitionKey, lambda.Body);
        }
    }
}
