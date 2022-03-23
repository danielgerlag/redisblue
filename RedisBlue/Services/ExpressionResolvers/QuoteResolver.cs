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
    internal class QuoteResolver : IExpressionResolver
    {
        private readonly IResolverProvider _resolverProvider;

        public QuoteResolver(IResolverProvider resolverProvider)
        {
            _resolverProvider = resolverProvider;
        }

        public ExpressionType[] NodeTypes => new ExpressionType[]
        {
            ExpressionType.Quote,
        };

        public async Task<ResolverResult> Resolve(ExpressionContext context, Expression expression)
        {
            if (expression is not UnaryExpression)
                throw new NotImplementedException();

            var unary = (UnaryExpression)expression;
            var opResolver = _resolverProvider.GetExpressionResolver(unary.Operand);

            return await opResolver.Resolve(context, unary.Operand);
        }
    }
}
