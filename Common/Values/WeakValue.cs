using System;

namespace Common.Values
{
    public class WeakValue<TValue> : IValue<TValue> where TValue : class
    {
        private WeakReference<TValue> _weakRef;

        public TValue Value
        {
            get => _weakRef != null && _weakRef.TryGetTarget(out var value) ? value : null;
            set => _weakRef?.SetTarget(value);
        }

        public bool IsAlive
        {
            get => _weakRef != null && _weakRef.TryGetTarget(out _);
        }

        public WeakValue(bool trackValue = true)
            => _weakRef = new WeakReference<TValue>(null, trackValue);

        public WeakValue(TValue value, bool trackValue = true)
            => _weakRef = new WeakReference<TValue>(value, trackValue);
    }
}