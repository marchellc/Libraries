using Common.Pooling;
using Common.Extensions;
using Common.Pooling.Pools;

using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

using Networking.Interfaces;
using Networking.Utilities;

using Utf8Json;

namespace Networking.Data
{
    public class Writer : Poolable
    {
        private List<byte> buffer;
        private Encoding encoding;
        private ITypeLibrary typeLib;
        private char[] charBuffer;

        public bool IsEmpty => buffer is null || buffer.Count <= 0;
        public bool IsFull => buffer != null && buffer.Count >= buffer.Capacity;

        public int Size => buffer.Count;
        public int CharSize => charBuffer.Length;

        public byte[] Buffer => buffer.ToArray();
        public char[] CharBuffer => charBuffer;

        public Encoding Encoding { get => encoding; set => encoding = value; }
        public ITypeLibrary TypeLibrary { get => typeLib; set => typeLib = value; }

        public event Action OnPooled;
        public event Action OnUnpooled;

        public Writer(ITypeLibrary typeLib = null) : this(Encoding.Default, typeLib) { }

        public Writer(Encoding encoding, ITypeLibrary typeLib = null)
        {
            this.encoding = encoding;
            this.charBuffer = new char[1];
            this.buffer = ListPool<byte>.Shared.Next();
            this.typeLib = typeLib;
        }

        public override void OnAdded()
        {
            base.OnAdded();

            this.buffer.Return();
            this.buffer = null;

            OnPooled.Call();
        }

        public override void OnRemoved()
        {
            base.OnRemoved();

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
            WriteShort(typeLib.GetTypeId(type));
        }

        public void WriteList<T>(IEnumerable<T> list, Action<T> writer)
        {
            var size = list.Count();

            WriteInt(size);

            if (size <= 0)
                return;

            foreach (var obj in list)
                writer.Call(obj);
        }

        public void WriteObject(object value)
        {
            if (value is null)
            {
                WriteByte(0);
                return;
            }

            var valueType = value.GetType();

            WriteType(valueType);

            if (valueType is ISerialize serialize)
                serialize.Serialize(this);
            else
            {
                var writer = TypeLoader.GetWriter(valueType);

                if (writer is null)
                    return;

                writer.Call(this, value);
            }
        }

        public void WriteObjects(params object[] objects)
        {
            var size = objects.Length;

            WriteInt(size);

            if (size <= 0)
                return;

            for (int i = 0; i < objects.Length; i++)
                WriteObject(objects[i]);
        }

        public void WriteObjects(IEnumerable<object> objects)
        {
            var size = objects.Count();

            WriteInt(size);

            if (size <= 0)
                return;

            foreach (var obj in objects)
                WriteObject(obj);
        }

        public void WriteJson(object value)
        {
            if (value is null)
            {
                WriteByte(0);
                return;
            }

            var valueType = value.GetType();
            var valueBytes = JsonSerializer.NonGeneric.Serialize(value);

            WriteType(valueType);
            WriteBytes(valueBytes);
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
    }
}