using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Interfaces
{
    internal interface IScoreCalculator
    {
        double Calculate(object value);
    }
}
