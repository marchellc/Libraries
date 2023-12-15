using Common.Extensions;

using System;
using System.Collections;
using System.Linq;

namespace Common.Utilities
{
    public static class IndexUtils
    {
        public static bool IsValidIndex(this int index, int count)
            => index < count && index > -1;

        public static bool IsEndIndex(this int index, int count)
            => index >= count;

        public static int GetLastIndex(this IEnumerable collection)
            => collection.Count() - 1;

        public static int GetLastIndex(this Array array)
            => array.Length - 1;
    }
}