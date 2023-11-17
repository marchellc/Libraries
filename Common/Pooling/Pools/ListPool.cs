using Common.Pooling.Buffers;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Pooling.Pools
{
    public class ListPool<TElement> : IPool<List<TElement>>
    {
        public static ListPool<TElement> Shared { get; } 

        static ListPool()
        {
            Shared = new ListPool<TElement>(PoolOptions.NewOnMissing);
            PoolHelper<List<TElement>>.SetPool(Shared, 15);
        }

        public ListPool(PoolOptions options, IPoolBuffer<List<TElement>> buffer = null)
        {
            Options = options;
            Buffer = buffer ?? new BasicBuffer<List<TElement>>(this, () => new List<TElement>());
        }

        public PoolOptions Options { get; set; }

        public IPoolBuffer<List<TElement>> Buffer { get; set; }

        public List<TElement> Next()
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            return Buffer.Get();
        }

        public List<TElement> Next(int size)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            var list = Buffer.Get();

            if (list.Capacity < size)
                list.Capacity = size;

            return list;
        }

        public List<TElement> Next(IEnumerable<TElement> elements)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            var list = Buffer.Get();

            if (elements != null)
            {
                var count = elements.Count();

                if (count > 0)
                {
                    if (list.Capacity < count)
                        list.Capacity = count;

                    list.AddRange(elements);
                }
            }

            return list;
        }

        public void Return(List<TElement> obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            obj.Clear();

            Buffer.Add(obj);
        }

        public TElement[] ToArrayReturn(List<TElement> obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            var array = new TElement[obj.Count];

            for (int i = 0; i < obj.Count; i++)
                array[i] = obj[i];

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