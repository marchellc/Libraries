using Common.Extensions;
using Common.Utilities;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.IO.Data
{
    public struct DataType
    {
        public readonly Type[] Types;
        public readonly Type Type;
        public readonly DataTypeName Name;

        public DataType(DataReader reader)
        {
            Name = (DataTypeName)reader.ReadByte();

            Types = reader.ReadArrayCustom(() =>
            {
                var typeId = reader.ReadInt();

                if (!TypeSearch.TryFind(typeId, out var type))
                    throw new TypeLoadException($"Failed to load type from ID '{typeId}'");

                return type;
            });

            switch (Name)
            {
                case DataTypeName.Object:
                    Type = Types[0];
                    break;

                case DataTypeName.Array:
                    Type = Types[0].MakeArrayType();
                    break;

                case DataTypeName.List:
                    Type = typeof(List<>).MakeGenericType(Types[0]);
                    break;

                case DataTypeName.HashSet:
                    Type = typeof(HashSet<>).MakeGenericType(Types[0]);
                    break;

                case DataTypeName.Dictionary:
                    Type = typeof(Dictionary<,>).MakeGenericType(Types[0], Types[1]);
                    break;
            }
        }

        public DataType(Type type)
        {
            Type = type;

            if (type.IsArray)
            {
                Types = new Type[] { type.GetElementType() };
                Name = DataTypeName.Array;
            }
            else if (type.GetTypeInfo().IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Types = new Type[] { type.GetFirstGenericType() };
                    Name = DataTypeName.List;
                }
                else if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    Types = new Type[] { type.GetFirstGenericType() };
                    Name = DataTypeName.HashSet;
                }
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Types = type.GetGenericArguments();
                    Name = DataTypeName.Dictionary;
                }
            }
            else
            {
                Types = new Type[] { type };
                Name = DataTypeName.Object;
            }
        }

        public void Write(DataWriter writer)
        {
            writer.WriteByte((byte)Name);
            writer.WriteEnumerableCustom(Types, type => writer.WriteInt(type.GetLongCode()));
        }

        public enum DataTypeName : byte
        {
            Object = 0,
            Array = 2,
            List = 4,
            HashSet = 8,
            Dictionary = 16
        }
    }
}