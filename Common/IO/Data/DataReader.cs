using Common.Pooling;
using Common.Pooling.Pools;
using Common.Utilities;
using Common.Extensions;

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace Common.IO.Data
{
    public class DataReader : PoolableItem
    {
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
            => buffer = new DataBuffer(data);

        public override void OnPooled()
        {
            buffer.Reset();
            buffer = default;
        }

        public byte ReadByte()
            => buffer.Move(1)[0];

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

        public long ReadCompressedLong()
            => DataCompression.DecompressLong(this);

        public ulong ReadULong()
            => buffer.Move(8).ToULong();

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

        public Version ReadVersion()
        {
            var major = ReadInt();
            var minor = ReadInt();
            var build = ReadInt();
            var revision = ReadInt();

            return new Version(major, minor, build, revision);
        }

        public Type ReadType()
        {
            var typeName = ReadString();

            if (!AssemblyCache.TryRetrieveType(typeName, out var type))
                throw new TypeLoadException($"Failed to find type of ID '{typeName}'");

            return type;
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
            var method = ReadString();
            var methods = type.GetAllMethods();

            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == method)
                    return methods[i];
            }

            throw new InvalidDataException($"Cannot find method of ID {method} in type {type.FullName}");
        }

        public FieldInfo ReadField()
        {
            var type = ReadType();
            var field = ReadString();
            var fields = type.GetAllFields();

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == field)
                    return fields[i];
            }

            throw new InvalidDataException($"Cannot find field of ID {field} in type {type.FullName}");
        }

        public PropertyInfo ReadProperty()
        {
            var type = ReadType();
            var property = ReadString();
            var properties = type.GetAllProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].Name == property)
                    return properties[i];
            }

            throw new InvalidDataException($"Cannot find property of ID {property} in type {type.FullName}");
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
            var isNull = ReadBool();

            if (isNull)
                return null;

            var objectType = ReadType();

            if (objectType.IsEnum)
                return DataReaderUtils.ReadEnum(this);

            if (typeof(IData).IsAssignableFrom(objectType))
            {
                var data = objectType.Construct<IData>();

                data.Deserialize(this);
                return data;
            }

            var reader = DataReaderUtils.GetReader(objectType);

            return reader(this);
        }

        public T Read<T>(T defaultValue = default)
        {
            var isNull = ReadBool();

            if (isNull)
                return defaultValue;

            if (typeof(T).IsEnum)
                return (T)(object)DataReaderUtils.ReadEnum(this);

            if (typeof(IData).IsAssignableFrom(typeof(T)))
            {
                var data = typeof(T).Construct<IData>();

                data.Deserialize(this);
                return (T)data;
            }

            var reader = DataReaderUtils.GetReader(typeof(T));
            return (T)reader(this);
        }

        public T? ReadNullable<T>() where T : struct
        {
            var isNull = ReadBool();

            if (isNull)
                return null;

            return Read<T>();
        }

        public void ReadRef<T>(ref T value, T defaultValue = default)
            => value = Read(defaultValue);

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

        public void ReadIntoList<T>(ICollection<T> list)
        {
            var size = ReadInt();

            if (size == 0)
                return;

            for (int i = 0; i < size; i++)
                list.Add(Read<T>());
        }

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

        public void ReadIntoDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            var size = ReadInt();

            if (size == 0)
                return;

            for (int i = 0; i < size; i++)
                dict[Read<TKey>()] = Read<TValue>();
        }

        public void ReadProperties<T>(T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var type = typeof(T);
            var count = ReadInt();
            var properties = type.GetAllProperties();

            PropertyInfo FindProperty(int propertyHash)
            {
                for (int x = 0; x < properties.Length; x++)
                {
                    if (properties[x].ToHash() == propertyHash)
                        return properties[x];
                }

                return null;
            }

            for (int i = 0; i < count; i++)
            {
                var propertyHash = ReadInt();
                var propertyValue = ReadObject();
                var property = FindProperty(propertyHash);

                if (property is null)
                    throw new InvalidDataException($"Property of ID {propertyHash} was not present in type {type.FullName}");

                property.SetValueFast(propertyValue, value);
            }
        }

        private void EnsureEncoding()
            => encoding ??= Encoding.Default;
    }
}