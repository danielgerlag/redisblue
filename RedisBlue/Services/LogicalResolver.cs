using RedisBlue.Interfaces;
using RedisBlue.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class LogicalResolver : IOperandResolver
    {
        private readonly IKeyResolver _keyResolver;
        private readonly IResolverProvider _resolverProvider;

        public LogicalResolver(IKeyResolver keyResolver, IResolverProvider resolverProvider)
        {
            _keyResolver = keyResolver;
            _resolverProvider = resolverProvider;
        }

        public Type OperandType => typeof(LogicalOperand);

        public async Task<RedisKey> Resolve(IDatabaseAsync db, string collectionName, string partitionKey, Operand operand)
        {
            if (operand is not LogicalOperand)
                throw new ArgumentException();

            var logicalOp = (LogicalOperand)operand;

            var destKey = _keyResolver.GetTempKey(collectionName, partitionKey);
            
            var weights = new List<double>();
            var tempKeys = new List<RedisKey>();

            try
            {
                foreach (var child in logicalOp.Operands)
                {
                    var resolver = _resolverProvider.GetResolver(child);
                    tempKeys.Add(await resolver.Resolve(db, collectionName, partitionKey, child));
                    weights.Add(0);
                }

                switch (logicalOp.Operator)
                {
                    case LogicalOperator.And:
                        await db.SortedSetCombineAndStoreAsync(SetOperation.Intersect, destKey, tempKeys.ToArray(), weights.ToArray());
                        break;
                    case LogicalOperator.Or:
                        await db.SortedSetCombineAndStoreAsync(SetOperation.Union, destKey, tempKeys.ToArray(), weights.ToArray());
                        break;
                }

                return destKey;
            }
            finally
            {
                await _keyResolver.DiscardTempKey(db, tempKeys);
            }
        }
    }
}
