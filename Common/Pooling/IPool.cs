namespace Common.Pooling
{
    public interface IPool<TObject>
    {
        PoolOptions Options { get; set; }

        IPoolBuffer<TObject> Buffer { get; set; }

        TObject Next();

        void Return(TObject obj);

        void Initialize(int initialSize);
        void Clear();
    }
}