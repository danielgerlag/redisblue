using RedisBlue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Interfaces
{
    internal interface IResolverProvider
    {
        IOperandResolver GetResolver(Operand operand);
    }
}
