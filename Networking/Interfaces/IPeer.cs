using System;

namespace Networking.Interfaces
{
    public interface IPeer : IClient
    {
        IServer Server { get; }

        Guid Id { get; }

        void Process(byte[] data);
    }
}