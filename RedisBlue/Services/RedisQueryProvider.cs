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
            switch (typeof(TResult))
            {
                case Type t when t == typeof(IAsyncEnumerable<T>):
                    var asyncEnumerable = _source.Query<T>(_partitionKey, expression);
                    return (TResult)asyncEnumerable;

                case Type t when t.IsValueType:
                    var valueResult = await _source.QueryValue<T>(_partitionKey, expression);
                    return (TResult)Convert.ChangeType(valueResult, t);

                default: 
                    throw new NotImplementedException();
            };
        }
    }
}
