using Common.Pooling;
using Common.Pooling.Pools;
using Common.Utilities;
using Common.Extensions;
using Common.Logging;

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Linq;

namespace Common.IO.Data
{
    public class DataReader : PoolableItem
    {
        public static readonly LogOutput Log = new LogOutput("Data Reader").Setup();

        public struct DataBuffer
        {
            public byte[] Data;
            public byte[] Buffer;

            public int Position;

            public int BufferSize;
            public int DataSize;

            public byte Next
            {
                get
                {
                    if (Position >= Data.Length || (Position + 1) >= Data.Length)
                        throw new EndOfStreamException();

                    return Data[Position + 1];
                }
            }

            public DataBuffer(byte[] data)
            {
                Data = data;
                DataSize = data.Length;

                Position = 0;
                BufferSize = 0;
            }

            public byte[] Move(int count)
            {
                if (Position >= Data.Length || (Position + count) > Data.Length)
                    throw new EndOfStreamException();

                if (Buffer is null || Buffer.Length != count)
                {
                    ReturnBuffer();

                    Buffer = ArrayPool<byte>.Shared.Rent(count);
                    BufferSize = count;
                }

                for (int i = 0; i < count; i++)
                    Buffer[i] = Data[Position++];

                return Buffer;
            }

            public void MoveTo(int index)
            {
                if (index >= Data.Length || index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                Position = index;
                ReturnBuffer();
            }

            public void MoveToNext()
            {
                Position++;
                ReturnBuffer();
            }

            public void MoveToEnd()
            {
                Position = Data.Length;
                ReturnBuffer();
            }

            public void MoveToBeginning()
            {
                Position = 0;
                ReturnBuffer();
            }

            public void Reset()
            {
                ReturnBuffer();

                Data = null;

                Position = 0;
                DataSize = 0;
            }

            private void ReturnBuffer()
            {
                if (Buffer != null)
                    ArrayPool<byte>.Shared.Return(Buffer);

                BufferSize = 0;
                Buffer = null;
            }
        }

        private DataBuffer buffer;
        private Encoding encoding;

        public DataBuffer Buffer
        {
            get => buffer;
            set => buffer = value;
        }

        public int BufferSize
        {
            get => buffer.BufferSize;
        }

        public int DataSize
        {
            get => buffer.DataSize;
        }

        public int Position
        {
            get => buffer.Position;
            set => buffer.Position = value;
        }

        public byte[] Data
        {
            get => buffer.Data;
            set => buffer = new DataBuffer(value);
        }

        public bool IsNull
        {
            get => buffer.Data is null;
        }

        public bool IsEmpty
        {
            get => !IsNull && buffer.Data.Length == 0;
        }

        public bool IsEnd
        {
            get => !IsNull && buffer.Position >= buffer.Data.Length;
        }

        public Encoding StringEncoding
        {
            get => encoding;
            set => encoding = value;
        }

        public void Set(byte[] data)
        {
            buffer = new DataBuffer(data);
        }

        public override void OnPooled()
        {
            buffer.Reset();
            buffer = default;
        }

        public byte ReadByte()
            => buffer.Move(1)[0];

        [DataLoaderIgnore]
        public byte[] ReadBytes(int count)
            => buffer.Move(count);

        public byte[] ReadBytes()
        {
            var count = ReadInt();
            return buffer.Move(count);
        }

        public short ReadShort()
            => buffer.Move(2).ToShort();

        public ushort ReadUShort()
            => buffer.Move(2).ToUShort();

        public int ReadInt()
            => buffer.Move(4).ToInt();

        public uint ReadUInt()
            => buffer.Move(4).ToUInt();

        public double ReadDouble()
            => buffer.Move(8).ToDouble();

        public long ReadLong()
            => buffer.Move(8).ToLong();

        [DataLoaderIgnore]
        public long ReadCompressedLong()
            => DataCompression.DecompressLong(this);

        public ulong ReadULong()
            => buffer.Move(8).ToULong();

        [DataLoaderIgnore]
        public ulong ReadCompressedULong()
            => DataCompression.DecompressULong(this);

