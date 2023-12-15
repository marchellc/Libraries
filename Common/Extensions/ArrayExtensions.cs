using System;
using System.Collections;
using System.Linq;

namespace Common.Extensions
{
    public static class ArrayExtensions
    {
        public static TValue[] CastArray<TValue>(this IEnumerable objects)
            => objects.Cast<TValue>().ToArray();

        public static void Add<TValue>(ref TValue[] array, TValue value)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));

            var newArray = new TValue[array.Length + 1];

            for (int i = 0; i < array.Length; i++)
                newArray[i] = array[i];

            newArray.SetLastValue(value);
        }

        public static void Remove<TValue>(ref TValue[] array, TValue value)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));

            array = array.Where(item => !(item?.Equals(value) ?? false)).ToArray();
        }
    }
}