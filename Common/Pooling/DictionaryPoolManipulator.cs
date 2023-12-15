using Common.Extensions;

using System;
using System.Collections.Generic;

namespace Common.Pooling
{
    public class DictionaryPoolManipulator<TKey, TElement> : Poolable
    {
        public Dictionary<TKey, TElement> List { get; set; }
        public Dictionary<TKey, TElement> Copy { get; set; }

        public int Index { get; internal set; }

        public TElement Value { get; internal set; }
        public TKey Key { get; internal set; }

        public override void OnAdded()
        {
            base.OnAdded();

            List = null;
            Copy = null;

            Index = 0;
        }

        public void CopyCurrent()
            => List[Key] = Copy[Key];

        public void Add(TKey key, TElement element)
            => List[key] = element;

        public void Remove(TKey key)
            => List.Remove(key);

        public void Clear()
            => List.Clear();

        public void PerformAndCopy(Action<KeyValuePair<TKey, TElement>> action)
        {
            action.Call(new KeyValuePair<TKey, TElement>(Key, Value));
            CopyCurrent();
        }
    }
}