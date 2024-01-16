using Common.Extensions;
using Common.Pooling.Pools;

using Fasterflect;

using System.Reflection;

namespace Common.Pooling
{
    public class PoolableItem
    {
        private readonly object poolInstance;
        private readonly MethodInfo poolMethod;

        public PoolableItem()
        {
            var thisType = this.GetType();
            var poolType = typeof(PoolablePool<>).MakeGenericType(thisType);

            poolInstance = poolType.Property("Shared").GetValueFast<object>();
            poolMethod = Extensions.TypeExtensions.Method(poolType, "Return");
        }

        internal bool isPooled;

        public bool IsPooled
        {
            get => isPooled;
        }

        public virtual void OnPooled() { }
        public virtual void OnUnPooled() { }

        public void ToPool()
        {
            if (isPooled)
                return;

            poolMethod.Call(poolInstance, this);
        }
    }
}