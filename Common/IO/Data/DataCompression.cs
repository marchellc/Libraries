﻿using System;

namespace Common.IO.Data
{
    public static class DataCompression
    {
        public static void CompressLong(DataWriter writer, long value)
        {
            var ulongValue = (ulong)((value >> 63) ^ (value << 1));
            CompressULong(writer, ulongValue);
        }

        public static void CompressULong(DataWriter writer, ulong value)
        {
            if (value <= 240)
            {
                var a = (byte)value;

                writer.WriteByte(a);

                return;
            }

            if (value <= 2287)
            {
                var a = (byte)(((value - 240) >> 8) + 241);
                var b = (byte)((value - 240) & 0xFF);

                writer.WriteUShort((ushort)(b << 8 | a));

                return;
            }

            if (value <= 67823)
            {
                var a = (byte)249;
                var b = (byte)((value - 2288) >> 8);
                var c = (byte)((value - 2288) & 0xFF);

                writer.WriteByte(a);
                writer.WriteUShort((ushort)(c << 8 | b));

                return;
            }

            if (value <= 16777215)
            {
                var a = (byte)250;
                var b = (uint)(value << 8);

                writer.WriteUInt(b | a);

                return;
            }

            if (value <= 4294967295)
            {
                var a = (byte)251;
                var b = (uint)value;

                writer.WriteByte(a);
                writer.WriteUInt(b);

                return;
            }

            if (value <= 1099511627775)
            {
                var a = (byte)252;
                var b = (byte)(value & 0xFF);
                var c = (uint)(value >> 8);

                writer.WriteUShort((ushort)(b << 8 | a));
                writer.WriteUInt(c);

                return;
            }

            if (value <= 281474976710655)
            {
                var a = (byte)253;
                var b = (byte)(value & 0xFF);
                var c = (byte)((value >> 8) & 0xFF);
                var d = (uint)(value >> 16);

                writer.WriteByte(a);
                writer.WriteUShort((ushort)(c << 8 | b));
                writer.WriteUInt(d);

                return;
            }

            if (value <= 72057594037927935)
            {
                var a = (byte)254;
                var b = value << 8;

                writer.WriteULong(b | a);

                return;
            }

            writer.WriteByte(255);
            writer.WriteULong(value);
        }

        public static long DecompressLong(DataReader reader)
        {
            var data = DecompressULong(reader);
            return ((long)(data >> 1)) ^ -((long)data & 1);
        }

        public static ulong DecompressULong(DataReader reader)
        {
            var a0 = reader.ReadByte();

            if (a0 < 241)
                return a0;

            var a1 = reader.ReadByte();

            if (a0 <= 248)
                return 240 + ((a0 - (ulong)241) << 8) + a1;

            var a2 = reader.ReadByte();

            if (a0 == 249)
                return 2288 + ((ulong)a1 << 8) + a2;

            var a3 = reader.ReadByte();

            if (a0 == 250)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16);

            var a4 = reader.ReadByte();

            if (a0 == 251)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24);

            var a5 = reader.ReadByte();

            if (a0 == 252)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32);

            var a6 = reader.ReadByte();

            if (a0 == 253)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40);

            var a7 = reader.ReadByte();

            if (a0 == 254)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48);

            var a8 = reader.ReadByte();

            if (a0 == 255)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48) + (((ulong)a8) << 56);

            throw new IndexOutOfRangeException($"DecompressULong failed: {a0}");
        }
    }
}