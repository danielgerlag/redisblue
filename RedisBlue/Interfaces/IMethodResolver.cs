using RedisBlue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Interfaces
{
    internal interface IMethodResolver
    {
        string MethodName { get; }
        Task<ResolverResult> Resolve(ExpressionContext context, MethodCallExpression expression);
    }
}
