namespace Common.Pooling
{
    public class Poolable : IPoolable
    {
        private bool _pooled;

        public bool IsPooled => _pooled;

        public virtual void OnAdded()
            => _pooled = true;

        public virtual void OnRemoved()
            => _pooled = false;
    }
}