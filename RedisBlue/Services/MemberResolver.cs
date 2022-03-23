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
    internal class MemberResolver : IExpressionResolver
    {
        private readonly IResolverProvider _resolverProvider;

        public MemberResolver(IResolverProvider resolverProvider)
        {
            _resolverProvider = resolverProvider;
        }

        public ExpressionType[] NodeTypes => new ExpressionType[]
        {
            ExpressionType.MemberAccess,
        };

        public async Task<ResolverResult> Resolve(IDatabaseAsync db, string collectionName, string partitionKey, Expression expression)
        {
            if (expression is not MemberExpression)
                throw new NotImplementedException();

            var member = (MemberExpression)expression;

            return new MemberResult(GetMemberPath(member));
        }

        private string GetMemberPath(MemberExpression member)
        {
            var path = member.Member.Name;

            while (member.Expression is not ParameterExpression)
            {
                switch (member.Expression)
                {
                    case MemberExpression me:
                        member = me;
                        path = $"{me.Member.Name}:{path}";
                        break;
                }
            }

            return path;
        }
    }
}
