using Common.Extensions;

using System;

namespace Common.Pooling
{
    public static class PoolHelper<TObject>
    {
        public static IPool<TObject> Pool { get; private set; }
        public static IPoolBuffer<TObject> Buffer { get => Pool?.Buffer ?? null; set => Pool!.Buffer = value; }

        public static PoolOptions Options { get => Pool?.Options ?? PoolOptions.None; set => Pool!.Options = value; }

        public static TObject Next
        {
            get
            {
                if (Pool is null)
                    return default;

                return Pool.Next();
            }
        }

        public static event Action OnPoolCreated;
        public static event Action OnPoolDestroyed;

        public static void SetPool(IPool<TObject> pool, int size = 0)
        {
            RemovePool();

            Pool = pool;
            Pool.Initialize(size);

            OnPoolCreated.Call();
        }

        public static void RemovePool()
        {
            if (Pool != null)
            {
                Pool.Clear();
                Pool = null;

                OnPoolDestroyed.Call();
            }
        }

        public static void Return(TObject obj)
            => Pool?.Return(obj);
    }
}