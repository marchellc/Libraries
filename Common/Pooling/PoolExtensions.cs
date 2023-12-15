using Common.Pooling.Pools;
using Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Pooling
{
    public static class PoolExtensions
    {
        public static List<TElement> GetList<TElement>()
            => ListPool<TElement>.Shared.Next();

        public static List<TElement> GetList<TElement>(this int size)
            => ListPool<TElement>.Shared.Next(size);

        public static List<TElement> GetList<TElement>(this IEnumerable<TElement> elements)
            => ListPool<TElement>.Shared.Next(elements);

        public static void Return<TElement>(this List<TElement> elements)
            => ListPool<TElement>.Shared.Return(elements);

        public static TElement[] ArrayReturn<TElement>(this List<TElement> elements)
            => ListPool<TElement>.Shared.ToArrayReturn(elements);

        public static Dictionary<TKey, TElement> GetDict<TKey, TElement>()
            => DictionaryPool<TKey, TElement>.Shared.Next();

        public static Dictionary<TKey, TElement> GetDict<TKey, TElement>(this IDictionary<TKey, TElement> dictionary)
            => DictionaryPool<TKey, TElement>.Shared.Next(dictionary);

        public static void Return<TKey, TElement>(this Dictionary<TKey, TElement> dict)
            => DictionaryPool<TKey, TElement>.Shared.Return(dict);

        public static void Perform<TElement>(this List<TElement> elements, Action<PoolManipulator<TElement>> action)
        {
            var manip = PoolablePool<PoolManipulator<TElement>>.Shared.Next();

            manip.Copy = elements.GetList();
            manip.List = elements;

            for (int i = 0; i < manip.Copy.Count; i++)
            {
                action.Call(manip);
                manip.Index++;
            }

            manip.Copy.Return();

            PoolablePool<PoolManipulator<TElement>>.Shared.Return(manip);
        }

        public static void Perform<TKey, TElement>(this Dictionary<TKey, TElement> dict, Action<DictionaryPoolManipulator<TKey, TElement>> action)
        {
            var manip = PoolablePool<DictionaryPoolManipulator<TKey, TElement>>.Shared.Next();

            manip.Copy = dict.GetDict();
            manip.List = dict;

            for (int i = 0; i < manip.Copy.Count; i++)
            {
                var pair = manip.Copy.ElementAt(i);

                manip.Key = pair.Key;
                manip.Value = pair.Value;

                action.Call(manip);

                manip.Index++;
            }

            manip.Copy.Return();

            PoolablePool<DictionaryPoolManipulator<TKey, TElement>>.Shared.Return(manip);
        }
    }
}