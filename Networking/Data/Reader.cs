using Common.Extensions;
using Common.Pooling.Pools;

using Networking.Utilities;
using Networking.Pooling;

using System;
using System.Text;
using System.Collections.Generic;
using Common.Logging;

namespace Networking.Data
{
    public class Reader 
    {
        public const byte BYTE_SIZE = 1;
        public const byte SBYTE_SIZE = 1;

        public const byte INT_16_SIZE = 2;
        public const byte INT_32_SIZE = 4;
        public const byte INT_64_SIZE = 8;

        public const byte SINGLE_SIZE = 4;
        public const byte DOUBLE_SIZE = 8;

        internal ReaderPool pool;

        private Encoding encoding;
        private List<byte> buffer;
        private byte[] data;
        private int offset;

        public byte[] Data => data;
        public byte[] Buffer => buffer.ToArray();

        public int Offset { get => offset; set => offset = value; }

        public int Size => data.Length;
        public int BufferSize => buffer.Count;

        public bool IsEmpty => data is null || data.Length <= 0;
        public bool IsEnd => offset >= data.Length;

        public Encoding Encoding { get => encoding; set => encoding = value; }

        public event Action<int, int> OnMoved;

        public event Action OnPooled;
        public event Action OnUnpooled;

        public Reader() : this(Encoding.Default) { }

        public Reader(Encoding encoding) 
        { 
            this.encoding = encoding;
        }

        internal void ToPool()
        {
            ListPool<byte>.Shared.Return(buffer);

            data = null;
            buffer = null;

            offset = 0;

            OnPooled.Call();
        }

        internal void FromPool(byte[] data)
        {
            this.buffer = ListPool<byte>.Shared.Next();
            this.data = data;
            this.encoding ??= Encoding.Default;

            OnUnpooled.Call();
        }

        public byte ReadByte()
        {
            Move(BYTE_SIZE);
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
            Move(SBYTE_SIZE);
            return (sbyte)buffer[0];
        }

        public short ReadShort()
        {
            Move(INT_16_SIZE);
            return (short)(buffer[0] | buffer[1] << 8);
        }

        public ushort ReadUShort()
        {
            Move(INT_16_SIZE);
            return (ushort)(buffer[0] | buffer[1] << 8);
        }

        public int ReadInt()
        {
            Move(INT_32_SIZE);
            return buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24;
        }

        public uint ReadUInt()
        {
            Move(INT_32_SIZE);
            return (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
        }

        public long ReadLong()
        {
            Move(INT_64_SIZE);

            var lo = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
            var hi = (uint)(buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24);

            return (long)((ulong)hi << 32 | lo);
        }

        public ulong ReadULong()
        {
            Move(INT_64_SIZE);

            var lo = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
            var hi = (uint)(buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24);

            return (ulong)hi << 32 | lo;
        }

        public unsafe float ReadFloat()
        {
            Move(SINGLE_SIZE);

            var buff = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);

            return *(float*)&buff;
        }

        public unsafe double ReadDouble()
        {
            Move(DOUBLE_SIZE);

            var lo = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
            var hi = (uint)(buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24);
            var buff = ((ulong)hi) << 32 | lo;

            return *(double*)&buff;
        }

        public bool ReadBool()
        {
            Move(BYTE_SIZE);
            return buffer[0] != 0;
        }

        public char ReadChar()
        {
            var charBytes = ReadBytes();
            var charValue = encoding.GetChars(charBytes, 0, charBytes.Length);

            return charValue[0];
        }

        public string ReadCleanString()
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

        public NetworkString ReadString()
        {
            var stringId = ReadByte();

            if (stringId == 0)
                return new NetworkString(true, true, null);
            else if (stringId == 1)
                return new NetworkString(true, false, string.Empty);
            else
            {
                var stringSize = ReadInt();
                var stringBytes = ReadBytes(stringSize);
                var stringValue = encoding.GetString(stringBytes);

                return new NetworkString(false, false, stringValue);
            }
        }

        public Version ReadVersion()
            => new Version(
                ReadInt(),
                ReadInt(),
                ReadInt(),
                ReadInt());

        public Type ReadType()
        {
            var typeName = ReadCleanString();
            var typeValue = Type.GetType(typeName);

            return typeValue;
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

        public Reader ReadReader()
        {
            var bytes = ReadBytes();

            if (pool != null)
                return pool.Next(bytes);
            else
            {
                var reader = new Reader();
                reader.FromPool(bytes);
                return reader;
            }
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

        public void Return()
        {
            if (pool is null)
                throw new InvalidOperationException($"Cannot return to an empty pool");

            pool.Return(this);
        }

        private void Move(int count)
        {
            if (offset >= data.Length || (offset + count > data.Length))
                throw new InvalidOperationException($"Cannot move offset by '{count}' (reached the end)");

            buffer.Clear();

            var current = offset;

            for (int i = 0; i < count; i++)
            {
                buffer.Add(data[offset]);
                offset++;
            }

            OnMoved.Call(current, offset);
        }
    }
}