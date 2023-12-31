using System.Collections.Concurrent;

namespace Common.Extensions
{
    public static class QueueExtensions
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.TryDequeue(out _))
                continue;
        }
    }
}