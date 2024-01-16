using Common.Logging;
using Common.IO.Collections;
using Common.Extensions;
using Common.Utilities;
using Common.Pooling.Pools;

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System;

using Networking.Utilities;

namespace Networking.Data
{
    public class Writer : Disposable
    {
        private static LockedDictionary<Type, MethodInfo> cachedGenericWriters = new LockedDictionary<Type, MethodInfo>();

        internal LogOutput log = new LogOutput("Network Writer").Setup();

        private List<byte> buffer;

        private Encoding encoding;

        private char[] charBuffer;

        public bool IsEmpty
        {
            get => buffer is null || buffer.Count == 0;
        }

        public bool IsFull
        {
            get => buffer != null && buffer.Count >= buffer.Capacity;
        }

        public int Size
        {
            get => buffer?.Count ?? 0;
        }

        public int CharSize
        {
            get => charBuffer.Length;
        }

        public byte[] Buffer
        {
            get => buffer?.ToArray() ?? Array.Empty<byte>();
        }

        public char[] CharBuffer
        {
            get => charBuffer ?? Array.Empty<char>();
        }

        public Encoding Encoding
        {
            get => encoding ??= Encoding.Default;
            set => encoding = value ?? Encoding.Default;
        }

        public Writer() 
            : this(Encoding.Default) { }

        public Writer(Encoding encoding)
        {
            this.encoding = encoding;
            this.charBuffer = new char[1];
        }

        public void WriteByte(byte value)
            => buffer.Add(value);

        public void WriteBool(bool value)
            => WriteByte(value ? (byte)1 : (byte)0);

        public void WriteSByte(sbyte value)
            => WriteByte((byte)value);

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
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
        }

        public void WriteUShort(ushort value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
        }

        public void WriteInt(int value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
            WriteByte((byte)(value >> 16));
            WriteByte((byte)(value >> 24));
        }

        public void WriteUInt(uint value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
            WriteByte((byte)(value >> 16));
            WriteByte((byte)(value >> 24));
        }

        public void WriteLong(long value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
            WriteByte((byte)(value >> 16));
            WriteByte((byte)(value >> 24));
            WriteByte((byte)(value >> 32));
            WriteByte((byte)(value >> 40));
            WriteByte((byte)(value >> 48));
            WriteByte((byte)(value >> 56));
        }

        public void WriteULong(ulong value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
            WriteByte((byte)(value >> 16));
            WriteByte((byte)(value >> 24));
            WriteByte((byte)(value >> 32));
            WriteByte((byte)(value >> 40));
            WriteByte((byte)(value >> 48));
            WriteByte((byte)(value >> 56));
        }

        public unsafe void WriteFloat(float value)
        {
            var tmpValue = *(uint*)&value;

            WriteByte((byte)tmpValue);
            WriteByte((byte)(tmpValue >> 8));
            WriteByte((byte)(tmpValue >> 16));
            WriteByte((byte)(tmpValue >> 24));
        }

        public unsafe void WriteDouble(double value)
        {
            var tmpValue = *(ulong*)&value;

            WriteByte((byte)tmpValue);
            WriteByte((byte)(tmpValue >> 8));
            WriteByte((byte)(tmpValue >> 16));
            WriteByte((byte)(tmpValue >> 24));
            WriteByte((byte)(tmpValue >> 32));
            WriteByte((byte)(tmpValue >> 40));
            WriteByte((byte)(tmpValue >> 48));
            WriteByte((byte)(tmpValue >> 56));
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
                WriteByte((byte)(tempValue | 0x80));
                tempValue >>= 7;
            }

            WriteByte((byte)tempValue);
        }

        public void WriteType(Type type)
            => WriteString(type.AssemblyQualifiedName);

        public void WriteTime(TimeSpan span)
            => WriteLong(span.Ticks);

        public void WriteDate(DateTime date)
            => WriteLong(date.Ticks);

        public void WriteWriter(Writer writer)
            => WriteBytes(writer.Buffer);

        public void WriteVersion(Version version)
        {
            WriteInt(version.Major);
            WriteInt(version.Minor);
            WriteInt(version.Build);
            WriteInt(version.Revision);
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

                if (obj is Enum enumValue)
                {
                    var enumTypeCode = enumValue.GetTypeCode();
                    var enumType = enumTypeCode.ToType();
                    var enumWriter = GetGenericWriterForType(enumType);

                    WriteByte((byte)enumTypeCode);
                    enumWriter.Call(this, Convert.ChangeType(enumValue, enumType));
                }
                else if (obj is IMessage msg)
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

        public void WriteAnonymousArray(params object[] objects)
        {
            WriteInt(objects.Length);

            for (int i = 0; i < objects.Length; i++)
                WriteAnonymous(objects[i]);
        }

        public void Write<T>(T value)
        {
            if (typeof(T).IsEnum)
            {
                var enumType = Enum.GetUnderlyingType(typeof(T));
                var enumTypeCode = Type.GetTypeCode(enumType);
                var enumWriter = GetGenericWriterForType(enumType);

                WriteByte((byte)enumTypeCode);
                enumWriter.Call(this, Convert.ChangeType(value, enumType));
                return;
            }

            if (value != null && value is IMessage msg)
            {
                WriteByte(0);
                msg.Serialize(this);
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

        public void EnsureBuffer()
            => this.buffer ??= ListPool<byte>.Shared.Rent();

        public override void OnDispose()
        {
            ListPool<byte>.Shared.Return(buffer);

            this.buffer = null;
        }

        private static MethodInfo GetGenericWriterForType(Type type)
        {
            if (cachedGenericWriters.TryGetValue(type, out var method))
                return method;

            var writeMethod = typeof(Writer).Method("Write");

            if (writeMethod is null)
                throw new Exception($"Failed to find method 'Write<T>'");

            return cachedGenericWriters[type] = writeMethod.ToGeneric(type);
        }
    }
}