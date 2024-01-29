using Common.Extensions;
using Common.Pooling;
using Common.Pooling.Pools;

using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace Common.IO.Data
{
    public class DataWriter : PoolableItem
    {
        public struct DataBuffer
        {
            public byte[] Data;

            public int BufferSize;
            public int DataSize;

            public bool IsFinished;

            public List<byte> Buffer;

            public DataBuffer()
            {
                Data = null;
                DataSize = 0;

                Buffer = ListPool<byte>.Shared.Rent();
                BufferSize = 0;
            }

            public void Add(byte b)
            {
                Buffer.Add(b);         
                BufferSize = Buffer.Count;
            }

            public void Finish()
            {
                if (Buffer != null)
                {
                    Data = ListPool<byte>.Shared.ToArrayReturn(Buffer);
                    DataSize = Data.Length;
                }

                BufferSize = 0;
                IsFinished = true;
            }
        }

        private DataBuffer buffer;
        private Encoding encoding;

        private char[] charBuffer = new char[1];

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

        public byte[] Data
        {
            get
            {
                if (buffer.IsFinished && buffer.Data != null)
                    return buffer.Data;

                buffer.Finish();
                return buffer.Data;
            }
        }

        public bool IsNull
        {
            get => buffer.Buffer is null;
        }

        public bool IsEmpty
        {
            get => !IsNull && buffer.Buffer.Count == 0;
        }

        public bool IsFinished
        {
            get => IsNull && buffer.Data != null;
        }

        public Encoding StringEncoding
        {
            get => encoding;
            set => encoding = value;
        }

        public override void OnPooled()
        {
            buffer.Finish();
            buffer.Data = null;
            buffer = default;
        }

        public override void OnUnPooled()
            => Refresh();

        public void Refresh()
            => buffer = new DataBuffer();

        public void WriteByte(byte b)
            => buffer.Add(b);

        public void WriteBytes(IEnumerable<byte> bytes)
        {
            WriteInt(bytes.Count());

            foreach (var b in bytes)
                buffer.Add(b);
        }

        public void WriteBool(bool b)
            => buffer.Add((byte)(b ? 1 : 0));

        public void WriteShort(short s)
        {
            buffer.Add((byte)s);
            buffer.Add((byte)(s >> 8));
        }

        public void WriteUShort(ushort us)
        {
            buffer.Add((byte)us);
            buffer.Add((byte)(us >> 8));
        }

        public void WriteInt(int i)
        {
            buffer.Add((byte)i);
            buffer.Add((byte)(i >> 8));
            buffer.Add((byte)(i >> 16));
            buffer.Add((byte)(i >> 24));
        }

        public void WriteUInt(uint ui)
        {
            buffer.Add((byte)ui);
            buffer.Add((byte)(ui >> 8));
            buffer.Add((byte)(ui >> 16));
            buffer.Add((byte)(ui >> 24));
        }

        public void WriteLong(long l)
        {
            buffer.Add((byte)l);
            buffer.Add((byte)(l >> 8));
            buffer.Add((byte)(l >> 16));
            buffer.Add((byte)(l >> 24));
            buffer.Add((byte)(l >> 32));
            buffer.Add((byte)(l >> 40));
            buffer.Add((byte)(l >> 48));
            buffer.Add((byte)(l >> 56));
        }

        public void WriteCompressedLong(long l)
            => DataCompression.CompressLong(this, l);

        public void WriteULong(ulong ul)
        {
            buffer.Add((byte)ul);
            buffer.Add((byte)(ul >> 8));
            buffer.Add((byte)(ul >> 16));
            buffer.Add((byte)(ul >> 24));
            buffer.Add((byte)(ul >> 32));
            buffer.Add((byte)(ul >> 40));
            buffer.Add((byte)(ul >> 48));
            buffer.Add((byte)(ul >> 56));
        }

        public void WriteCompressedULong(ulong ul)
            => DataCompression.CompressULong(this, ul);

        public unsafe void WriteFloating(float f)
        {
            var temp = *(uint*)&f;
            WriteUInt(temp);
        }

        public unsafe void WriteDouble(double d)
        {
            var temp = *(ulong*)&d;
            WriteULong(temp);
        }

        public void WriteChar(char c)
        {
            EnsureEncoding();

            charBuffer[0] = c;

            var bytes = encoding.GetBytes(charBuffer);
            WriteBytes(bytes);
        }

        public void WriteString(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                WriteByte(0);
                return;
            }

            EnsureEncoding();

            var bytes = encoding.GetBytes(s);

            WriteByte(1);
            WriteBytes(bytes);
        }

        public void WriteDate(DateTime d)
        {
            var binary = d.ToBinary();
            WriteLong(binary);
        }

        public void WriteTime(TimeSpan t)
        {
            var ticks = t.Ticks;
            WriteLong(ticks);
        }

        public void WriteVersion(Version v)
        {
            WriteInt(v.Major);
            WriteInt(v.Minor);
            WriteInt(v.Build);
            WriteInt(v.Revision);
        }

        public void WriteType(Type type)
        {
            var typeName = type.FullName;
            WriteString(typeName);
        }

        public void WriteMethod(MethodInfo method)
        {
            WriteType(method.DeclaringType);
            WriteString(method.Name);
        }

        public void WriteField(FieldInfo field)
        {
            WriteType(field.DeclaringType);
            WriteString(field.Name);
        }

        public void WriteProperty(PropertyInfo property)
        {
            WriteType(property.DeclaringType);
            WriteString(property.Name);
        }

        public void WriteAssemblyImage(Assembly assembly)
        {
            var assemblyImageData = assembly.GetRawBytes();
            var assemblyImage = new AssemblyImageData(assemblyImageData);

            Write(assemblyImage);
        }

        public void WriteAssemblyImage(byte[] image)
        {
            var assemblyImage = new AssemblyImageData(image);
            Write(assemblyImage);
        }

        public void WriteReader(DataReader r)                                                                                                                                              
        {
            var data = r.Buffer.Data;
            WriteBytes(data);
        }

        public void WriteWriter(DataWriter w)
        {
            var data = w.Buffer.Buffer.ToArray();
            WriteBytes(data);
        }

        public void WriteObject(object o)
        {
            if (o is null)
            {
                WriteBool(true);
                return;
            }

            WriteBool(false);

            var type = o.GetType();

            WriteType(type);

            if (o is Enum en)
            {
                DataWriterUtils.WriteEnum(this, en);
                return;
            }

            if (o is IData data)
            {
                data.Serialize(this);
                return;
            }

            var writer = DataWriterUtils.GetWriter(type);

            writer(this, o);
        }

        public void Write<T>(T value)
        {
            if (value is null)
            {
                WriteBool(true);
                return;
            }

            WriteBool(false);

            if (value is Enum en)
            {
                DataWriterUtils.WriteEnum(this, en);
                return;
            }

            if (value is IData data)
                data.Serialize(this);
            else
            {
                var writer = DataWriterUtils.GetWriter(typeof(T));
                writer(this, value);
            }
        }

        public void WriteNullable<T>(T? value) where T : struct
        {
            if (!value.HasValue)
            {
                WriteBool(true);
                return;
            }

            Write(value.Value);
        }

        public void WriteEnumerable<T>(IEnumerable<T> values)
        {
            WriteInt(values.Count());

            foreach (var item in values)
                Write(item);
        }

        public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            WriteInt(dict.Count);

            foreach (var pair in dict)
            {
                Write(pair.Key);
                Write(pair.Value);
            }
        }

        public void WriteProperties<T>(T value, params string[] props)
        {
            WriteInt(props.Length);

            for (int i = 0; i < props.Length; i++)
            {
                var property = typeof(T).Property(props[i]);

                if (property is null)
                    throw new InvalidOperationException($"Property of name '{props[i]}' does not exist in type {typeof(T).FullName}");

                var propValue = property.GetValueFast<object>(value);

                WriteInt(property.ToHash());
                WriteObject(propValue);
            }
        }

        private void EnsureEncoding()
            => encoding ??= Encoding.Default;
    }
}