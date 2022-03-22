using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedisBlue.Models
{
    internal class TypeCacheInfo
    {
        public readonly Dictionary<string, PropertyCacheInfo> IndexProperties = new();
        public readonly MethodInfo KeyGetMethod;
        public readonly MethodInfo PartitionKeyGetMethod;
        public readonly bool IsLeafValue;
        public readonly bool IsCollection;
        public readonly bool IsDictionary;

        public TypeCacheInfo(Type type)
        {
            var ifaces = type.GetInterfaces();
            IsDictionary = ifaces.Contains(typeof(IDictionary));
            IsCollection = ifaces.Contains(typeof(IEnumerable)) && type != typeof(string);
            IsLeafValue = (type.IsValueType || type.IsPrimitive || type == typeof(string));

            foreach (var prop in type.GetProperties())
            {
                var indexAttr = prop.GetCustomAttribute<IndexAttribute>();
                if (indexAttr != null)
                {
                    IndexProperties.Add(prop.Name, new PropertyCacheInfo(prop));
                }

                var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
                if (keyAttr != null)
                {
                    KeyGetMethod = prop.GetGetMethod();
                    IndexProperties.Add(prop.Name, new PropertyCacheInfo(prop));
                }

                var partitionKeyAttr = prop.GetCustomAttribute<PartitionKeyAttribute>();
                if (partitionKeyAttr != null)
                {
                    PartitionKeyGetMethod = prop.GetGetMethod();
                }

            }
        }
    }
}
