namespace Networking.Requests
{
    public class AsyncResponseInfo<T>
    {
        public readonly ResponseInfo response;
        public readonly T value;

        public AsyncResponseInfo(ResponseInfo response, T value)
        {
            this.response = response;
            this.value = value;
        }
    }
}