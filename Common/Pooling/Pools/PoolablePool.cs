using Common.Extensions;

using System;

namespace Common.Pooling.Pools
{
    public class PoolablePool<T> : Pool<T> 
        where T : PoolableItem
    {
        private static readonly Type cachedType = typeof(T);

        public static PoolablePool<T> Shared { get; } = new PoolablePool<T>(64);

        public PoolablePool(uint size) : base(size) { }

        public override T Construct()
            => cachedType.Construct() as T;

        public override void OnRenting(T value)
        {
            value.isPooled = false;
            value.OnUnPooled();
        }

        public override void OnReturning(T value)
        {
            value.isPooled = false;
            value.OnPooled();
        }
    }
}