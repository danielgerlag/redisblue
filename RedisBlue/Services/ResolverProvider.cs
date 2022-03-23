using Microsoft.Extensions.DependencyInjection;
using RedisBlue.Interfaces;
using RedisBlue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class ResolverProvider : IResolverProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<Dictionary<ExpressionType, IExpressionResolver>> _expressionResolvers;
        private readonly Lazy<Dictionary<string, IMethodResolver>> _methodResolvers;

        public ResolverProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _expressionResolvers = new Lazy<Dictionary<ExpressionType, IExpressionResolver>>(() =>
            {
                var result = new Dictionary<ExpressionType, IExpressionResolver>();
                foreach (var impl in _serviceProvider.GetServices<IExpressionResolver>())
                {
                    foreach (var nt in impl.NodeTypes)
                    {
                        result.Add(nt, impl);
                    }
                }

                return result;
            });

            _methodResolvers = new Lazy<Dictionary<string, IMethodResolver>>(() =>
            {
                var result = new Dictionary<string, IMethodResolver>();
                foreach (var impl in _serviceProvider.GetServices<IMethodResolver>())
                {
                    result.Add(impl.MethodName, impl);
                }

                return result;
            });
        }

        public IExpressionResolver GetExpressionResolver(Expression expression)
        {
            var t = expression.GetType();
            if (!_expressionResolvers.Value.ContainsKey(expression.NodeType))
                throw new NotImplementedException();
            return _expressionResolvers.Value[expression.NodeType];
        }

        public IMethodResolver GetMethodResolver(MethodCallExpression expression)
        {
            if (!_methodResolvers.Value.ContainsKey(expression.Method.Name))
                throw new NotImplementedException();
            return _methodResolvers.Value[expression.Method.Name];
        }
    }
}
