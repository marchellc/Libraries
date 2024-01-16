using Common.Pooling;

using Networking.Data;

using System.Text;

namespace Networking.Pooling
{
    public class ReaderPool : Pool<Reader>
    {
        public static ReaderPool Shared { get; } = new ReaderPool(32);

        public ReaderPool(uint size) : base(size) { }

        public Encoding Encoding { get; set; } = Encoding.Default;

        public override Reader Construct()
            => new Reader(Encoding);

        public Reader Rent(byte[] data)
        {
            var reader = Rent();
            reader.SetData(data);
            return reader;
        }

        public override void OnReturning(Reader value)
            => value.OnDispose();
    }
}
