using System;

namespace Common.Values
{
    public class StatusValue<T> : IValue<T>
    {
        private Func<T> origValueGetter;
        private T overridenValue;

        public StatusValue(Func<T> origValueGetter)
        {
            this.origValueGetter = origValueGetter;
        }

        public T Value
        {
            get => IsActive ? overridenValue : origValueGetter();
            set
            {
                overridenValue = value;
                IsActive = true;
            }
        }

        public bool IsActive { get; private set; }

        public void Reset()
        {
            overridenValue = default;
            IsActive = false;
        }
    }
}