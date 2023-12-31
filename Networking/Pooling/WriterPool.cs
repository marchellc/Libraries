using Common.Pooling;
using Common.Pooling.Buffers;

using Networking.Data;
using Networking.Interfaces;

using System;

namespace Networking.Pooling
{
    public class WriterPool : IPool<Writer>
    {
        public PoolOptions Options { get; set; }
        public IPoolBuffer<Writer> Buffer { get; set; } 

        public WriterPool(ITypeLibrary typeLibrary)
        {
            if (typeLibrary is null)
                throw new ArgumentNullException(nameof(typeLibrary));

            Options = PoolOptions.NewOnMissing;
            Buffer = new BasicBuffer<Writer>(this, () => new Writer(typeLibrary));
        }

        public Writer Next()
        {
            var writer = Buffer.Get();

            writer.pool = this;
            writer.FromPool();

            return writer;
        }

        public void Return(Writer obj)
        {
            obj.ToPool();
            obj.pool = this;

            Buffer.Add(obj);
        }

        public void Clear()
            => Buffer.Clear();

        public void Initialize(int initialSize)
        {
            for (int i = 0; i < initialSize; i++)
                Buffer.AddNew();
        }
    }
}
