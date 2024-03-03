using Common.Extensions;
using Common.IO.Collections;
using Common.Pooling.Pools;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Caching
{
    public class MemoryCache<T> : ICache<T>
    {
        private readonly LockedList<T> cache = new LockedList<T>();

        public int Size => cache.Count;

        public bool Add(T value)
        {
            if (cache.Contains(value))
                return false;

            cache.Add(value);
            return true;
        }

        public T Find(Func<T, bool> predicate)
        {
            foreach (var value in cache)
            {
                if (predicate.Call(value))
                    return value;
            }

            return default;
        }

        public T[] FindAll(Func<T, bool> predicate)
        {
            var list = ListPool<T>.Shared.Rent();

            foreach (var value in cache)
            {
                if (predicate.Call(value))
                    list.Add(value);
            }

            return ListPool<T>.Shared.ToArrayReturn(list);
        }

        public T[] GetAll()
            => cache.ToArray();

        public bool TryFind(Func<T, bool> predicate, out T value)
        {
            foreach (var cachedValue in cache)
            {
                if (predicate.Call(cachedValue))
                {
                    value = cachedValue;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public int RemoveAll(Func<T, bool> predicate)
            => cache.List.RemoveAll(value => predicate.Call(value));

        public int RemoveAll(IEnumerable<T> values)
            => cache.List.RemoveAll(value => values.Contains(value));

        public bool Contains(T value)
            => cache.Contains(value);

        public bool Remove(T value)
            => cache.Remove(value);
    }
}