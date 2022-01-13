using System;

namespace RedisBlue
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PartitionKeyAttribute : Attribute
    {
    }
}
