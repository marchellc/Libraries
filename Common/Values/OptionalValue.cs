namespace Common.Values
{
    public struct OptionalValue<TValue> : IGetValue<TValue>
    {
        public OptionalValue(TValue value, bool hasValue)
        {
            Value = value;
            HasValue = hasValue;
        }

        public OptionalValue(TValue value) : this(value, value != null) { }

        public TValue Value { get; }
        public bool HasValue { get; }
    }
}