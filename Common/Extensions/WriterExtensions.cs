using System;
using System.Collections.Generic;
using System.IO;

namespace Common.Extensions
{
    public static class WriterExtensions
    {
        public static void WriteString(this BinaryWriter writer, string value)
        {
            if (value is null)
            {
                writer.Write((byte)2);
                return;
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                writer.Write((byte)1);
                return;
            }
            else
            {
                writer.Write((byte)0);
                writer.Write(value);
                return;
            }
        }

        public static void WriteDate(this BinaryWriter writer, DateTime value)
            => writer.Write(value.Ticks);

        public static void WriteTime(this BinaryWriter writer, TimeSpan value)
            => writer.Write(value.Ticks);

        public static void WriteType(this BinaryWriter writer, Type type)
            => writer.Write(type.AssemblyQualifiedName);

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
