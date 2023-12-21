using Networking.Data;

using System;

namespace Networking.Interfaces
{
    public interface ITypeLibrary
    {
        Reader PendingTypes { get; set; }

        bool IsSynchronized { get; }

        short GetTypeId(Type type);
        Type GetType(short typeId);

        void SendTypes();
        void ReceiveTypes();
    }
}