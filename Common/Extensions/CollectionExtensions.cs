﻿using Common.Utilities;
using Common.Pooling.Pools;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class CollectionExtensions
    {
        public static void ReturnToShared<T>(this List<T> list)
            => ListPool<T>.Shared.Return(list);

        public static void ReturnToShared<T>(this HashSet<T> set)
            => HashSetPool<T>.Shared.Return(set);

        public static void ExternalModify<T>(this IList<T> collection, Action<T, IList<T>> action)
        {
            var copy = ListPool<T>.Shared.Rent(collection);

            foreach (var item in copy)
                action.Call(item, collection);

            copy.ReturnToShared();
        }

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> values)
        {
            var list = new List<T>();

            await foreach (var tValue in values)
                list.Add(tValue);

            return list;
        }

        public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> values)
        {
            var list = new List<T>();

            await foreach (var tValue in values)
                list.Add(tValue);

            return list.ToArray();
        }

        public static async Task<HashSet<T>> ToHashSetAsync<T>(this IAsyncEnumerable<T> values)
        {
            var set = new HashSet<T>();

            await foreach (var tValue in values)
                set.Add(tValue);

            return set;
        }

        public static void Shuffle<T>(this ICollection<T> source)
        {
            var copy = ListPool<T>.Shared.Rent(source);
            var size = copy.Count;

            while (size > 1)
            {
                size--;

                var index = Generator.Instance.GetInt32(0, size + 1);
                var value = copy.ElementAt(index);

                copy[index] = copy[size];
                copy[size] = value;
            }

            source.Clear();

            foreach (var value in copy) 
                source.Add(value);

            copy.ReturnToShared();
        }

        public static void AddRange(this IList destination, IEnumerable source)
        {
            foreach (var item in source)
            {
                destination.Add(item);
            }
        }

        public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                destination.Add(item);
            }
        }

        public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source, Func<T, bool> condition)
        {
            foreach (var item in source)
            {
                if (!condition(item))
                    continue;

                destination.Add(item);
            }
        }

        public static TElement ElementOfIndex<TElement>(this IEnumerable collection, int index)
        {
            var obj = collection.ElementOfIndex(index);

            if (obj != null && obj is TElement element)
                return element;

            return default;
        }

        public static object ElementOfIndex(this IEnumerable collection, int index)
        {
            var curIndex = 0;
            var enumerator = collection.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (index == curIndex)
                    return enumerator.Current;
                else
                    continue;
            }

            return null;
        }

        public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
        {
            foreach (var value in values)
                action?.Invoke(value);
        }

        public static void ForEach<T>(this IEnumerable<T> values, Action<T> action, Func<T, bool> match)
        {
            foreach (var value in values)
            {
                if (match(value))
                    action(value);
            }
        }

        public static void For<T>(this IEnumerable<T> values, Action<int, T> action)
        {
            var index = 0;

            foreach (var value in values)
            {
                action(index, value);
                index++;
            }
        }

        public static void For<T>(this IEnumerable<T> values, Action<int, T> action, Func<T, int, bool> match)
        {
            var index = 0;

            foreach (var value in values)
            {
                if (!match(value, index))
                {
                    index++;
                    continue;
                }

                action(index, value);
                index++;
            }
        }

        public static bool TryGetFirst<T>(this IEnumerable<T> values, Func<T, bool> predicate, out T value)
        {
            if (predicate is null)
                throw new ArgumentNullException($"This method requires the predicate to be present!");

            foreach (var val in values)
            {
                if (predicate(val))
                {
                    value = val;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static bool TryGetFirst<T>(this IEnumerable values, Func<T, bool> predicate, out T value)
        {
            if (predicate is null)
                throw new ArgumentNullException($"This method requires the predicate to be present!");

            foreach (var val in values)
            {
                if (val is null)
                    continue;

                if (!(val is T t))
                    continue;

                if (t is null)
                    continue;

                if (!predicate(t))
                    continue;

                value = t;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetFirst<T>(this IEnumerable values, out T value)
        {
            foreach (var val in values)
            {
                if (val is null)
                    continue;

                if (!(val is T t))
                    continue;

                if (t is null)
                    continue;

                value = t;
                return true;
            }

            value = default;
            return false;
        }

        public static List<T> Where<T>(this IEnumerable collection, bool addNull = false, Func<T, bool> predicate = null)
        {
            var list = new List<T>();

            foreach (var obj in collection)
            {
                if (obj is null)
                {
                    if (addNull)
                        list.Add(default);
                    else
                        continue;
                }

                if (!(obj is T t))
                    continue;

                if (t is null)
                    continue;

                if (predicate != null && !predicate(t))
                    continue;

                list.Add(t);
            }

            return list;
        }

        public static List<TType> WhereNot<TFilter, TType>(this IEnumerable collection, bool addNull = false, Func<TType, bool> predicate = null)
        {
            var list = new List<TType>();

            foreach (var obj in collection)
            {
                if (obj is null)
                {
                    if (addNull)
                        list.Add(default);
                    else
                        continue;
                }

                if (obj is TFilter)
                    continue;

                if (predicate != null && predicate((TType)obj))
                    continue;

                list.Add((TType)obj);
            }

            return list;
        }

        public static bool TryPeekIndex<T>(this T[] array, int index, out T value)
        {
            if (index >= array.Length)
            {
                value = default;
                return false;
            }

            value = array[index];
            return true;
        }

        public static int NextIndexOfList<T>(this IList<T> list, int curIndex, T value, Func<T, bool> comparer = null)
        {
            if (curIndex + 1 >= list.Count)
                return -1;

            for (int i = curIndex + 1; i < list.Count; i++)
            {
                var val = list[i];

                if (comparer != null && comparer(val))
                    return i;

                if (val?.Equals(value) ?? false)
                    return i;
            }

            return -1;
        }

        public static int NextIndexOf<T>(this IEnumerable<T> values, int curIndex, T value, Func<T, bool> comparer = null)
        {
            var count = values.Count();

            if (curIndex + 1 >= count)
                return -1;

            for (int i = curIndex; i < count; i++)
            {
                var val = values.ElementAtOrDefault(i);

                if (comparer != null && comparer(val))
                    return i;

                if (val?.Equals(value) ?? false)
                    return i;
            }

            return -1;
        }

        public static Queue<TItem> ToQueue<TItem>(this IEnumerable<TItem> values)
            => new Queue<TItem>(values);

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
