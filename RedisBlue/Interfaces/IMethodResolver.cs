using RedisBlue.Models;
using StackExchange.Redis;
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
        Task<ResolverResult> Resolve(IDatabaseAsync db, string collectionName, string partitionKey, MethodCallExpression expression);
    }
}
