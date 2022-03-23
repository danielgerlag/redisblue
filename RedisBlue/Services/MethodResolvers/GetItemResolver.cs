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
    internal class GetItemResolver : IMethodResolver
    {
        private readonly IResolverProvider _resolverProvider;

        public GetItemResolver(IResolverProvider resolverProvider)
        {
            _resolverProvider = resolverProvider;
        }
        public string MethodName => "get_Item";

        public async Task<ResolverResult> Resolve(ExpressionContext context, MethodCallExpression expression)
        {
            var objResolver = _resolverProvider.GetExpressionResolver(expression.Object);
            var keyResolver = _resolverProvider.GetExpressionResolver(expression.Arguments[0]);

            var objResult = await objResolver.Resolve(context, expression.Object);
            var keyResult = await keyResolver.Resolve(context, expression.Arguments[0]);

            if (objResult is not MemberResult)
                throw new NotImplementedException();
            if (keyResult is not ValueResult)
                throw new NotImplementedException();

            var obj = (MemberResult)objResult;
            var key = (ValueResult)keyResult;

            return new MemberResult($"{obj.Path}:{key.Value}");
        }
    }
}