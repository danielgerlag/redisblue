using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Models
{
    public abstract class Operand
    {

    }

    public class ConstantOperand : Operand
    {
        public object Value { get; set; }
    }

    public class MemberOperand : Operand
    {
        public string Path { get; set; }
    }

    public class ComparisonOperand : Operand
    {
        public MemberOperand Left { get; set; }
        public ConstantOperand Right { get; set; }
        public ComparisonOperator Operator { get; set; }
    }

    public class LogicalOperand : Operand
    {
        public IEnumerable<Operand> Operands { get; set; }
        public LogicalOperator Operator { get; set; }
    }

    public enum LogicalOperator
    {
        And,
        Or,
    }

    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        LessThan,
        GreaterThan,
        LessThanEqual,
        GreaterThanEqual,
    }
}
