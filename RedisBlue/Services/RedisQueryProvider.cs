using RedisBlue.Interfaces;
using RedisBlue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class RedisQueryProvider<T> : IAsyncQueryProvider
    {
        private readonly IndexedCollection _source;
        private readonly string _partitionKey;

        public RedisQueryProvider(IndexedCollection source, string partitionKey)
        {
            _source = source;
            _partitionKey = partitionKey;
        }

        public IAsyncQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new RedisQueryContext<TElement>(this, expression);
        }

        public async ValueTask<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token)
        {
            //var op = _expressionConverter.Convert(expression);
            var asyncEnumerable = _source.Query<T>(_partitionKey, expression);
            
            return typeof(TResult) switch
            {
                Type t when t == typeof(IAsyncEnumerable<T>) => (TResult)asyncEnumerable,
                _ => throw new NotImplementedException()
            };
        }
    }
}
