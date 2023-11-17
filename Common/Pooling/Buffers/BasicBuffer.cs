using Common.Reflection;

using System;
using System.Collections.Generic;

namespace Common.Pooling.Buffers
{
    public class BasicBuffer<TObject> : IPoolBuffer<TObject>
    {
        private static Stack<TObject> _stack;
        private static Stack<TObject> _track;

        private static IPool<TObject> _pool;

        public BasicBuffer(IPool<TObject> pool, Func<TObject> constructor)
        {
            _stack = new Stack<TObject>();
            _track = new Stack<TObject>();

            _pool = pool;

            Constructor = constructor;
        }

        public IPool<TObject> Pool => _pool;

        public Func<TObject> Constructor { get; set; }

        public int Size => _stack.Count;

        public TObject Get()
        {
            TObject item = default;

            if (_stack.Count <= 0)
            {
                if ((_pool.Options & PoolOptions.ExceptionOnMissing) != 0)
                    throw new InvalidOperationException($"The pool buffer is empty.");
                else if ((_pool.Options & PoolOptions.DefaultOnMissing) != 0)
                    item = default;

                else if ((_pool.Options & PoolOptions.NewOnMissing) != 0
                    && Constructor != null)
                    item = Constructor.Call();
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

        public void Add(TObject obj)
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
        {
            if (Constructor is null)
                throw new InvalidOperationException($"The buffer's constructor is missing.");

            var item = Constructor.Call();

            if (item is null)
                return;

            Add(item);
        }

        public void Clear()
        {
            _stack.Clear();
            _track.Clear();

            _stack = null;
            _track = null;
            _pool = null;

            Constructor = null;
        }
    }
}