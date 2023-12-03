using Common.Pooling.Pools;

using System;
using System.Diagnostics;

namespace Common.Utilities.Exceptions
{
    public static class ExceptionUtils
    {
        public static string FormatTrace(StackFrame[] trace)
        {
            var str = StringBuilderPool.Shared.Next();

            str.AppendLine($"Showing trace with {trace.Length} points;");

            for (int i = 0; i < trace.Length; i++)
            {
                var method = trace[i].GetMethod();

                if (method is null || method.DeclaringType is null)
                    continue;

                str.AppendLine(
                    $"[{i}]: {method.DeclaringType.FullName}.{method.Name}");
            }

            return StringBuilderPool.Shared.StringReturn(str);
        }

        public static string FormatException(Exception exception)
        {
            var str = StringBuilderPool.Shared.Next();

            str.AppendLine($"Showing exception of type '{exception.GetType().Name}'");
            str.AppendLine($"Status code: {exception.HResult}");
            str.AppendLine($"Status message: {exception.Message}");

            return StringBuilderPool.Shared.StringReturn(str);
        }
    }
}