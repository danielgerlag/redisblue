using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace RedisBlue.Models
{
    internal class PropertyCacheInfo
    {
        public readonly MethodInfo GetMethod;
        public readonly bool IsCollection;
        public readonly bool IsDictionary;
        public readonly bool IsObject;

        public PropertyCacheInfo(PropertyInfo info)
        {
            GetMethod = info.GetMethod;
            var ifaces = info.PropertyType.GetInterfaces();

            IsDictionary = ifaces.Contains(typeof(IDictionary));
            IsCollection = ifaces.Contains(typeof(IEnumerable)) && info.PropertyType != typeof(string);
            IsObject = !info.PropertyType.IsValueType && info.PropertyType != typeof(string);
        }
    }
}
