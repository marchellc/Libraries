using Common.Pooling;

using Networking.Data;

using System.Text;

namespace Networking.Pooling
{
    public class WriterPool : Pool<Writer>
    {
        public static WriterPool Shared { get; } = new WriterPool(32);

        public WriterPool(uint size) : base(size) { }

        public Encoding Encoding { get; set; } = Encoding.Default;

        public override Writer Construct()
            => new Writer(Encoding);

        public override void OnReturning(Writer value)
            => value.OnDispose();

        public override void OnRenting(Writer value)
            => value.EnsureBuffer();
    }
}