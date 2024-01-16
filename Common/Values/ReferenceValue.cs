using System;

namespace Common.Values
{
    public class ReferenceValue<TValue> : IValue<TValue>
    {
        public TValue Value { get; set; }

        public Type Type { get; }

        public ReferenceValue(TValue value)
        {
            Value = value;
            Type = typeof(TValue);
        }
    }
}