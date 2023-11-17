using Common.Pooling.Buffers;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Pooling.Pools
{
    public class HashSetPool<TElement> : IPool<HashSet<TElement>>
    {
        public static HashSetPool<TElement> Shared { get; } 

        static HashSetPool()
        {
            Shared = new HashSetPool<TElement>(PoolOptions.NewOnMissing);
            PoolHelper<HashSet<TElement>>.SetPool(Shared, 15);
        }

        public HashSetPool(PoolOptions options, IPoolBuffer<HashSet<TElement>> buffer = null)
        {
            Options = options;
            Buffer = buffer ?? new BasicBuffer<HashSet<TElement>>(this, () => new HashSet<TElement>());
        }

        public PoolOptions Options { get; set; }

        public IPoolBuffer<HashSet<TElement>> Buffer { get; set; }

        public HashSet<TElement> Next()
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            return Buffer.Get();
        }

        public HashSet<TElement> Next(IEnumerable<TElement> elements)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            var list = Buffer.Get();

            if (elements != null)
            {
                var count = elements.Count();

                if (count > 0)
                {
                    foreach (var element in elements)
                        list.Add(element);
                }
            }

            return list;
        }

        public void Return(HashSet<TElement> obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            obj.Clear();

            Buffer.Add(obj);
        }

        public TElement[] ToArrayReturn(HashSet<TElement> obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            var array = new TElement[obj.Count];

            for (int i = 0; i < obj.Count; i++)
                array[i] = obj.ElementAt(i);

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