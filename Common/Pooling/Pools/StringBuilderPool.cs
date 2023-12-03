using Common.Pooling.Buffers;

using System;
using System.Text;

namespace Common.Pooling.Pools
{
    public class StringBuilderPool : IPool<StringBuilder>
    {
        public static StringBuilderPool Shared { get; } = new StringBuilderPool(null, PoolOptions.NewOnMissing);

        public PoolOptions Options { get; set; }

        public IPoolBuffer<StringBuilder> Buffer { get; set; }

        public StringBuilderPool(IPoolBuffer<StringBuilder> buffer, PoolOptions options)
        {
            Options = options;
            Buffer = buffer ?? new BasicBuffer<StringBuilder>(this, () => new StringBuilder());
        }

        public void Clear()
        {
            if (Buffer is null)
                throw new InvalidOperationException($"Pool buffer is null");
        }

        public void Initialize(int initialSize)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"Pool buffer is null");

            if (initialSize < 1)
                throw new ArgumentOutOfRangeException(nameof(initialSize));

            for (int i = 0; i < initialSize; i++)
                Buffer.AddNew();
        }

        public StringBuilder Next()
        {
            if (Buffer is null)
                throw new InvalidOperationException($"Pool buffer is null");

            return Buffer.Get();
        }

        public StringBuilder NextLines(params string[] lines)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"Pool buffer is null");

            var next = Buffer.Get();

            for (int i = 0; i < lines.Length; i++)
                next.AppendLine(lines[i]);

            return next;
        }

        public void Return(StringBuilder obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"Pool buffer is null");

            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            obj.Clear();

            Buffer.Add(obj);
        }

        public string StringReturn(StringBuilder obj)
        {
            if (Buffer is null)
                throw new InvalidOperationException($"Pool buffer is null");

            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            var str = obj.ToString();

            obj.Clear();

            Buffer.Add(obj);

            return str;
        }
    }
}
