using Common.Reflection;

using System;
using System.Collections.Generic;
using System.IO;

namespace Common.Extensions
{
    public static class ReaderExtensions
    {
        public static string ReadStringEx(this BinaryReader reader)
        {
            var strType = reader.ReadByte();

            if (strType == 0)
                return reader.ReadString();
            else if (strType == 1)
                return string.Empty;
            else if (strType == 2)
                return null;
            else
                throw new InvalidDataException($"Unknown string ID: {strType}");
        }

        public static DateTime ReadDate(this BinaryReader reader)
            => new DateTime(reader.ReadInt64());

        public static TimeSpan ReadTime(this BinaryReader reader)
            => new TimeSpan(reader.ReadInt64());

        public static Guid ReadGuid(this BinaryReader reader)
            => new Guid(reader.ReadArray(false, reader.ReadByte));

        public static TElement[] ReadArray<TElement>(this BinaryReader reader, bool isUnsafe = false, Func<TElement> readerStep = null)
        {
            var size = reader.ReadInt32();

            if (size <= 0)
                return Array.Empty<TElement>();

            var array = new TElement[size];

            for (int i = 0; i < size; i++)
                array[i] = isUnsafe ? readerStep() : readerStep.Call();

            return array;
        }

        public static List<TElement> ReadList<TElement>(this BinaryReader reader, Func<TElement> readerStep)
        {
            var size = reader.ReadInt32();

            if (size <= 0)
                return new List<TElement>();

            var list = new List<TElement>();

            for (int i = 0; i < size; i++)
                list.Add(readerStep.Call());

            return list;
        }

        public static Type ReadType(this BinaryReader reader)
        {
            var typeStr = reader.ReadString();
            var type = Type.GetType(typeStr);

            if (type is null)
                throw new MissingMemberException(typeStr);

            return type;
        }
    }
}