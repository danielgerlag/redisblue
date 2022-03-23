using RedisBlue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Interfaces
{
    interface IExpressionConverter
    {
        Operand Convert(Expression expression);
    }
}
