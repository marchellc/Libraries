using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using Common.Utilities;

using Networking.Features;
using Networking.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Server
{
    public class NetworkServer
    {
        private readonly LockedDictionary<int, NetworkConnection> connections = new LockedDictionary<int, NetworkConnection>();
        private readonly LockedList<Type> features = new LockedList<Type>();

        public LogOutput Log { get; }
        public ClientConfig Config { get; } = new ClientConfig();

        public Telepathy.Server ApiServer { get; private set; }

        public IReadOnlyDictionary<int, NetworkConnection> Connections
        {
            get => connections.ToDictionary();
        }

        public IReadOnlyList<Type> Features
        {
            get => features.ToList();
        }

        public int Port { get; set; } = 8000;
        public int MaxConnections { get; set; } = -1;

        public bool IsRunning { get; private set; }

        public event Action OnStarted;
        public event Action OnStopped;

        public event Action<NetworkConnection> OnConnected;
        public event Action<NetworkConnection> OnDisconnected;
        public event Action<NetworkConnection, byte[]> OnData;

        public static Version ServerVersion { get; }
        public static NetworkServer Instance { get; }

        static NetworkServer()
        {
            ServerVersion = new Version(1, 0, 0, 0);
            Instance = new NetworkServer();

            TypeLoader.Init();
        }

        public NetworkServer(int port = 8000)
        {
            Log = new LogOutput($"Network Server ({port})");
            Log.Setup();

            Port = port;
        }

        public void Start()
        {
            if (IsRunning)
                Stop();

            Log.Info($"Starting the server ..");

            try
            {
                ApiServer = new Telepathy.Server(1024 * 1024 * 10);
                ApiServer.NoDelay = true;

                ApiServer.OnConnected = OnClientConnected;
                ApiServer.OnDisconnected = OnClientDisconnected;
                ApiServer.OnData = OnClientData;

                IsRunning = true;

                CodeUtils.WhileTrue(() => IsRunning, () =>
                {
                    ApiServer.Tick(100);
                }, 100);

                ApiServer.Start(Port);

                OnStarted.Call();

                Log.Info($"Server started.");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void Stop()
        {
            if (!IsRunning)
                throw new InvalidOperationException($"The server is not running!");

            Log.Info($"Stopping the server ..");

            ApiServer.Stop();

            ApiServer.OnConnected = null;
            ApiServer.OnDisconnected = null;
            ApiServer.OnData = null;

            ApiServer.NoDelay = false;

            ApiServer = null;

            IsRunning = false;

            OnStopped.Call();

            Log.Info($"Server stopped.");
        }

        public void Add<T>() where T : NetworkFeature
        {
            if (features.Contains(typeof(T)))
                return;

            features.Add(typeof(T));

            Log.Verbose($"Added feature: {typeof(T).FullName}");
        }

        public void Remove<T>() where T : NetworkFeature
        {
            if (features.Remove(typeof(T)))
                Log.Trace($"Removed feature: {typeof(T).FullName}");
        }

        private void OnClientData(int connId, ArraySegment<byte> data)
        {
            if (!connections.TryGetValue(connId, out var connection))
                return;

            Log.Verbose($"Received client data connId={connId}");

            var array = data.ToArray();

            connection.Receive(array);

            OnData.Call(connection, array);
        }

        private void OnClientConnected(int connId)
        {
            if (MaxConnections > 0 && connections.Count >= MaxConnections)
            {
                ApiServer.Disconnect(connId);
                return;
            }

            CodeUtils.Delay(() =>
            {

                var connection = new NetworkConnection(connId, this);

                connection.Config.MaxReconnectionAttempts = Config.MaxReconnectionAttempts;

                connection.Config.OutgoingAmount = Config.OutgoingAmount;
                connection.Config.IncomingAmount = Config.IncomingAmount;

                connections[connId] = connection;

                OnConnected.Call(connection);

                Log.Info($"Client connected from {connection.EndPoint} connId={connId}");
            }, 250);
        }

        private void OnClientDisconnected(int connId)
        {
            if (!connections.TryGetValue(connId, out var connection))
                return;

            OnDisconnected.Call(connection);

            var remoteStr = connection.EndPoint.ToString();

            try
            {
                connection.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to stop the network connection!\n{ex}");
            }

            connections.Remove(connId);

            Log.Info($"Client disconnected from {remoteStr} connId={connId}");
        }
    }
}
