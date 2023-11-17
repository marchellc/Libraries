using System;

namespace Common.Pooling
{
    public interface IPoolBuffer<TObject>
    {
        int Size { get; }

        IPool<TObject> Pool { get; }

        Func<TObject> Constructor { get; set; }

        TObject Get();

        void Add(TObject obj);
        void AddNew();

        void Clear();
    }
}
