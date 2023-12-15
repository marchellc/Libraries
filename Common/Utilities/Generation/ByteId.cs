using Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Utilities.Generation
{
    public class ByteId
    {
        private List<byte> generated;
        private bool emptyCalled;

        public ByteId()
        {
            generated = new List<byte>(byte.MaxValue);
            Regenerate();
        }

        public bool IsEmpty => generated.Count <= 0;
        public bool IsAny => generated.Count > 0;

        public event Action OnEmpty;

        public event Action<byte> OnTook;
        public event Action<byte> OnReturned;

        public byte Take()
        {
            if (IsEmpty)
                throw new InvalidOperationException($"The ID list is empty.");

            var id = generated.Last();

            generated.RemoveAt(generated.Count - 1);
            OnTook.Call(id);

            if (IsEmpty && !emptyCalled)
            {
                OnEmpty.Call();
                emptyCalled = true;
            }

            return id;
        }

        public bool TryTake(out byte result)
        {
            if (IsEmpty)
            {
                result = 0;
                return false;
            }

            result = generated.Last();

            generated.RemoveAt(generated.Count - 1);
            OnTook.Call(result);

            if (IsEmpty && !emptyCalled)
            {
                OnEmpty.Call();
                emptyCalled = true;
            }

            return true;
        }

        public void Return(byte id)
        {
            if (generated.Contains(id))
            {
                throw new InvalidOperationException($"ID '{id}' was already returned");
            }

            generated.Add(id);
            OnReturned.Call(id);

            emptyCalled = false;
        }

        public void Remove(byte id)
        {
            if (generated.Contains(id))
            {
                generated.Remove(id);

                if (IsEmpty && !emptyCalled)
                {
                    OnEmpty.Call();
                    emptyCalled = true;
                }
            }
        }

        public void Regenerate(params byte[] exclude)
        {
            generated.Clear();

            var next = byte.MinValue;

            while (generated.Count < (byte.MaxValue - exclude.Length))
            {
                if (exclude.Contains(next))
                    continue;

                generated.Add(next);

                next++;
            }

            emptyCalled = false;
        }
    }
}
