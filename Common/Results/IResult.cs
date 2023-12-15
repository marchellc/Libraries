namespace Common.Results
{
    public interface IResult
    {
        bool IsSuccess { get; }

        object Result { get; }
    }
}