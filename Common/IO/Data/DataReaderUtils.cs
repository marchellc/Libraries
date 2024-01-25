using Common.Extensions;
using Common.IO.Collections;

using HarmonyLib;

using System;
using System.Reflection;

namespace Common.IO.Data
{
    public static class DataReaderUtils
    {
        public static readonly LockedDictionary<Type, Func<DataReader, object>> Readers = new LockedDictionary<Type, Func<DataReader, object>>();

        public static Func<DataReader, object> GetReader(Type type)
        {
            if (Readers.TryGetValue(type, out var reader))
                return reader;

            throw new InvalidOperationException($"No readers assigned for type '{type.FullName}'");
        }

        public static Enum ReadEnum(this DataReader reader)
        {
            var code = (TypeCode)reader.ReadByte();
            var type = code.ToType();
            var enType = reader.ReadType();
            var enReader = GetReader(type);
            var enNumValue = enReader(reader);

            return (Enum)Convert.ChangeType(enNumValue, enType);
        }

        public static Func<DataReader, object> ReaderMethodToDelegate(MethodInfo method)
        {
            var invoker = MethodInvoker.GetHandler(method);
            return reader => invoker(reader);
        }
    }
}