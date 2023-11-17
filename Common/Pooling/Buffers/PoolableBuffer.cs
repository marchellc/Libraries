using System;
using System.Collections.Generic;

namespace Common.Pooling.Buffers
{
    public class PoolableBuffer<TPoolable> : IPoolBuffer<TPoolable> where TPoolable : IPoolable, new()
    {
        private static Stack<TPoolable> _stack;
        private static Stack<TPoolable> _track;

        private static IPool<TPoolable> _pool;

        public PoolableBuffer(IPool<TPoolable> pool)
        {
            _stack = new Stack<TPoolable>();
            _track = new Stack<TPoolable>();

            _pool = pool;
        }

        public IPool<TPoolable> Pool => _pool;

        public int Size => _stack.Count;

        public Func<TPoolable> Constructor { get => () => new TPoolable(); set => throw new InvalidOperationException(); }

        public TPoolable Get()
        {
            TPoolable item = default;

            if (_stack.Count <= 0)
            {
                if ((_pool.Options & PoolOptions.ExceptionOnMissing) != 0)
                    throw new InvalidOperationException($"The pool buffer is empty.");
                else if ((_pool.Options & PoolOptions.DefaultOnMissing) != 0)
                    item = default;

                else if ((_pool.Options & PoolOptions.NewOnMissing) != 0)
                    item = new TPoolable();
                else
                    throw new InvalidOperationException($"There is no action specified in case of an empty pool buffer.");
            }
            else
            {
                item = _stack.Pop();
            }

            if (item != null
                && !_track.Contains(item)
                && (_pool.Options & PoolOptions.EnableTracking) != 0)
                _track.Push(item);

            return item;
        }

        public void Add(TPoolable obj)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            if (_stack.Contains(obj))
                throw new InvalidOperationException($"This object is already in the pool buffer.");

            if ((_pool.Options & PoolOptions.EnableTracking) != 0 && !_track.Contains(obj))
                throw new InvalidOperationException($"This object does not belong to this pool buffer.");

            _stack.Push(obj);
        }

        public void AddNew()
            => Add(new TPoolable());

        public void Clear()
        {
            _stack.Clear();
            _track.Clear();

            _stack = null;
            _track = null;
            _pool = null;
        }
    }
}
