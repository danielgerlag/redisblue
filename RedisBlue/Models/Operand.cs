using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Models
{
    public abstract class Operand
    {

    }

    public class ConstantOperand : Operand
    {
        public ConstantOperand(object value)
        {
            Value = value;
        }

        public object Value { get; private set; }
    }

    public class MemberOperand : Operand
    {
        public MemberOperand(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }
    }

    public class ComparisonOperand : Operand
    {
        public ComparisonOperand(ComparisonOperator @operator, Operand left, Operand right)
        {
            Operator = @operator;
            Left = left;
            Right = right;
        }

        public ComparisonOperand(ExpressionType expressionType, Operand left, Operand right)
        {
            Operator = expressionType switch
            {
                ExpressionType.Equal => ComparisonOperator.Equal,
                ExpressionType.NotEqual => ComparisonOperator.NotEqual,
                ExpressionType.GreaterThan => ComparisonOperator.GreaterThan,
                ExpressionType.GreaterThanOrEqual => ComparisonOperator.GreaterThanEqual,
                ExpressionType.LessThan => ComparisonOperator.LessThan,
                ExpressionType.LessThanOrEqual => ComparisonOperator.LessThanEqual,
                _ => throw new NotImplementedException(),
            };
            Left = left;
            Right = right;
        }

        

        public Operand Left { get; set; }
        public Operand Right { get; set; }
        public ComparisonOperator Operator { get; set; }
    }

    public class LogicalOperand : Operand
    {
        public LogicalOperand(ExpressionType expressionType, params Operand[] operands)
        {
            Operator = expressionType switch
            {
                ExpressionType.AndAlso => LogicalOperator.And,
                ExpressionType.OrElse => LogicalOperator.Or,                
                _ => throw new NotImplementedException(),
            };
            Operands = operands;
        }

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