        public bool ReadBool()
            => buffer.Move(1).ToBoolean();

        public char ReadChar()
        {
            EnsureEncoding();

            var bytes = ReadBytes();
            var c = encoding.GetChars(bytes);

            return c[0];
        }

        public string ReadString()
        {
            var strByte = ReadByte();

            if (strByte == 0)
                return string.Empty;

            var length = ReadInt();
            var bytes = buffer.Move(length);

            EnsureEncoding();

            return encoding.GetString(bytes);
        }

        public DateTime ReadDate()
        {
            var ticks = ReadLong();
            return DateTime.FromBinary(ticks);
        }

        public TimeSpan ReadTime()
        {
            var ticks = ReadLong();
            return TimeSpan.FromTicks(ticks);
        }

        public FileData ReadFile()
            => Read<FileData>();

        public Version ReadVersion()
        {
            var major = ReadInt();
            var minor = ReadInt();
            var build = ReadInt();
            var revision = ReadInt();

            return new Version(major, minor, build, revision);
        }

        public IPAddress ReadIpAddress()
        {
            var data = ReadBytes();
            return new IPAddress(data);
        }

        public IPEndPoint ReadIpEndPoint()
        {
            var address = ReadIpAddress();
            var port = (int)ReadCompressedULong();

            return new IPEndPoint(address, port);
        }

        public Type ReadType()
        {
            var typeData = new DataType(this);

            if (typeData.Type is null)
                throw new TypeLoadException($"Failed to read type - unknown error.");

            return typeData.Type;
        }

        public Assembly ReadAssemblyImage()
        {
            var assemblyImage = Read<AssemblyImageData>();

            if (assemblyImage.Image is null)
                throw new ArgumentNullException(nameof(assemblyImage.Image));

            return assemblyImage.Load();
        }

        public MethodInfo ReadMethod()
        {
            var type = ReadType();
            var methodCode = ReadUShort();
            var method = type.GetAllMethods().FirstOrDefault(m => m.Name.GetShortCode() == methodCode);

            if (method != null)
                return method;

            throw new InvalidDataException($"Cannot find method of ID {methodCode} in type {type.FullName}");
        }

        public FieldInfo ReadField()
        {
            var type = ReadType();
            var fieldCode = ReadUShort();
            var field = type.GetAllFields().FirstOrDefault(f => f.Name.GetShortCode() == fieldCode);

            if (field != null)
                return field;

            throw new InvalidDataException($"Cannot find field of ID {fieldCode} in type {type.FullName}");
        }

        public PropertyInfo ReadProperty()
        {
            var type = ReadType();
            var propertyCode = ReadUShort();
            var property = type.GetAllProperties().FirstOrDefault(p => p.Name.GetShortCode() == propertyCode);

            if (property != null)
                return property;

            throw new InvalidDataException($"Cannot find property of ID {propertyCode} in type {type.FullName}");
        }

        public DataReader ReadReader()
        {
            var data = ReadBytes();
            var reader = PoolablePool<DataReader>.Shared.Rent();

            reader.Set(data);
            return reader;
        }

        public DataWriter ReadWriter()
        {
            var data = ReadBytes();
            var writer = PoolablePool<DataWriter>.Shared.Rent();
            var buffer = new DataWriter.DataBuffer();

            for (int i = 0; i < data.Length; i++)
                buffer.Add(data[i]);

            writer.Buffer = buffer;
            return writer;
        }

        public object ReadObject()
        {
            var objectType = ReadType();

            if (objectType.IsEnum)
                return DataReaderUtils.ReadEnum(this, objectType);

            if (typeof(IData).IsAssignableFrom(objectType))
            {
                var data = objectType.Construct<IData>();

                data.Deserialize(this);
                return data;
            }

            var reader = DataReaderUtils.GetReader(objectType);
            return reader(this);
        }

        [DataLoaderIgnore]
        public T ReadAnonymous<T>(Type type) where T : IData
        {
            var data = type.Construct<T>();

            data.Deserialize(this);
            return data;
        }

        [DataLoaderIgnore]
        public T Read<T>()
        {
            var isNull = ReadBool();

            if (isNull)
                return default;

            return (T)ReadObject();
        }

