using Common.Pooling;
using Common.Extensions;
using Common.Pooling.Pools;

using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

using Networking.Utilities;
using Networking.Pooling;

namespace Networking.Data
{
    public class Writer
    {
        internal WriterPool pool;

        private List<byte> buffer;
        private Encoding encoding;
        private char[] charBuffer;

        public bool IsEmpty => buffer is null || buffer.Count <= 0;
        public bool IsFull => buffer != null && buffer.Count >= buffer.Capacity;

        public int Size => buffer.Count;
        public int CharSize => charBuffer.Length;

        public byte[] Buffer => buffer.ToArray();
        public char[] CharBuffer => charBuffer;

        public Encoding Encoding { get => encoding; set => encoding = value; }

        public event Action OnPooled;
        public event Action OnUnpooled;

        public Writer() : this(Encoding.Default) { }

        public Writer(Encoding encoding)
        {
            this.encoding = encoding;
            this.charBuffer = new char[1];
            this.buffer = ListPool<byte>.Shared.Next();
        }

        internal void ToPool()
        {
            this.buffer?.Return();
            this.buffer = null;

            OnPooled.Call();
        }

        internal void FromPool()
        {
            this.buffer = ListPool<byte>.Shared.Next();
            this.encoding ??= Encoding.Default;

            OnUnpooled.Call();
        }

        public void WriteBool(bool value)
            => buffer.Add(value ? (byte)1 : (byte)0);

        public void WriteByte(byte value)
            => buffer.Add(value);

        public void WriteSByte(sbyte value)
            => buffer.Add((byte)value);

        public void WriteBytes(IEnumerable<byte> bytes)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            var size = bytes.Count();

            if (size <= 0)
            {
                WriteInt(0);
                return;
            }

            WriteInt(size);

            foreach (var b in bytes)
                WriteByte(b);
        }

        public void WriteShort(short value)
        {
            buffer.Add((byte)value);
            buffer.Add((byte)(value >> 8));
        }

        public void WriteUShort(ushort value)
        {
            buffer.Add((byte)value);
            buffer.Add((byte)(value >> 8));
        }

