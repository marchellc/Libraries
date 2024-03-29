﻿using Common.Extensions;
using Common.Logging;
using Common.Pooling;
using Common.Pooling.Pools;

using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Net;

namespace Common.IO.Data
{
    public class DataWriter : PoolableItem
    {
        public static readonly LogOutput Log = new LogOutput("Data Writer").Setup();

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
            if (!buffer.IsFinished)
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

        [DataLoaderIgnore]
        public void WriteBytes(IEnumerable<byte> bytes)
        {
            WriteInt(bytes.Count());

            foreach (var b in bytes)
                WriteByte(b);
        }

        public void WriteBool(bool b)
            => WriteByte((byte)(b ? 1 : 0));

        public void WriteShort(short s)
        {
            WriteByte((byte)s);
            WriteByte((byte)(s >> 8));
        }

        public void WriteUShort(ushort us)
        {
            WriteByte((byte)us);
            WriteByte((byte)(us >> 8));
        }

        public void WriteInt(int i)
        {
            WriteByte((byte)i);
            WriteByte((byte)(i >> 8));
            WriteByte((byte)(i >> 16));
            WriteByte((byte)(i >> 24));
        }

        public void WriteUInt(uint ui)
        {
            WriteByte((byte)ui);
            WriteByte((byte)(ui >> 8));
            WriteByte((byte)(ui >> 16));
            WriteByte((byte)(ui >> 24));
        }

        public void WriteLong(long l)
        {
            WriteByte((byte)l);
            WriteByte((byte)(l >> 8));
            WriteByte((byte)(l >> 16));
            WriteByte((byte)(l >> 24));
            WriteByte((byte)(l >> 32));
            WriteByte((byte)(l >> 40));
            WriteByte((byte)(l >> 48));
            WriteByte((byte)(l >> 56));
        }

        [DataLoaderIgnore]
        public void WriteCompressedLong(long l)
            => DataCompression.CompressLong(this, l);

        public void WriteULong(ulong ul)
        {
            WriteByte((byte)ul);
            WriteByte((byte)(ul >> 8));
            WriteByte((byte)(ul >> 16));
            WriteByte((byte)(ul >> 24));
            WriteByte((byte)(ul >> 32));
            WriteByte((byte)(ul >> 40));
            WriteByte((byte)(ul >> 48));
            WriteByte((byte)(ul >> 56));
        }

        [DataLoaderIgnore]
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

        [DataLoaderIgnore]
        public void WriteFile(string filePath)
        {
            var data = new FileData(filePath);
            Write(data);
        }

        [DataLoaderIgnore]
        public void WriteFile(string name, string extension, byte[] bytes)
        {
            var data = new FileData(name, extension, bytes);
            Write(data);
        }

        public void WriteVersion(Version v)
        {
            WriteInt(v.Major);
            WriteInt(v.Minor);
            WriteInt(v.Build < 0 ? 0 : v.Build);
            WriteInt(v.Revision < 0 ? 0 : v.Build);
        }

        public void WriteIpAddress(IPAddress address)
        {
            var data = address.GetAddressBytes();
            WriteBytes(data);
        }

        public void WriteIpEndPoint(IPEndPoint endPoint)
        {
            WriteIpAddress(endPoint.Address);
            WriteCompressedULong((ulong)endPoint.Port);
        }

        public void WriteType(Type type)
            => new DataType(type).Write(this);

        public void WriteMember(MemberInfo member)
        {
            WriteType(member.DeclaringType);
            WriteUShort(member.ToShortCode());
        }

        public void WriteAssemblyImage(Assembly assembly)
        {
            var assemblyImageData = assembly.GetRawBytes();
            var assemblyImage = new AssemblyImageData(assemblyImageData);

            Write(assemblyImage);
        }

        [DataLoaderIgnore]
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
            var data = w.Data;
            WriteBytes(data);
        }

        public void WriteObject(object o)
        {
            if (o is null)
                throw new ArgumentNullException(nameof(o));

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

        [DataLoaderIgnore]
        public void WriteAnonymous<T>(T data) where T : IData
        {
            if (data is null)
                return;

            data.Serialize(this);
        }

        [DataLoaderIgnore]
        public void Write<T>(T value)
            => WriteObject(value);

        [DataLoaderIgnore]
        public void WriteNullable<T>(T? value) where T : struct
        {
            if (!value.HasValue)
            {
                WriteBool(true);
                return;
            }

            WriteBool(false);
            Write(value.Value);
        }

        [DataLoaderIgnore]
        public void WriteEnumerable<T>(IEnumerable<T> values)
        {
            WriteInt(values.Count());

            foreach (var item in values)
                Write(item);
        }

        [DataLoaderIgnore]
        public void WriteEnumerableCustom<T>(IEnumerable<T> values, Action<T> writer)
        {
            WriteInt(values.Count());

            foreach (var item in values)
                writer(item);
        }

        [DataLoaderIgnore]
        public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            WriteInt(dict.Count);

            foreach (var pair in dict)
            {
                Write(pair.Key);
                Write(pair.Value);
            }
        }

        [DataLoaderIgnore]
        public void WriteProperties<T>(T value, params string[] props)
        {
            WriteInt(props.Length);

            for (int i = 0; i < props.Length; i++)
            {
                var property = typeof(T).Property(props[i]);

                if (property is null)
                    throw new InvalidOperationException($"Property of name '{props[i]}' does not exist in type {typeof(T).FullName}");

                var propValue = property.GetValueFast<object>(value);

                WriteUShort(property.ToShortCode());
                WriteObject(propValue);
            }
        }

        [DataLoaderIgnore]
        public void Return()
            => PoolablePool<DataWriter>.Shared.Return(this);

        public static DataWriter Get()
            => PoolablePool<DataWriter>.Shared.Rent() ?? new DataWriter();

        public static byte[] Write(Action<DataWriter> writer)
        {
            var pooled = Get();
            writer.Call(pooled, null, Log.Error);
            var data = pooled.Data;
            pooled.Return();
            return data;
        }

        private void EnsureEncoding()
            => encoding ??= Encoding.Default;
    }
}