using Common.Extensions;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Common.IO.Collections
{
    public class LockedList<T> : IList<T>
    {
        private volatile object listLock;
        private volatile List<T> list;

        public LockedList()
        {
            list = new List<T>();
            listLock = new object();
        }

        public LockedList(int size)
        {
            list = new List<T>(size);
            listLock = new object();
        }

        public LockedList(IEnumerable<T> items)
        {
            list = new List<T>(items);
            listLock = new object();
        }

        public T this[int index]
        {
            get
            {
                lock (listLock)
                {
                    if (index >= list.Count)
                        return default;

                    return list[index];
                }
            }
            set
            {
                lock (listLock)
                {
                    if (index >= list.Count)
                        list.Add(value);
                    else
                        list[index] = value;
                }
            }
        }

        public int Count => list.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            lock (listLock)
                list.Add(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            lock (listLock)
            {
                foreach (var item in items)
                    list.Add(item);
            }
        }

        public void Clear()
        {
            lock (listLock)
                list.Clear();
        }

        public bool Contains(T item)
        {
            lock (listLock)
                return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (listLock)
                list.CopyTo(array, arrayIndex);
        }

        public int IndexOf(T item)
        {
            lock (listLock)
                return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            lock (listLock)
                list.Insert(index, item);
        }

        public bool Remove(T item)
        {
            lock (listLock)
                return list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            lock (listLock)
                list.RemoveAt(index);
        }

        public List<T> RemoveRange(Func<T, bool> predicate)
        {
            lock (listLock)
            {
                var toRemove = new List<T>();

                foreach (var item in list)
                {
                    if (predicate.Call(item))
                        toRemove.Add(item);
                }

                foreach (var item in toRemove)
                    list.Remove(item);

                return toRemove;
            }
        }

        public IEnumerator<T> GetEnumerator()
            => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => list.GetEnumerator();
    }
}