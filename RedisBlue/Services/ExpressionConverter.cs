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
    internal class ExpressionConverter : IExpressionConverter
    {
        public Operand Convert(Expression expression)
        {
            switch (expression)
            {
                case MethodCallExpression methodCall:

                    switch (methodCall.Method.Name)
                    {
                        case "Where":
                            var a = Convert(methodCall.Arguments[1]);
                            return a;
                        case "get_Item":
                            return new MemberOperand(GetMemberPath(methodCall));
                    }
                    throw new NotImplementedException();
                case UnaryExpression unary:
                    return Convert(unary.Operand);
                case BinaryExpression binary:

                    var left = Convert(binary.Left);
                    var right = Convert(binary.Right);
                    switch (binary.NodeType)
                    {
                        case ExpressionType.Equal:
                        case ExpressionType.NotEqual:
                        case ExpressionType.GreaterThan:
                        case ExpressionType.GreaterThanOrEqual:
                        case ExpressionType.LessThan:
                        case ExpressionType.LessThanOrEqual:
                            return new ComparisonOperand(binary.NodeType, left, right);

                        case ExpressionType.AndAlso:
                        case ExpressionType.OrElse:
                            return new LogicalOperand(binary.NodeType, left, right);
                    }
                    throw new NotImplementedException();
                case ParameterExpression parameter:
                    break;
                case ConstantExpression constant:
                    return new ConstantOperand(constant.Value);
                case LambdaExpression lambda:
                    return Convert(lambda.Body);
                case MemberExpression member:
                    return new MemberOperand(GetMemberPath(member));
            }

            throw new NotImplementedException();
        }

        private string GetMemberPath(MemberExpression member)
        {
            var path = member.Member.Name;

            while (member.Expression is not ParameterExpression)
            {
                switch (member.Expression) 
                {
                    case MemberExpression me:
                        member = me;
                        path = $"{me.Member.Name}:{path}";
                        break;
                }
            }
            
            return path;
        }

        private string GetMemberPath(MethodCallExpression methodCall)
        {
            if (methodCall.Object is not MemberExpression)
                throw new NotImplementedException();

            if (methodCall.Arguments[0] is not ConstantExpression)
                throw new NotImplementedException();

            var obj = (MemberExpression)methodCall.Object;
            var key = (ConstantExpression)methodCall.Arguments[0];

            return $"{GetMemberPath(obj)}:{key.Value}";
        }
    }
}
