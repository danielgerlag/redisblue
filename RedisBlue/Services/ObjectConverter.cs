using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class ObjectConverter : JsonConverter<object>
    {
        private const int MaxDepth = 16;

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var doc = JsonDocument.ParseValue(ref reader);
            return DeserializeElement(doc.RootElement, 0);
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static object DeserializeElement(JsonElement element, int depth)
        {
            if (depth > MaxDepth)
                throw new JsonException("Max recursion depth exeeded.");

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var objEnumerator = element.EnumerateObject();
                    var obj = new Dictionary<string, object>();
                    while (objEnumerator.MoveNext())
                        obj.Add(objEnumerator.Current.Name, DeserializeElement(objEnumerator.Current.Value, depth + 1));
                    return obj;
                case JsonValueKind.Array:
                    var arrayEnumerator = element.EnumerateArray();
                    var arr = new List<object>();
                    while (arrayEnumerator.MoveNext())
                        arr.Add(DeserializeElement(arrayEnumerator.Current, depth + 1));
                    return arr;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.GetDecimal();
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.True:
                    return true;
            }

            return null;
        }
    }
}
