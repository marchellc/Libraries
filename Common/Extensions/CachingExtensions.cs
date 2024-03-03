using Common.Caching;

using System;

namespace Common.Extensions
{
    public static class CachingExtensions
    {
        public static bool ContainsAny<T>(this ICache<T> cache, Func<T, bool> predicate)
        {
            var array = cache.GetAll();

            for (int i = 0; i < array.Length; i++)
            {
                if (predicate.Call(array[i]))
                    return true;
            }

            return false;
        }
    }
}