using Microsoft.Extensions.DependencyInjection;
using RedisBlue.Interfaces;
using RedisBlue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class ResolverProvider : IResolverProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<Dictionary<Type, IOperandResolver>> _resolvers;

        public ResolverProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _resolvers = new Lazy<Dictionary<Type, IOperandResolver>>(() =>
            {
                var result = new Dictionary<Type, IOperandResolver>();
                foreach (var impl in _serviceProvider.GetServices<IOperandResolver>())
                {
                    result.Add(impl.OperandType, impl);
                }

                return result;
            });
        }

        public IOperandResolver GetResolver(Operand operand)
        {
            return _resolvers.Value[operand.GetType()];
        }
    }
}
