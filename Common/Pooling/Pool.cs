using System.Collections.Generic;
using System;

using Common.Extensions;

namespace Common.Pooling
{
    public class Pool<T>
    {
        private Stack<T> poolStack;

        public uint InitialSize { get; set; }

        public uint Size
        {
            get => poolStack is null ? uint.MinValue : (uint)poolStack.Count;
        }

        public Pool(uint initialSize = uint.MinValue)
        {
            InitialSize = initialSize;
            Clear();
        }

        public T Rent()
        {
            var value = GetNextValue();

            if (value is null)
                return value;

            OnRenting(value);

            return value;
        }

        public void Return(T value)
        {
            if (value is null)
                return;

            OnReturning(value);

            poolStack.TryPush(value);
        }

        public void Clear()
            => Clear(InitialSize);

        public void Clear(uint newSize)
        {
            if (poolStack != null)
                poolStack?.Clear();
            else
                poolStack = new Stack<T>();

            for (int i = 0; i < newSize; i++)
                Return(Construct());
        }

        public virtual void OnRenting(T value) { }
        public virtual void OnReturning(T value) { }

        public virtual T Construct() => throw new NotImplementedException();

        private T GetNextValue()
        {
            if (!poolStack.TryPop(out var nextValue))
                nextValue = Construct();

            return nextValue;
        }
    }
}