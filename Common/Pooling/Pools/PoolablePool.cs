using Common.Pooling.Buffers;

using System;

namespace Common.Pooling.Pools
{
    public class PoolablePool<TPoolable> : IPool<TPoolable> where TPoolable : IPoolable, new()
    {
        public static PoolablePool<TPoolable> Shared { get; } 

        static PoolablePool()
        {
            Shared = new PoolablePool<TPoolable>(PoolOptions.NewOnMissing);
            PoolHelper<TPoolable>.SetPool(Shared, 15);
        }

        public PoolablePool(PoolOptions options, IPoolBuffer<TPoolable> buffer = null)
        {
            Options = options;
            Buffer = buffer ?? new PoolableBuffer<TPoolable>(this);
        }

        public PoolOptions Options { get; set; }

        public IPoolBuffer<TPoolable> Buffer { get; set; }

        public TPoolable Next()
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            var item = Buffer.Get();

            item.OnRemoved();

            return item;
        }

        public void Return(TPoolable obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            obj.OnAdded();

            Buffer.Add(obj);
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
