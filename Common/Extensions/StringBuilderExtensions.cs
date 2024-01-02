using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Extensions
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendLines<TValue>(this StringBuilder builder, IEnumerable<TValue> objects, Func<TValue, string> parser = null, string nullObj = "null")
        {
            objects.ForEach(obj =>
            {
                if (obj is null)
                {
                    builder.AppendLine(nullObj);
                    return;
                }

                if (parser != null)
                {
                    builder.AppendLine(parser(obj));
                    return;
                }

                builder.AppendLine(obj.ToString());
            });

            return builder;
        }
    }
}