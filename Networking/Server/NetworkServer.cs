using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using Common.Utilities;
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

            log.Info($"Starting the server ..");

            try
            {
                this.server = new Telepathy.Server(1024 * 1024 * 10);
                this.server.NoDelay = true;

                this.server.OnConnected = OnClientConnected;
                this.server.OnDisconnected = OnClientDisconnected;
                this.server.OnData = OnClientData;

                this.isRunning = true;

                CodeUtils.WhileTrue(() => isRunning, () =>
                {
                    this.server.Tick(100);
                }, 100);

                this.server.Start(port);

                OnStarted.Call();

                log.Info($"Server started.");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public void Stop()
        {
            if (!isRunning)
                throw new InvalidOperationException($"The server is not running!");

            log.Info($"Stopping the server ..");

            this.server.Stop();

            this.server.OnConnected = null;
            this.server.OnDisconnected = null;
            this.server.OnData = null;

            this.server.NoDelay = false;

            this.server = null;

            this.isRunning = false;

            OnStopped.Call();

            log.Info($"Server stopped.");
        }

        public void Add<T>() where T : NetworkFeature
        {
            if (requestFeatures.Contains(typeof(T)))
                return;

            requestFeatures.Add(typeof(T));

            log.Trace($"Added feature: {typeof(T).FullName}");
        }

        public void Remove<T>() where T : NetworkFeature
        {
            if (requestFeatures.Remove(typeof(T)))
                log.Trace($"Removed feature: {typeof(T).FullName}");
        }

        private void OnClientData(int connId, ArraySegment<byte> data)
        {
            if (!connections.TryGetValue(connId, out var connection))
                return;

            log.Trace($"Received client data connId={connId}");

            var array = data.ToArray();

            connection.Receive(array);

            OnData.Call(connection, array);
        }

        private void OnClientConnected(int connId)
        {
            var connection = new NetworkConnection(connId, this, isNoDelay);

            connections[connId] = connection;

            OnConnected.Call(connection);

            log.Info($"Client connected from {connection.remote} connId={connId}");
        }

        private void OnClientDisconnected(int connId)
        {
            if (!connections.TryGetValue(connId, out var connection))
                return;

            OnDisconnected.Call(connection);

            log.Info($"Client disconnected from {connection.remote} connId={connId}");

            connection.Stop();

            connections.Remove(connId);
        }
    }
}
