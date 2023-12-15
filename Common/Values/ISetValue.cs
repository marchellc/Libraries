namespace Common.Values
{
    public interface ISetValue<TValue>
    {
        TValue Value { set; }
    }
}