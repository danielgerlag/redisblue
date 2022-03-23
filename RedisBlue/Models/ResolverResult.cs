using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Models
{
    internal abstract class ResolverResult
    {
    }

    internal class SetKeyResult : ResolverResult
    {
        public SetKeyResult(RedisKey key)
        {
            Key = key;
        }

        public RedisKey Key { get; set; }
    }

    internal class MemberResult : ResolverResult
    {
        public MemberResult(string path)
        {
            Path = path;
        }

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
