using Common.Utilities;
using Common.Extensions;
using Common.Pooling.Pools;

using Networking.Utilities;
using Networking.Pooling;

using System;
using System.Text;
using System.Collections.Generic;

namespace Networking.Data
{
    public class Reader : Disposable
    {
        private Encoding encoding;

        private List<byte> buffer;

        private byte[] data;

        private int offset;

        public byte[] Data
        {
            get => data ?? Array.Empty<byte>();
        }

        public byte[] Buffer
        {
            get => buffer.ToArray();
        }

        public int Offset
        {
            get => offset;
            set => offset = value < 0 ? 0 : value;
        }

        public int Size
        {
            get => data.Length;
        }

        public int BufferSize
        {
            get => buffer.Count;
        }

        public bool IsEmpty
        {
            get => data is null || data.Length == 0;
        }

        public bool IsEnd
        {
            get => offset >= data.Length;
        }

        public Encoding Encoding
        {
            get => encoding ??= Encoding.Default;
            set => encoding = value ?? Encoding.Default;
        }

        public Reader() 
            : this(Encoding.Default) { }

        public Reader(Encoding encoding)
            => this.encoding = encoding;

        public void SetData(byte[] data)
        {
            this.buffer = ListPool<byte>.Shared.Rent();
            this.encoding ??= Encoding.Default;
            this.data = data;

            this.offset = 0;
        }

        public override void OnDispose()
        {
            ListPool<byte>.Shared.Return(this.buffer);

            this.buffer = null;
            this.data = null;

            this.offset = 0;
        }

        public byte ReadByte()
        {
            Move(1);

            return buffer[0];
        }

        public byte[] ReadBytes()
        {
            var size = ReadInt();

            if (size <= 0)
                return Array.Empty<byte>();

            return ReadBytes(size);
        }

        public byte[] ReadBytes(int size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException("size");

            Move(size);

            return buffer.ToArray();
        }

        public void ReadBytes(byte[] buffer, int offset, int count)
        {
            Move(count);

            if (buffer is null)
                throw new ArgumentNullException("buffer");
            else if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            else if (offset > count)
                throw new ArgumentOutOfRangeException("offset");
            else if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            for (int i = offset; i < count; i++)
                buffer[i] = this.buffer[i - offset];
        }

        public void ReadBytes(IList<byte> buffer, int offset, int count)
        {
            Move(count);

            if (buffer is null)
                throw new ArgumentNullException("buffer");
            else if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            else if (offset > count)
                throw new ArgumentOutOfRangeException("offset");
            else if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            for (int i = offset; i < count; i++)
                buffer[i] = this.buffer[i - offset];
        }

        public sbyte ReadSByte()
        {
            Move(1);

            return (sbyte)buffer[0];
        }

        public short ReadShort()
        {
            Move(2);

            return (short)(buffer[0] | buffer[1] << 8);
        }

        public ushort ReadUShort()
        {
            Move(2);

            return (ushort)(buffer[0] | buffer[1] << 8);
        }

        public int ReadInt()
        {
            Move(4);

            return buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24;
        }

        public uint ReadUInt()
        {
            Move(4);

            return (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
        }

        public int Read7BitEncodedInt()
        {
            var count = 0;
            var shift = 0;

            byte val;

            do
            {
                if (shift == (5 * 7))
                    throw new FormatException("Incorrect 7-bit int32 format");

                val = ReadByte();
                count |= (val & 0x7F) << shift;
                shift += 7;
            }
            while ((val & 0x80) != 0);

            return count;
        }

        public long ReadLong()
        {
            Move(8);

            var lo = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
            var hi = (uint)(buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24);

            return (long)((ulong)hi << 32 | lo);
        }

        public ulong ReadULong()
        {
            Move(8);

            var lo = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
            var hi = (uint)(buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24);

            return (ulong)hi << 32 | lo;
        }

        public unsafe float ReadFloat()
        {
            Move(8);

            var buff = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);

            return *(float*)&buff;
        }

        public unsafe double ReadDouble()
        {
            Move(8);

            var lo = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
            var hi = (uint)(buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24);
            var buff = ((ulong)hi) << 32 | lo;

            return *(double*)&buff;
        }

        public bool ReadBool()
        {
            Move(1);

            return buffer[0] != 0;
        }

        public char ReadChar()
        {
            var charBytes = ReadBytes();
            var charValue = encoding.GetChars(charBytes, 0, charBytes.Length);

            return charValue[0];
        }

        public string ReadString()
        {
            var stringId = ReadByte();

            if (stringId == 0)
                return null;
            else if (stringId == 1)
                return string.Empty;
            else
            {
                var stringSize = ReadInt();
                var stringBytes = ReadBytes(stringSize);
                var stringValue = encoding.GetString(stringBytes);

                return stringValue;
            }
        }

        public TimeSpan ReadTime()
            => new TimeSpan(ReadLong());

