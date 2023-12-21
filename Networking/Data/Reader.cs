using Common.Extensions;
using Common.Pooling;
using Common.Pooling.Pools;

using Networking.Interfaces;
using Networking.Utilities;

using System;
using System.Text;
using System.Collections.Generic;

using Utf8Json;

namespace Networking.Data
{
    public class Reader : Poolable
    {
        public const byte BYTE_SIZE = 1;
        public const byte SBYTE_SIZE = 1;

        public const byte INT_16_SIZE = 2;
        public const byte INT_32_SIZE = 4;
        public const byte INT_64_SIZE = 8;

        public const byte SINGLE_SIZE = 4;
        public const byte DOUBLE_SIZE = 8;

        private Encoding encoding;
        private List<byte> buffer;
        private ITypeLibrary typeLib;
        private byte[] data;
        private int offset;

        public byte[] Data => data;
        public byte[] Buffer => buffer.ToArray();

        public int Offset => offset;
        public int Size => data.Length;
        public int BufferSize => buffer.Count;

        public bool IsEmpty => data is null || data.Length <= 0;
        public bool IsEnd => offset >= data.Length;

        public Encoding Encoding { get => encoding; set => encoding = value; }

        public ITypeLibrary TypeLibrary { get => typeLib; set => typeLib = value; }

        public event Action<int, int> OnMoved;

        public event Action OnPooled;
        public event Action OnInitialized;

        public Reader(ITypeLibrary typeLib = null) : this(Encoding.Default, typeLib) { }

        public Reader(Encoding encoding, ITypeLibrary typeLib = null) 
        { 
            this.encoding = encoding; 
            this.typeLib = typeLib;
        }

        public override void OnAdded()
        {
            base.OnAdded();

            ListPool<byte>.Shared.Return(buffer);

            data = null;
            buffer = null;

            offset = 0;

            OnPooled.Call();
        }

        public void Initialize(byte[] data, ITypeLibrary typeLibrary = null)
        {
            this.buffer = ListPool<byte>.Shared.Next();
            this.data = data;
            this.encoding ??= Encoding.Default;

            if (typeLibrary != null && typeLib is null)
                typeLib = typeLibrary;

            OnInitialized.Call();
        }

        public byte ReadByte()
        {
            Move(BYTE_SIZE);
            return buffer[0];
        }

        public byte[] ReadBytes()
        {
            var size = ReadInt();
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

        public NetworkType ReadType()
            => new NetworkType(ReadShort());

        public List<T> ReadList<T>(Func<T> generator)
        {
            var list = new List<T>();
            var size = ReadInt();

            if (size <= 0)
                return list;

            for (int i = 0; i < size; i++)
                list.Add(generator.Call());

            return list;
        }

        public object ReadObject()
        {
            var objectValueType = ReadByte();

            if (objectValueType == 0)
                return null;

            var objectTypeInfo = ReadType();
            var objectTypeValue = objectTypeInfo.GetValue(typeLib);

            if (objectTypeValue is null)
                throw new TypeLoadException($"Type '{objectTypeInfo.TypeId}' is missing!");

            if (TypeLoader.IsDeserializable(objectTypeValue))
            {
                var objectValue = TypeLoader.Instance(objectTypeValue);

                if (objectValue != null && objectValue is IDeserialize deserialize)
                    deserialize.Deserialize(this);

                return objectValue;
            }
            else
            {
                var reader = TypeLoader.GetReader(objectTypeValue);

                if (reader is null)
                    throw new InvalidOperationException($"Cannot read type '{objectTypeValue.FullName}'");

                return reader.Call(this);
            }
        }

        public object[] ReadObjects()
        {
            var size = ReadInt();
            var array = new object[size];

            if (size <= 0)
                return array;

            for (int i = 0; i < size; i++)
                array[i] = ReadObject();

            return array;
        }

        public TObject ReadObject<TObject>()
        {
            var obj = ReadObject();

            if (obj is null)
                return default;

            if (obj is not TObject objValue)
                throw new InvalidCastException($"Cannot cast '{obj.GetType().FullName}' to '{typeof(TObject).FullName}'");

            return objValue;
        }

        public TObject[] ReadObjects<TObject>()
        {
            var size = ReadInt();

            if (size <= 0)
                return Array.Empty<TObject>();

            var list = new List<TObject>();

            for (int i = 0; i < size; i++)
            {
                var objValue = ReadObject();

                if (objValue is null || objValue is not TObject obj)
                    continue;

                list.Add(obj);
            }

            return list.ToArray();
        }

        public object ReadJson()
        {
            var objectValueType = ReadByte();

            if (objectValueType == 0)
                return null;

            var jsonBytes = ReadBytes();
            var jsonTypeInfo = ReadType();
            var jsonTypeValue = jsonTypeInfo.GetValue(typeLib);

            if (jsonTypeValue is null)
                throw new TypeLoadException($"Type '{jsonTypeInfo.TypeId}' is missing!");

            var jsonObj = JsonSerializer.NonGeneric.Deserialize(jsonTypeValue, jsonBytes);

            return jsonObj;
        }

        public TObject ReadJson<TObject>()
        {
            var objectValueType = ReadByte();

            if (objectValueType == 0)
                return default;

            var jsonBytes = ReadBytes();
            var jsonTypeInfo = ReadType();
            var jsonTypeValue = jsonTypeInfo.GetValue(typeLib);

            if (jsonTypeValue is null)
                throw new TypeLoadException($"Type '{jsonTypeInfo.TypeId}' is missing!");

            var jsonObj = JsonSerializer.NonGeneric.Deserialize(jsonTypeValue, jsonBytes);

            if (jsonObj is TObject obj)
                return obj;

            throw new InvalidCastException($"Cannot cast '{jsonTypeValue.FullName}' to '{typeof(TObject).FullName}'");
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

        private void Move(int count)
        {
            if (offset >= data.Length || (offset + count > data.Length))
                throw new InvalidOperationException($"Cannot move offset by '{count}' (reached the end)");

            buffer.Clear();

            var current = offset;

            for (int i = 0; i < count; i++)
                buffer.Add(data[offset++]);

            OnMoved.Call(current, offset);
        }
    }
}