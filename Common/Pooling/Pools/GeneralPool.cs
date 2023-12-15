using Common.Pooling.Buffers;
using Common.Extensions;

using System;

namespace Common.Pooling.Pools
{
    public class GeneralPool<TObject> : IPool<TObject>
    {
        public static GeneralPool<TObject> Shared { get; }

        static GeneralPool()
        {
            Shared = new GeneralPool<TObject>(PoolOptions.NewOnMissing, null, Activator.CreateInstance<TObject>);
            PoolHelper<TObject>.SetPool(Shared);
        }

        public GeneralPool(PoolOptions options, IPoolBuffer<TObject> buffer = null, Func<TObject> constructor = null)
        {
            Options = options;
            Buffer = buffer ?? new BasicBuffer<TObject>(this, constructor);
        }

        public PoolOptions Options { get; set; }

        public IPoolBuffer<TObject> Buffer { get; set; }

        public TObject Next()
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            return Buffer.Get();
        }

        public void Return(TObject obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            Buffer.Add(obj);
        }

        public void Return(TObject obj, Action<TObject> dispose)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"This pool's buffer is missing");

            dispose.Call(obj);

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