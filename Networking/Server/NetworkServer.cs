using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;

using Networking.Features;

using System;

namespace Networking.Server
{
    public class NetworkServer
    {
        public LogOutput log;
        public Telepathy.Server server;

        public LockedDictionary<int, NetworkConnection> connections;
        public LockedList<Type> requestFeatures;

        public int port = 8000;

        public bool isRunning;
        public bool isNoDelay = true;

        public event Action OnStarted;
        public event Action OnStopped;
        public event Action<NetworkConnection> OnConnected;
        public event Action<NetworkConnection> OnDisconnected;
        public event Action<NetworkConnection, byte[]> OnData;

        public static readonly Version version;
        public static readonly NetworkServer instance;

        static NetworkServer()
        {
            version = new Version(1, 0, 0, 0);
            instance = new NetworkServer();
        }

        public NetworkServer(int port = 8000)
        {
            this.log = new LogOutput($"Network Server ({port})");
            this.log.Setup();

            this.port = port;

            this.connections = new LockedDictionary<int, NetworkConnection>();
            this.requestFeatures = new LockedList<Type>();
        }

        public void Start()
        {
            if (isRunning)
                Stop();

            this.server = new Telepathy.Server(int.MaxValue - 10);
            this.server.NoDelay = isNoDelay;

            this.server.OnConnected = OnClientConnected;
            this.server.OnDisconnected = OnClientDisconnected;
            this.server.OnData = OnClientData;

            this.server.Start(port);

            this.isRunning = true;

            OnStarted.Call();
        }

        public void Stop()
        {
            if (!isRunning)
                throw new InvalidOperationException($"The server is not running!");

            this.server.Stop();

            this.server.OnConnected = null;
            this.server.OnDisconnected = null;
            this.server.OnData = null;

            this.server.NoDelay = false;

            this.server = null;

            this.isRunning = false;

            OnStopped.Call();
        }

        public void Add<T>() where T : NetworkFeature
        {
            if (requestFeatures.Contains(typeof(T)))
                return;

            requestFeatures.Add(typeof(T));
        }

        public void Remove<T>() where T : NetworkFeature
        {
            requestFeatures.Remove(typeof(T));
        }

        private void OnClientData(int connId, ArraySegment<byte> data)
        {
            if (!connections.TryGetValue(connId, out var connection))
                return;

            var array = data.ToArray();

            connection.Receive(array);

            OnData.Call(connection, array);
        }

        private void OnClientConnected(int connId)
        {
            var connection = new NetworkConnection(connId, this);

            connections[connId] = connection;

            OnConnected.Call(connection);
        }

        private void OnClientDisconnected(int connId)
        {
            if (!connections.TryGetValue(connId, out var connection))
                return;

            OnDisconnected.Call(connection);

            connection.Stop();

            connections.Remove(connId);
        }
    }
}
