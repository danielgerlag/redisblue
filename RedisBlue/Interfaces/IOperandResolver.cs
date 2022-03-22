using RedisBlue.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Interfaces
{
    internal interface IOperandResolver
    {
        Type OperandType { get; }
        Task<RedisKey> Resolve(IDatabaseAsync db, string collectionName, string partitionKey, Operand operand);
    }
}
