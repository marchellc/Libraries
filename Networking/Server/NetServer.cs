using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;

using Networking.Enums;
using Networking.Interfaces;
using Networking.Peer;
using Networking.Utilities;

using WatsonTcp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Networking.Server
{
    public class NetServer : IServer
    {
        private WatsonTcpServer server;

        private LockedDictionary<Guid, NetPeer> peers;
        private LockedList<Type> preloads;

        public readonly LogOutput Log;

        public IPEndPoint LocalAddress { get; set; }
        public IPEndPoint RemoteAddress => throw new InvalidOperationException($"Cannot access the remote IP on the server.");

        public ISender Sender => throw new InvalidOperationException($"Cannot access the ISender object on the server.");

        public ClientType Type => ClientType.Server;

        public bool IsRunning => server != null && server.IsListening;
        public bool IsConnected => server != null && server.IsListening && Peers.Count > 0;

        public IReadOnlyCollection<IPeer> Peers => peers.Values.ToList();

        public IComponent[] Components => throw new InvalidOperationException($"Cannot access the IComponent[] object on the server.");

        public int Port
        {
            get => LocalAddress?.Port ?? -1;
            set => LocalAddress = new IPEndPoint(IPAddress.Any, value);
        }

        public event Action<NetPeer> OnConnected;
        public event Action<NetPeer> OnDisconnected;

        public static NetServer Instance { get; } = new NetServer();

        public NetServer()
        {
            peers = new LockedDictionary<Guid, NetPeer>();
            preloads = new LockedList<Type>();

            Log = new LogOutput("NetServer");
            Log.Setup();
        }

        public T Get<T>() where T : IComponent
            => throw new InvalidOperationException($"Cannot get component instances on the server.");

        public void Send(byte[] data)
            => throw new InvalidOperationException($"You need to use the Send(string, byte[]) overload for the server!");

        public void Add<T>() where T : IComponent
        {
            if (!preloads.Contains(typeof(T)))
            {
                preloads.Add(typeof(T));
                Log.Verbose($"Added component type to peer preloads: {typeof(T).FullName}");
            }
        }

        public bool Remove<T>() where T : IComponent
        {
            if (preloads.Remove(typeof(T)))
            {
                Log.Verbose($"Removed component type from peer preloads: {typeof(T).FullName}");
                return true;
            }

            return false;
        }

        public void Send(Guid id, byte[] data)
        {
            if (!IsRunning)
            {
                Log.Warn($"Attempted to send data over an inactive socket.");
                return;
            }

            try
            {
                Task.Run(async () => await server.SendAsync(id, data));
            }
            catch (Exception ex)
            {
                Log.Error($"The TCP server failed to send data to clientId={id}!\n{ex} ");
            }
        }

        public void Start()
        {
            if (server != null)
                Stop();

            if (LocalAddress is null || Port < 0)
                throw new InvalidOperationException($"Cannot start the server without a port.");

            Log.Info($"Starting a new server socket on port: {Port}");

            try
            {
                server = new WatsonTcpServer(null, Port);

                server.Settings.NoDelay = true;

                server.Keepalive.EnableTcpKeepAlives = true;
                server.Keepalive.TcpKeepAliveInterval = 1;
                server.Keepalive.TcpKeepAliveTime = 1;

                server.Events.MessageReceived += OnClientData;
                server.Events.ClientConnected += OnClientConnected;
                server.Events.ClientDisconnected += OnClientDisconnected;

                server.Start();

                Log.Name = $"NetServer ({Port})";
                Log.Info($"Server socket started! Listening on port {Port}.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start a new Telepathy server on '{LocalAddress}'!\n{ex}");
            }
        }

        public void Stop()
        {
            if (server is null)
                return;

            try
            {
                Log.Info($"Stopping the server socket on port {Port} ..");

                server.Events.MessageReceived -= OnClientData;
                server.Events.ClientConnected -= OnClientConnected;
                server.Events.ClientDisconnected -= OnClientDisconnected;

                foreach (var peer in peers)
                {
                    try
                    {
                        peer.Value.Stop();
                    }
                    catch (Exception ex)
                    {
                        Log?.Error($"Failed to stop peer '{peer.Key}':\n{ex}");
                    }
                }

                peers.Clear();

                server.Stop();
                server.Dispose();
                server = null;

                preloads?.Clear();
                preloads = null;

                peers?.Clear();
                peers = null;

                Log.Name = "NetServer";
                Log.Warn($"Stopped the server socket on port '{Port}'!");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to stop the currently active Telepathy server!\n{ex}");
            }
        }

        private void OnClientConnected(object _, ConnectionEventArgs ev)
        {
            if (peers.ContainsKey(ev.Client.Guid))
            {
                Log.Warn($"Received connect for an already connected clientId={ev.Client.Guid}");
                return;
            }

            try
            {
                var address = IpParser.ParseEndPoint(ev.Client.IpPort);
                var peer = new NetPeer(this, ev.Client.Guid, address, LocalAddress, preloads);

                Log.Info($"Accepted an incoming connection from address={address}");

                peer.Start();

                peers[ev.Client.Guid] = peer;

                OnConnected.Call(peer);
            }
            catch (Exception ex)
            {
                Log.Error($"Peer '{ev.Client.Guid}' failed to process connection!\n{ex}");
            }
        }

        private void OnClientDisconnected(object _, DisconnectionEventArgs ev)
        {
            if (!peers.TryGetValue(ev.Client.Guid, out var peer))
            {
                Log.Warn($"Received disconnect for an unknown clientId={ev.Client.IpPort}");
                return;
            }

            OnDisconnected.Call(peer);

            try
            {
                peer.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Peer '{peer.Id}' failed to process disconnect!\n{ex}");
            }

            if (!peers.Remove(ev.Client.Guid))
                Log.Warn($"Failed to remove peer clientId={ev.Client.Guid} from the peering dictionary!");

            Log.Info($"Client clientId={ev.Client.Guid} has disconnected from address={peer.RemoteAddress}");
        }

        private void OnClientData(object _, MessageReceivedEventArgs ev)
        {
            if (!peers.TryGetValue(ev.Client.Guid, out var peer))
            {
                Log.Warn($"Received data for an unknown clientId={ev.Client.Guid}");
                return;
            }

            try
            {
                peer.Process(ev.Data);
            }
            catch (Exception ex)
            {
                Log.Error($"Peer '{peer.Id}' failed to process incoming data!\n{ex}");
            }
        }
    }
}