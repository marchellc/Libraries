namespace Common.Values
{
    public class ReferenceValue<TValue> : IValue<TValue>
    {
        public TValue Value { get; set; }

        public ReferenceValue(TValue value)
        {
            Value = value;
        }
    }
}