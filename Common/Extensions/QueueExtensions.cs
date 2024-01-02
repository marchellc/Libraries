using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Common.Extensions
{
    public static class QueueExtensions
    {
        public static bool TryDequeue<T>(this Queue<T> queue, out T value)
        {
            if (queue.Count <= 0)
            {
                value = default;
                return false;
            }

            value = queue.Dequeue();
            return true;
        }

        public static void EnqueueMany<T>(this ConcurrentQueue<T> queue, IEnumerable<T> source)
        {
            foreach (var item in source)
                queue.Enqueue(item);
        }

        public static void EnqueueMany<T>(this Queue<T> queue, IEnumerable<T> source)
        {
            foreach (var item in source)
                queue.Enqueue(item);
        }

        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.TryDequeue(out _))
                continue;
        }
    }
}