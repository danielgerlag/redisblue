using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Models
{
    internal abstract class ResolverResult
    {
    }

    internal class SetKeyResult : ResolverResult
    {
        public SetKeyResult(RedisKey key, bool isTemp)
        {
            Key = key;
            IsTemp = isTemp;
        }

        public RedisKey Key { get; set; }
        public bool IsTemp { get; set; }
    }

    internal class MemberResult : ResolverResult
    {
        public MemberResult(string path, Expression expression)
        {
            Path = path;
            Expression = expression;
        }
        public Expression Expression { get; set; }
        public string Path { get; set; }
    }

    internal class ValueResult : ResolverResult
    {
        public ValueResult(object value)
        {
            Value = value;
        }

        public object Value { get; set; }
    }
}
