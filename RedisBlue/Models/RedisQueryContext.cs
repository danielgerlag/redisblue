using Microsoft.Extensions.DependencyInjection;
using RedisBlue.Interfaces;
using RedisBlue.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisBlue.Models
{
    internal class RedisQueryContext<T> : IOrderedAsyncQueryable<T>
    {
        public RedisQueryContext(IndexedCollection source, string partitionKey, IServiceProvider serviceProvider)
        {
            Expression = Expression.Parameter(GetType());
            Provider = new RedisQueryProvider<T>(source, partitionKey);
        }

        public RedisQueryContext(IAsyncQueryProvider provider, Expression expression)
        {
            Provider = provider;
            Expression = expression;
        }

        public Type ElementType => typeof(T);

        public Expression Expression { get; private set; }
        public IAsyncQueryProvider Provider { get; private set; }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var enumerable = await Provider.ExecuteAsync<IAsyncEnumerable<T>>(Expression, cancellationToken);
            await foreach (var item in enumerable)
                yield return item;
        }
    }
}
