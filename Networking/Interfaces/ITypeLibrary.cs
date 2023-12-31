using Networking.Data;

using System;

namespace Networking.Interfaces
{
    public interface ITypeLibrary
    {
        short GetTypeId(Type type);
        Type GetType(short typeId);

        bool Verify(Reader reader);
        void Write(Writer writer);
    }
}