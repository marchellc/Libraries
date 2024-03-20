using Common.Extensions;
using Common.IO.Collections;

using HarmonyLib;

using System;
using System.Collections.Generic;
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

            MethodInfo method = null;

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                method = typeof(DataReader).Method("ReadArray").MakeGenericMethod(elementType);
            }
            else if (type.GetTypeInfo().IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = type.GetFirstGenericType();
                    method = typeof(DataReader).Method("ReadList").MakeGenericMethod(elementType);
                }
                else if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    var elementType = type.GetFirstGenericType();
                    method = typeof(DataReader).Method("ReadHashSet").MakeGenericMethod(elementType);
                }
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var args = type.GetGenericArguments();

                    var keyType = args[0];
                    var elementType = args[1];

                    method = typeof(DataReader).Method("ReadDictionary").MakeGenericMethod(keyType, elementType);
                }
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                var valueType = Nullable.GetUnderlyingType(type);
                method = typeof(DataReader).Method("ReadNullable").MakeGenericMethod(valueType);
            }

            if (method != null)
                return Readers[type] = ReaderMethodToDelegate(method);

            throw new InvalidOperationException($"No readers assigned for type '{type.FullName}'");
        }

        public static Enum ReadEnum(this DataReader reader)
        {
            var code = (TypeCode)reader.ReadByte();
            var type = code.ToType();
            var enType = reader.ReadType();
            var enReader = GetReader(type);
            var enNumValue = enReader(reader);

            return (Enum)Enum.ToObject(enType, enNumValue);
        }

        public static Func<DataReader, object> ReaderMethodToDelegate(MethodInfo method)
        {
            if (MethodExtensions.DisableFastInvoker)
                return reader => method.Invoke(reader, null);

            try
            {
                var invoker = MethodInvoker.GetHandler(method);
                return reader => invoker(reader);
            }
            catch
            {
                return reader => method.Invoke(reader, null);
            }
        }
    }
}