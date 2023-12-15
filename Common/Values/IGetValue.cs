namespace Common.Values
{
    public interface IGetValue<TValue>
    {
        TValue Value { get; }
    }
}