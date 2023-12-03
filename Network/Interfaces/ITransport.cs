using System;
using System.IO;

namespace Network
{
    public interface ITransport
    {
        public event Action OnReady;

        IController Controller { get; }

        bool IsRunning { get; }

        long Sent { get; }
        long Received { get; }

        Latency Latency { get; }

        void Initialize();
        void Shutdown();

        void Send(byte[] data);
        void Receive(byte[] data);

        void CreateHandler(byte msgId, Action<BinaryReader> handler);
        void CreateHandler<T>(Action<T> handler);

        void RemoveHandler(byte msgId, Action<BinaryReader> handler);
        void RemoveHandler<T>(Action<T> handler);

        void Synchronize();

        short GetTypeId(Type type);

        Type GetType(short typeId);
    }
}