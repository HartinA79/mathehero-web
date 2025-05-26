using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatheHero.Shared.Helper
{
    public static class FirestoreHelper
    {
        public static Dictionary<string, object> ToFirestoreFields(Dictionary<string, object> data)
        {
            var fields = new Dictionary<string, object>();
            foreach (var kvp in data)
            {
                fields[kvp.Key] = WrapValue(kvp.Value);
            }
            return fields;
        }

        public static Dictionary<string, object> ToFirestoreMap(Dictionary<string, int> map)
        {
            var wrappedMap = new Dictionary<string, object>();
            foreach (var kvp in map)
            {
                wrappedMap[kvp.Key] = new Dictionary<string, object> { { "integerValue", kvp.Value.ToString() } };
            }
            return new Dictionary<string, object> { { "mapValue", new Dictionary<string, object> { { "fields", wrappedMap } } } };
        }

        public static Dictionary<string, object> ToFirestoreArray(List<string> list)
        {
            var values = list.Select(item => new Dictionary<string, object> { { "stringValue", item } }).ToList<object>();
            return new Dictionary<string, object> { { "arrayValue", new Dictionary<string, object> { { "values", values } } } };
        }

        private static Dictionary<string, object> WrapValue(object value)
        {
            switch (value)
            {
                case string s:
                    return new Dictionary<string, object> { { "stringValue", s } };
                case int i:
                    return new Dictionary<string, object> { { "integerValue", i.ToString() } };
                case bool b:
                    return new Dictionary<string, object> { { "booleanValue", b } };
                case Dictionary<string, object> d:
                    return new Dictionary<string, object> { { "mapValue", new Dictionary<string, object> { { "fields", d } } } };
                default:
                    throw new InvalidOperationException($"Unsupported Firestore value type: {value.GetType()}");
            }
        }
    }
}
