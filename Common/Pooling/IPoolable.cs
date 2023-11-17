namespace Common.Pooling
{
    public interface IPoolable
    {
        public bool IsPooled { get; }

        void OnRemoved();
        void OnAdded();
    }
}