        public DateTime ReadDate()
            => new DateTime(ReadLong());

        public Version ReadVersion()
            => new Version(
                ReadInt(),
                ReadInt(),
                ReadInt(),
                ReadInt());

        public Type ReadType()
        {
            var typeName = ReadString();
            var typeValue = Type.GetType(typeName, false, true);

            if (typeValue is null)
                throw new TypeLoadException($"Type '{typeName}' has not been found.");

            return typeValue;
        }

        public Reader ReadReader()
        {
            var bytes = ReadBytes();
            var reader = ReaderPool.Shared.Rent();

            reader.SetData(bytes);

            return reader;
        }

        public T? ReadNullable<T>() where T : struct
        {
            if (ReadBool())
                return Read<T>();

            return null;
        }

        public object ReadAnonymous()
        {
            var isNull = ReadBool();

            if (isNull)
                return null;

            var type = ReadType();
            var isMessage = ReadBool();
            
            if (isMessage)
            {
                var message = type.Construct() as IMessage;

                message.Deserialize(this);

                return message;
            }

            var reader = TypeLoader.GetReader(type);

            return reader(this);
        }

        public object[] ReadAnonymousArray()
        {
            var size = ReadInt();
            var array = new object[size];

            for (int i = 0; i < size; i++)
                array[i] = ReadAnonymous();

            return array;
        }

        public T Read<T>()
        {
            var reader = TypeLoader.GetReader(typeof(T));
            var isNull = ReadByte();

            switch (isNull)
            {
                case 0:
                    {
                        var item = typeof(T).Construct();

                        if (item is null || item is not IMessage msg)
                            throw new Exception($"Invalid data signature");

                        msg.Deserialize(this);

                        if (msg is not T tMsg)
                            throw new InvalidCastException($"Cannot cast {msg.GetType().FullName} to {typeof(T).FullName}");

                        return tMsg;
                    }

                case 1:
                    return default;

                case 2:
                    {
                        var item = reader(this);

                        if (item is null || item is not T tItem)
                            throw new Exception($"Invalid data");

                        return tItem;
                    }

                default:
                    throw new Exception($"Unknown data");
            }
        }

        public List<T> ReadList<T>()
        {
            var reader = TypeLoader.GetReader(typeof(T));
            var isNull = ReadByte();

            switch (isNull)
            {
                case 0:
                    return null;

                case 1:
                    {
                        var size = ReadInt();

                        if (size <= 0)
                            return new List<T>();

                        var list = new List<T>();

                        for (int i = 0; i < size; i++)
                        {
                            var item = reader(this);

                            if (item is null || item is not T tItem)
                                continue;

                            list.Add(tItem);
                        }

                        return list;
                    }

                default:
                    throw new Exception($"Invalid data");
            }
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        {
            var keyReader = TypeLoader.GetReader(typeof(TKey));
            var valueReader = TypeLoader.GetReader(typeof(TValue));
            var isNull = ReadByte();

            switch (isNull)
            {
                case 0:
                    return null;

                case 1:
                    {
                        var size = ReadInt();

                        if (size <= 0)
                            return new Dictionary<TKey, TValue>();

                        var dict = new Dictionary<TKey, TValue>(size);

                        for (int i = 0; i < size; i++)
                        {
                            var keyItem = keyReader(this);
                            var valueItem = valueReader(this);

                            if (keyItem is null || keyItem is not TKey tKeyItem)
                                continue;

                            if (valueItem is null || valueItem is not TValue tValueItem)
                                continue;

                            dict[tKeyItem] = tValueItem;
                        }

                        return dict;
                    }

                default:
                    throw new Exception($"Invalid data");
            }
        }

        public void ReadProperties<T>(ref T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var type = typeof(T);
            var properies = ReadList<string>();

            for (int i = 0; i < properies.Count; i++)
            {
                var property = type.Property(properies[i]);

                if (property is null)
                    continue;

                var setMethod = property.GetSetMethod(true);

                if (setMethod is null)
                    continue;

                var propertyValue = ReadAnonymous();

                if (propertyValue is null)
                {
                    property.SetValueFast(null, value);
                    continue;
                }

                if (propertyValue.GetType() != property.PropertyType)
                    continue;

                property.SetValueFast(propertyValue, value);
            }
        }

        public void ClearBuffer()
            => buffer.Clear();

        public void Clear()
        {
            buffer.Clear();
            Array.Clear(data, 0, data.Length);
        }

        public void Reset()
        {
            offset = 0;
            buffer.Clear();
        }

        public void Reset(int newOffset)
        {
            offset = newOffset;
            buffer.Clear();
        }

        private void Move(int count)
        {
            if (offset >= data.Length || (offset + count > data.Length))
                throw new InvalidOperationException($"Cannot move offset by '{count}' (reached the end)");

            buffer.Clear();

            for (int i = 0; i < count; i++)
            {
                buffer.Add(data[offset]);
                offset++;
            }
        }
    }
}