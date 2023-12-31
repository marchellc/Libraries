using System;
using System.Collections.Generic;

namespace Common.Extensions
{
    public static class DictionaryExtensions
    {
        public static bool ContainsValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TValue value)
            => dict.Values.Contains(value);

        public static bool TryGetKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TValue value, out TKey key)
        {
            if (dict is null)
                throw new ArgumentNullException(nameof(dict));

            if (value is null)
                throw new ArgumentNullException(nameof(value));

            foreach (var pair in dict)
            {
                if (pair.Value.Equals(value))
                {
                    key = pair.Key;
                    return true;
                }
            }

            key = default;
            return false;
        }
    }
}