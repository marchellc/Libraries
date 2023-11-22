using Common.Reflection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common.Extensions
{
    public static class BinaryWriterExtensions
    {
        public static void WriteString(this BinaryWriter writer, string value)
        {
            if (value is null)
            {
                writer.Write(2);
                return;
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                writer.Write(1);
                return;
            }
            else
            {
                writer.Write(0);
                writer.Write(value);
                return;
            }
        }

        public static void WriteDate(this BinaryWriter writer, DateTime value)
            => writer.Write(value.Ticks);

        public static void WriteTime(this BinaryWriter writer, TimeSpan value)
            => writer.Write(value.Ticks);

        public static void WriteItems<TItem>(this BinaryWriter writer, IEnumerable<TItem> items, Action<TItem> writerStep)
        {
            var size = items?.Count() ?? 0;

            writer.Write(size);

            if (size <= 0)
                return;

            foreach (var item in items)
                writerStep.Call(item);
        }
    }
}
