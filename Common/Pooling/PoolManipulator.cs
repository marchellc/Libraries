using Common.Extensions;

using System;
using System.Collections.Generic;

namespace Common.Pooling
{
    public class PoolManipulator<TElement> : Poolable
    {
        public List<TElement> List { get; set; }
        public List<TElement> Copy { get; set; }

        public int Index { get; internal set; }

        public TElement Current => Copy[Index];

        public override void OnAdded()
        {
            base.OnAdded();

            List = null;
            Copy = null;

            Index = 0;
        }

        public void CopyCurrent()
        {
            if (Index >= List.Count)
                List.Add(Copy[Index]);
            else
                List[Index] = Copy[Index];
        }

        public void Move(TElement element, int destIndex)
        {
            if (destIndex >= List.Count || destIndex < 0)
                List.Add(element);
            else
                List[destIndex] = element;
        }

        public void Add(TElement element)
            => List.Add(element);

        public void Remove(int curIndex)
            => List.RemoveAt(curIndex);

        public void Remove(TElement element)
            => List.Remove(element);

        public void Clear()
            => List.Clear();

        public void Range(IEnumerable<TElement> elements)
            => List.AddRange(elements);

        public void PerformAndCopy(Action<TElement> action)
        {
            action.Call(Current);
            CopyCurrent();
        }
    }
}