        [DataLoaderIgnore]
        public T? ReadNullable<T>() where T : struct
        {
            var isNull = ReadBool();

            if (isNull)
                return null;

            return (T)ReadObject();
        }

        [DataLoaderIgnore]
        public void ReadRef<T>(ref T value)
            => value = Read<T>();

        [DataLoaderIgnore]
        public T[] ReadArray<T>()
        {
            var size = ReadInt();
            var array = new T[size];

            if (size == 0)
                return array;

            for (int i = 0; i < size; i++)
                array[i] = Read<T>();

            return array;
        }

        [DataLoaderIgnore]
        public T[] ReadArrayCustom<T>(Func<T> reader)
        {
            var size = ReadInt();
            var array = new T[size];

            if (size == 0)
                return array;

            for (int i = 0; i < size; i++)
                array[i] = reader();

            return array;
        }

        [DataLoaderIgnore]
        public void ReadIntoArray<T>(T[] destination, int start = 0)
        {
            var size = ReadInt();

            if (size == 0)
                return;

            if (size > destination.Length)
                throw new Exception($"Destination array is smaller than required.");

            for (int i = 0; i < size; i++)
                destination[i + start] = Read<T>();
        }

        [DataLoaderIgnore]
        public void ReadIntoArrayCustom<T>(T[] destination, int start, Func<T> reader)
        {
            var size = ReadInt();

            if (size == 0)
                return;

            if (size > destination.Length)
                throw new Exception($"Destination array is smaller than required.");

            for (int i = 0; i < size; i++)
                destination[i + start] = reader();
        }

        [DataLoaderIgnore]
        public List<T> ReadList<T>()
        {
            var size = ReadInt();
            var list = new List<T>(size);

            if (size == 0)
                return list;

            for (int i = 0; i < size; i++)
                list.Add(Read<T>());

            return list;
        }

        [DataLoaderIgnore]
        public void ReadIntoList<T>(ICollection<T> list)
        {
            var size = ReadInt();

            if (size == 0)
                return;

            for (int i = 0; i < size; i++)
                list.Add(Read<T>());
        }

        [DataLoaderIgnore]
        public HashSet<T> ReadHashSet<T>()
        {
            var size = ReadInt();
            var set = new HashSet<T>(size);

            if (size == 0)
                return set;

            for (int i = 0; i < size; i++)
                set.Add(Read<T>());

            return set;
        }

        [DataLoaderIgnore]
        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        {
            var size = ReadInt();
            var dict = new Dictionary<TKey, TValue>(size);

            if (size == 0)
                return dict;

            for (int i = 0; i < size; i++)
                dict[Read<TKey>()] = Read<TValue>();

            return dict;
        }

        [DataLoaderIgnore]
        public void ReadIntoDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            var size = ReadInt();

            if (size == 0)
                return;

            for (int i = 0; i < size; i++)
                dict[Read<TKey>()] = Read<TValue>();
        }

        [DataLoaderIgnore]
        public void ReadProperties<T>(T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var type = typeof(T);
            var count = ReadInt();

            for (int i = 0; i < count; i++)
            {
                var propertyHash = ReadUShort();
                var propertyValue = ReadObject();
                var property = type.GetAllProperties().FirstOrDefault(p => p.Name.GetShortCode() == propertyHash);

                if (property is null)
                    throw new InvalidDataException($"Property of ID {propertyHash} was not present in type {type.FullName}");

                property.Set(propertyValue, value);
            }
        }

        [DataLoaderIgnore]
        public void Return()
            => PoolablePool<DataReader>.Shared.Return(this);

        public static DataReader Get(byte[] data)
        {
            var reader = PoolablePool<DataReader>.Shared.Rent() ?? new DataReader();
            reader.Set(data);
            return reader;
        }

        public static void Read(byte[] data, Action<DataReader> reader)
        {
            var read = Get(data);
            reader.Call(read, null, Log.Error);
            read.Return();
        }

        private void EnsureEncoding()
            => encoding ??= Encoding.Default;
    }
}