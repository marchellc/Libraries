using Common.Reflection;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Common.IO.Collections
{
    public class LockedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private volatile object dictLock;
        private volatile Dictionary<TKey, TValue> dict;

        public LockedDictionary()
        {
            dictLock = new object();
            dict = new Dictionary<TKey, TValue>();
        }

        public LockedDictionary(int size)
        {
            dictLock = new object();
            dict = new Dictionary<TKey, TValue>(size);
        }

        public LockedDictionary(IDictionary<TKey, TValue> pairs)
        {
            dictLock = new object();
            dict = new Dictionary<TKey, TValue>(pairs);
        }

        public LockedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            dictLock = new object();
            dict = new Dictionary<TKey, TValue>();

            foreach (var pair in pairs)
                Add(pair);
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (dictLock)
                {
                    if (dict.TryGetValue(key, out var value))
                        return value;

                    return default;
                }
            }
            set
            {
                lock (dictLock)
                {
                    dict[key] = value;
                }
            }
        }


        public ICollection<TKey> Keys => dict.Keys;
        public ICollection<TValue> Values => dict.Values;

        public int Count => dict.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            lock (dictLock)
                dict[key] = value;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            lock (dictLock)
                dict[item.Key] = item.Value;
        }

        public void Clear()
        {
            lock (dictLock)
                dict.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (dictLock)
                return dict.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            lock (dictLock)
                return dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (dictLock)
                dict.ToArray().CopyTo(array, arrayIndex);
        }

        public bool Remove(TKey key)
        {
            lock (dictLock)
                return dict.Remove(key);   
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            lock (dictLock)
                return dict.Remove(item.Key);
        }

        public Dictionary<TKey, TValue> RemoveRange(Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            lock (dictLock)
            {
                var toRemove = new Dictionary<TKey, TValue>();

                foreach (var item in dict)
                {
                    if (predicate.Call(item))
                        toRemove[item.Key] = item.Value;
                }

                foreach (var item in toRemove)
                    dict.Remove(item.Key);

                return toRemove;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (dictLock)
                return dict.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dict.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => dict.GetEnumerator();
    }
}
