using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using HarmonyLib;

using System;
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
            var invoker = MethodInvoker.GetHandler(method);

            return (writer, value) =>
            {
                invoker(writer, value);
            };
        }
    }
}