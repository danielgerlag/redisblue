using Microsoft.Extensions.DependencyInjection;
using RedisBlue.Interfaces;
using RedisBlue.Models;
using RedisBlue.Services;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue
{
    public partial class IndexedCollection
    {
        private static IServiceProvider _serviceProvider;

        static IndexedCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ConcurrentDictionary<Type, TypeCacheInfo>>(sp => new ConcurrentDictionary<Type, TypeCacheInfo>());
            serviceCollection.AddSingleton<IKeyResolver, KeyResolver>();
            serviceCollection.AddSingleton<IScoreCalculator, ScoreCalculator>();
            serviceCollection.AddSingleton<IIndexer, Indexer>();
            serviceCollection.AddSingleton<IResolverProvider, ResolverProvider>();
            serviceCollection.AddSingleton<IOperandResolver, ComparisonResolver>();
            serviceCollection.AddSingleton<IOperandResolver, LogicalResolver>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public static IndexedCollection Create(IDatabase database, string collectionName)
        {
            var indexer = _serviceProvider.GetRequiredService<IIndexer>();
            var keyResolver = _serviceProvider.GetRequiredService<IKeyResolver>();
            var resolverProvider = _serviceProvider.GetRequiredService<IResolverProvider>();
            var typeCache = _serviceProvider.GetRequiredService<ConcurrentDictionary<Type, TypeCacheInfo>>();

            return new IndexedCollection(database, collectionName, indexer, keyResolver, resolverProvider, typeCache);
        }

    }
}
