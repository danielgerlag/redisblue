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
    internal class ConstantResolver : IExpressionResolver
    {
        public ConstantResolver()
        {
        }

        public ExpressionType[] NodeTypes => new ExpressionType[]
        {
            ExpressionType.Constant,
        };

        public async Task<ResolverResult> Resolve(ExpressionContext context, Expression expression)
        {
            if (expression is not ConstantExpression)
                throw new NotImplementedException();

            var constant = (ConstantExpression)expression;
            
            return new ValueResult(constant.Value);
        }
    }
}
