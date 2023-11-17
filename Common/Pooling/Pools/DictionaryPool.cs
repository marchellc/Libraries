using Common.Pooling.Buffers;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Pooling.Pools
{
    public class DictionaryPool<TKey, TElement> : IPool<Dictionary<TKey, TElement>>
    {
        public static DictionaryPool<TKey, TElement> Shared { get; }

        static DictionaryPool()
        {
            Shared = new DictionaryPool<TKey, TElement>(PoolOptions.NewOnMissing);
            PoolHelper<Dictionary<TKey, TElement>>.SetPool(Shared, 15);
        }

        public DictionaryPool(PoolOptions options, IPoolBuffer<Dictionary<TKey, TElement>> buffer = null)
        {
            Options = options;
            Buffer = buffer ?? new BasicBuffer<Dictionary<TKey, TElement>>(this, () => new Dictionary<TKey, TElement>());
        }

        public PoolOptions Options { get; set; }

        public IPoolBuffer<Dictionary<TKey, TElement>> Buffer { get; set; }

        public Dictionary<TKey, TElement> Next()
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            return Buffer.Get();
        }

        public Dictionary<TKey, TElement> Next(IEnumerable<KeyValuePair<TKey, TElement>> elements)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            var list = Buffer.Get();

            if (elements != null)
            {
                var count = elements.Count();

                if (count > 0)
                {
                    foreach (var pair in elements)
                        list[pair.Key] = pair.Value;
                }
            }

            return list;
        }

        public void Return(Dictionary<TKey, TElement> obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            obj.Clear();

            Buffer.Add(obj);
        }

        public KeyValuePair<TKey, TElement>[] ToArrayReturn(Dictionary<TKey, TElement> obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            var array = new KeyValuePair<TKey, TElement>[obj.Count];

            for (int i = 0; i < obj.Count; i++)
                array[i] = new KeyValuePair<TKey, TElement>(
                    obj.ElementAt(i).Key,
                    obj.ElementAt(i).Value);

            Return(obj);

            return array;
        }

        public void Clear()
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            Buffer.Clear();
            Buffer = null;

            Options = default;
        }

        public void Initialize(int initialSize)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            if (initialSize > 0)
            {
                for (int i = 0; i < initialSize; i++)
                {
                    Buffer.AddNew();
                }
            }
        }
    }
}