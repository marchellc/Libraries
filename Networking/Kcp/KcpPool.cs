using System;
using System.Collections.Generic;

namespace Networking.Kcp
{
    public class KcpPool<T>
    {
        readonly Stack<T> objects = new Stack<T>();
        readonly Func<T> objectGenerator;
        readonly Action<T> objectResetter;

        public KcpPool(Func<T> objectGenerator, Action<T> objectResetter, int initialCapacity)
        {
            this.objectGenerator = objectGenerator;
            this.objectResetter = objectResetter;

            for (int i = 0; i < initialCapacity; ++i)
                objects.Push(objectGenerator());
        }

        public T Take() 
            => objects.Count > 0 ? objects.Pop() : objectGenerator();

        public void Return(T item)
        {
            objectResetter(item);
            objects.Push(item);
        }


        public void Clear() => objects.Clear();

        public int Count => objects.Count;
    }
}