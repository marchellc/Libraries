using Common.Pooling;
using Common.Pooling.Buffers;

using Networking.Data;

using System;

namespace Networking.Pooling
{
    public class ReaderPool : IPool<Reader>
    {
        public PoolOptions Options { get; set; }
        public IPoolBuffer<Reader> Buffer { get; set; }

        public ReaderPool()
        {
            Options = PoolOptions.NewOnMissing;
            Buffer = new BasicBuffer<Reader>(this, () => new Reader());
        }

        public Reader Next(byte[] data)
        {
            var reader = Buffer.Get();

            reader.pool = this;
            reader.FromPool(data);

            return reader;
        }

        public void Return(Reader obj)
        {
            obj.ToPool();
            obj.pool = this;

            Buffer.Add(obj);
        }

        public void Initialize(int initialSize)
        {
            for (int i = 0; i < initialSize; i++)
                Buffer.AddNew();
        }

        public void Clear()
            => Buffer.Clear();

        public Reader Next() => throw new InvalidOperationException($"You must use the method with the byte[] overload to get a Reader");
    }
}
