using Networking.Interfaces;

using System;

namespace Networking.Data
{
    public struct NetworkType
    {
        public readonly short TypeId;
        public Type Value;

        public NetworkType(short typeId)
        {
            TypeId = typeId;
            Value = null;
        }

        public Type GetValue(ITypeLibrary typeLibrary)
        {
            if (Value != null)
                return Value;

            if (typeLibrary is null)
                throw new ArgumentNullException(nameof(typeLibrary));

            return Value = typeLibrary.GetType(TypeId);
        }
    }
}