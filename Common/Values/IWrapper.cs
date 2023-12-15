namespace Common.Values
{
    public interface IWrapper<TValue>
    {
        public TValue Base { get; }
    }
}