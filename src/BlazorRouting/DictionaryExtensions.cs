using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlazorRouting
{
    internal static class DictionaryExtensions
    {
        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
            this Dictionary<TKey, TValue>? left,
            Dictionary<TKey, TValue>? right,
            Func<TKey, TValue, TValue, TValue> resolve)
        {
            return Merge(left, right, resolve, EqualityComparer<TKey>.Default);
        }

        public static Dictionary<TKey,TValue> Merge<TKey,TValue>(
            this Dictionary<TKey, TValue>? left,
            Dictionary<TKey, TValue>? right,
            Func<TKey,TValue,TValue,TValue> resolve,
            IEqualityComparer<TKey> equalityComparer)
        {
            if (left == null && right == null) return new Dictionary<TKey, TValue>(equalityComparer);
            if (left == null) return new Dictionary<TKey, TValue>(right, equalityComparer);

            var result = new Dictionary<TKey, TValue>(left, equalityComparer);
            if (right == null) return result;

            foreach(var entry in right.Keys)
            {
                if (left.ContainsKey(entry))
                {
                    result[entry] = resolve(entry, result[entry], right[entry]);
                }
                else
                {
                    result[entry] = right[entry];
                }
            }

            return result;
        }
    }
}
