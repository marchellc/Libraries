using Common.Extensions;
using Common.IO.Collections;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.IO.Data
{
    public static class DataWriterUtils
    {
        public static readonly LockedDictionary<Type, Action<DataWriter, object>> Writers = new LockedDictionary<Type, Action<DataWriter, object>>();

        public static Action<DataWriter, object> GetWriter(Type type)
        {
            if (Writers.TryGetValue(type, out var writer))
                return writer;

            MethodInfo method = null;

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                method = typeof(DataWriter).Method("WriteEnumerable").MakeGenericMethod(elementType);
            }
            else if (type.GetTypeInfo().IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    var elementType = type.GetFirstGenericType();
                    method = typeof(DataWriter).Method("WriteEnumerable").MakeGenericMethod(elementType);
                }
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var args = type.GetGenericArguments();

                    var keyType = args[0];
                    var elementType = args[1];

                    method = typeof(DataWriter).Method("WriteDictionary").MakeGenericMethod(elementType);
                }
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                var elementType = Nullable.GetUnderlyingType(type);
                method = typeof(DataWriter).Method("WriteNullable").MakeGenericMethod(elementType);
            }

            if (method != null)
                return Writers[type] = WriterMethodToInvoke(method);

            throw new InvalidOperationException($"No writers assigned for type '{type.FullName}'");
        }

        public static void WriteEnum(DataWriter writer, Enum en)
        {
            var code = en.GetTypeCode();
            var codeType = code.ToType();

            var type = en.GetType();

            writer.WriteByte((byte)code);
            writer.WriteType(type);

            var enWriter = GetWriter(codeType);
            var enNumValue = Convert.ChangeType(en, codeType);

            enWriter(writer, enNumValue);
        }

        public static Action<DataWriter, object> WriterMethodToInvoke(MethodInfo method)
        {
            if (MethodExtensions.DisableFastInvoker)
                return (writer, value) => method.Invoke(writer, new object[] { value });

            try
            {
                var invoker = MethodInvoker.GetHandler(method);

                return (writer, value) =>
                {
                    invoker(writer, value);
                };
            }
            catch
            {
                return (writer, value) => method.Invoke(writer, new object[] { value });
            }
        }
    }
}