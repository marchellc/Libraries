using Common.Utilities;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Common.Extensions
{
    public static class CollectionExtensions
    {
        public static bool IsMatch<TElement>(this IEnumerable<TElement> collection, IEnumerable<TElement> target)
            => IsMatch(collection, target, (element, match) =>
            {
                if (element is null && match is null)
                    return true;
                else if (element is null || match != null)
                    return false;
                else if (element != null && match is null)
                    return false;
                else if (element is IEqualityComparer comparer)
                    return comparer.Equals(element, target);
                else if (element is IEquatable<TElement> equatable)
                    return equatable.Equals(target);
                else
                    return element.Equals(target);
            });

        public static bool IsMatch<TElement>(this IEnumerable<TElement> collection, IEnumerable<TElement> target, Func<TElement, TElement, bool> evaluator)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (target is null)
                throw new ArgumentNullException(nameof(target));

            if (evaluator is null)
                throw new ArgumentNullException(nameof(evaluator));

            if (collection.Count() != target.Count())
                return false;

            for (int i = 0; i < collection.Count(); i++)
            {
                var element = collection.ElementAt(i);
                var matchElement = target.ElementAt(i);

                if (!evaluator.Call(element, matchElement))
                    return false;
            }

            return true;
        }

        public static void SetLastValue<T>(this T[] array, T value)
            => array[array.GetLastIndex()] = value;

        public static bool TryPeekLastValue<T>(this T[] array, out T value)
            => TryPeek<T>(array, array.GetLastIndex(), out value);

        public static bool TryPeek<T>(this T[] array, int index, out T value)
        {
            if (!IndexUtils.IsValidIndex(index, array.Length))
            {
                value = default;
                return false;
            }

            value = array[index];
            return true;
        }

        public static int Count(this IEnumerable objects)
        {
            var count = 0;
            var enumerator = objects.GetEnumerator();

            try
            {
                while (enumerator.MoveNext())
                    count++;
            }
            catch { }

            return count;
        }
    }
}
