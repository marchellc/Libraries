﻿using Common.Extensions;
using Common.Pooling.Pools;
using Common.IO.Collections;

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Common.Serialization;

namespace Common.Caching
{
    public class FileCache<T> : ICache<T>
    {
        private readonly LockedList<T> cache = new LockedList<T>();

        public int Size => cache.Count;

        public bool SaveOnChange { get; set; }
        public string DefaultPath { get; set; }

        public FileCache() { }
        public FileCache(string filePath)
        {
            SaveOnChange = true;
            DefaultPath = filePath;
            Load(filePath);
        }

        public bool Add(T value)
        {
            if (cache.Contains(value))
                return false;

            cache.Add(value);
            Save();
            return true;
        }

        public T Find(Func<T, bool> predicate)
        {
            foreach (var value in cache)
            {
                if (predicate.Call(value))
                    return value;
            }

            return default;
        }

        public IEnumerable<T> FindAll(Func<T, bool> predicate)
        {
            var list = ListPool<T>.Shared.Rent();

            foreach (var value in cache)
            {
                if (predicate.Call(value))
                    list.Add(value);
            }

            return ListPool<T>.Shared.ToArrayReturn(list);
        }

        public IEnumerable<T> GetAll()
            => cache;

        public bool TryFind(Func<T, bool> predicate, out T value)
        {
            foreach (var cachedValue in cache)
            {
                if (predicate.Call(cachedValue))
                {
                    value = cachedValue;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public int RemoveAll(Func<T, bool> predicate)
        {
            var count = cache.List.RemoveAll(value => predicate.Call(value));

            if (count > 0)
                Save();

            return count;
        }

        public int RemoveAll(IEnumerable<T> values)
        {
            var count = cache.List.RemoveAll(value => values.Contains(value));

            if (count > 0)
                Save();

            return count;
        }

        public bool Contains(T value)
            => cache.Contains(value);

        public bool Remove(T value)
        {
            if (cache.Remove(value))
            {
                Save();
                return true;
            }

            return false;
        }

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            cache.Clear();

            var data = File.ReadAllBytes(filePath);

            if (data.Length < 4)
                return;

            Deserializer.Deserialize(data, deserializer =>
            {
                var size = deserializer.GetInt32();

                for (int i = 0; i < size; i++)
                    cache.Add(deserializer.Get<T>());
            });
        }

        public void Write(string filePath)
        {
            var data = Serializer.Serialize(serializer =>
            {
                serializer.Put(cache.Count);

                for (int i = 0; i < cache.Count; i++)
                    serializer.Put(cache[i]);
            });

            File.WriteAllBytes(filePath, data);
        }

        private void Save()
        {
            if (!SaveOnChange || string.IsNullOrWhiteSpace(DefaultPath))
                return;

            Write(DefaultPath);
        }
    }
}
