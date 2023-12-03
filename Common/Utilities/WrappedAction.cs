using System;

namespace Common.Utilities
{
    public struct WrappedAction<TDelegate> where TDelegate : Delegate
    {
        public Type Type;
        public Action<object> Target;
        public TDelegate Proxy;
    }
}