        public void WriteInt(int value)
        {
            buffer.Add((byte)value);
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)(value >> 16));
            buffer.Add((byte)(value >> 24));
        }

        public void WriteUInt(uint value)
        {
            buffer.Add((byte)value);
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)(value >> 16));
            buffer.Add((byte)(value >> 24));
        }

        public void WriteLong(long value)
        {
            buffer.Add((byte)value);
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)(value >> 16));
            buffer.Add((byte)(value >> 24));
            buffer.Add((byte)(value >> 32));
            buffer.Add((byte)(value >> 40));
            buffer.Add((byte)(value >> 48));
            buffer.Add((byte)(value >> 56));
        }

        public void WriteULong(ulong value)
        {
            buffer.Add((byte)value);
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)(value >> 16));
            buffer.Add((byte)(value >> 24));
            buffer.Add((byte)(value >> 32));
            buffer.Add((byte)(value >> 40));
            buffer.Add((byte)(value >> 48));
            buffer.Add((byte)(value >> 56));
        }

        public unsafe void WriteFloat(float value)
        {
            var tmpValue = *(uint*)&value;

            buffer.Add((byte)tmpValue);
            buffer.Add((byte)(tmpValue >> 8));
            buffer.Add((byte)(tmpValue >> 16));
            buffer.Add((byte)(tmpValue >> 24));
        }

        public unsafe void WriteDouble(double value)
        {
            var tmpValue = *(ulong*)&value;

            buffer.Add((byte)tmpValue);
            buffer.Add((byte)(tmpValue >> 8));
            buffer.Add((byte)(tmpValue >> 16));
            buffer.Add((byte)(tmpValue >> 24));
            buffer.Add((byte)(tmpValue >> 32));
            buffer.Add((byte)(tmpValue >> 40));
            buffer.Add((byte)(tmpValue >> 48));
            buffer.Add((byte)(tmpValue >> 56));
        }

        public void WriteChar(char value)
        {
            charBuffer[0] = value;
            WriteBytes(encoding.GetBytes(charBuffer, 0, charBuffer.Length));
        }

        public void WriteString(string value)
        {
            if (value is null)
            {
                WriteByte(0);
                return;
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                WriteByte(1);
                return;
            }
            else
            {
                WriteByte(2);
                WriteBytes(encoding.GetBytes(value));
            }
        }

        public void Write7BitEncodedInt(int value)
        {
            var tempValue = (uint)value;

            while (tempValue > 0x80)
            {
                buffer.Add((byte)(tempValue | 0x80));
                tempValue >>= 7;
            }

            buffer.Add((byte)tempValue);
        }

        public void WriteType(Type type)
        {
            WriteString(type.AssemblyQualifiedName);
        }

        public void WriteTime(TimeSpan span)
            => WriteLong(span.Ticks);

        public void WriteDate(DateTime date)
        {
            WriteShort((short)date.Year);

            WriteByte((byte)date.Month);
            WriteByte((byte)date.Day);
            WriteByte((byte)date.Hour);
            WriteByte((byte)date.Second);
            WriteByte((byte)date.Millisecond);
        }

        public void WriteVersion(Version version)
        {
            WriteInt(version.Major);
            WriteInt(version.Minor);
            WriteInt(version.Build);
            WriteInt(version.Revision);
        }

        public void WriteWriter(Writer writer)
        {
            WriteBytes(writer.Buffer);
        }

        public void WriteAnonymous(object obj)
        {
            if (obj is null)
            {
                WriteBool(true);
                return;
            }
            else
            {


                WriteBool(false);
                WriteType(obj.GetType());

                if (obj is IMessage msg)
                {
                    WriteBool(true);
                    msg.Serialize(this);
                }
                else
                {
                    WriteBool(false);
                    var writer = TypeLoader.GetWriter(obj.GetType());
                    writer(this, obj);
                }
            }
        }

        public void WriteAnonymousArray(object[] objects)
        {
            WriteInt(objects.Length);

            for (int i = 0; i < objects.Length; i++)
                WriteAnonymous(objects[i]);
        }

        public void Write<T>(T value)
        {
            if (value != null && value is ISerialize serialize)
            {
                WriteByte(0);
                serialize.Serialize(this);
                return;
            }

            var writer = TypeLoader.GetWriter(typeof(T));

            if (value is null)
            {
                WriteByte(1);
                return;
            }
            else
            {
                WriteByte(2);
                writer(this, value);
            }
        }

        public void WriteList<T>(IEnumerable<T> items)
        {
            var writer = TypeLoader.GetWriter(typeof(T));

            if (items != null)
            {
                WriteByte(1);

                var size = items.Count();

                WriteInt(size);

                if (size <= 0)
                    return;

                foreach (var item in items)
                    writer(this, item);
            }
            else
            {
                WriteByte(0);
                return;
            }
        }

        public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            var keyWriter = TypeLoader.GetWriter(typeof(TKey));
            var valueWriter = TypeLoader.GetWriter(typeof(TValue));

            if (dict != null)
            {
                WriteByte(1);
                WriteInt(dict.Count);

                if (dict.Count <= 0)
                    return;

                foreach (var pair in dict)
                {
                    keyWriter(this, pair.Key);
                    valueWriter(this, pair.Value);
                }
            }
            else
            {
                WriteByte(0);
                return;
            }
        }

        public void Clear()
            => buffer.Clear();

        public void Take(byte[] target, int start, int size)
        {
            if (size > Size)
                throw new ArgumentOutOfRangeException("size");

            if (size > target.Length)
                throw new ArgumentOutOfRangeException("size");

            for (int i = start; i < size; i++)
            {
                target[i] = buffer.First();
                buffer.RemoveAt(0);
            }
        }

        public byte[] Take(int size)
        {
            if (size > Size)
                throw new ArgumentOutOfRangeException("size");

            var array = new byte[size];

            Take(array, 0, size);

            return array;
        }

        public void Return()
        {
            if (pool is null)
                throw new InvalidOperationException($"Cannot return to pool");

            pool.Return(this);
        }
    }
}