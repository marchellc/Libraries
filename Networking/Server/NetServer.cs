using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using Common.Utilities;

using Networking.Enums;
using Networking.Interfaces;
using Networking.Kcp;
using Networking.Peer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Networking.Server
{
    public class NetServer : IServer
    {
        private static readonly KcpConfig config;

        private KcpServer server;

        private LockedDictionary<int, NetPeer> peers;
        private LockedList<Type> preloads;

        public readonly LogOutput Log;

        public IPEndPoint LocalAddress { get; set; }
        public IPEndPoint RemoteAddress => throw new InvalidOperationException($"Cannot access the remote IP on the server.");

        public ISender Sender => throw new InvalidOperationException($"Cannot access the ISender object on the server.");

        public ClientType Type => ClientType.Server;

        public bool IsRunning => server != null;
        public bool IsConnected => server != null && server.connections.Count > 0;

        public IReadOnlyCollection<IPeer> Peers => peers.Values.ToList();

        public IComponent[] Components => throw new InvalidOperationException($"Cannot access the IComponent[] object on the server.");

        public int Port
        {
            get => LocalAddress?.Port ?? -1;
            set => LocalAddress = new IPEndPoint(IPAddress.Any, value);
        }

        public event Action<NetPeer> OnConnected;
        public event Action<NetPeer> OnDisconnected;

        public static NetServer Instance { get; }

        static NetServer()
        {
            config = new KcpConfig(
                        NoDelay: true,
                        DualMode: false,
                        CongestionWindow: false,

                        Interval: 10,
                        Timeout: 5000,

                        SendWindowSize: KcpSocket.WND_SND * 1000,
                        ReceiveWindowSize: KcpSocket.WND_RCV * 1000,
                        MaxRetransmits: KcpSocket.DEADLINK * 2);

            Instance = new NetServer();
        }

        public NetServer()
        {
            peers = new LockedDictionary<int, NetPeer>();
            preloads = new LockedList<Type>();

            Log = new LogOutput("NetServer");
            Log.Setup();

            CodeUtils.WhileTrue(() => true, Tick, (int)config.Interval);
        }

        public T Get<T>() where T : IComponent
            => throw new InvalidOperationException($"Cannot get component instances on the server.");

        public void Send(byte[] data)
            => throw new InvalidOperationException($"You need to use the Send(int, byte[]) overload for the server!");

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

        public void Send(int id, byte[] data)
        {
            if (!IsRunning)
            {
                Log.Warn($"Attempted to send data over an inactive socket.");
                return;
            }

            try
            {
                Log.Verbose($"Sending {data.Length} bytes to clientId={id}");
                server.Send(id, data.ToSegment(), KcpChannel.Reliable);
            }
            catch (Exception ex)
            {
                Log.Error($"The KCP server failed to send data to clientId={id}!\n{ex} ");
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
                server = new KcpServer(
                    OnClientConnected,
                    OnClientData,
                    OnClientDisconnected,
                    OnClientError,

                    config);

                server.Start((ushort)LocalAddress.Port);

                Log.Name = $"NetServer ({Port})";
                Log.Info($"Server socket started! Listening on port {Port}.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start a new KCP server on '{LocalAddress}'!\n{ex}");
            }
        }

        public void Stop()
        {
            if (server is null)
                return;

            try
            {
                Log.Info($"Stopping the server socket on port {Port} ..");

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
                Log.Error($"Failed to stop the currently active KCP server!\n{ex}");
            }
        }

        private void OnClientConnected(int clientId)
        {
            if (peers.ContainsKey(clientId))
            {
                Log.Warn($"Received connect for an already connected clientId={clientId}");
                return;
            }

            try
            {
                var address = server.GetClientEndPoint(clientId);
                var peer = new NetPeer(this, clientId, address, LocalAddress, preloads);

                Log.Info($"Accepted an incoming connection from address={address}");

                peer.Start();

                peers[clientId] = peer;

                OnConnected.Call(peer);
            }
            catch (Exception ex)
            {
                Log.Error($"Peer '{clientId}' failed to process connection!\n{ex}");
            }
        }

        private void OnClientDisconnected(int clientId)
        {
            if (!peers.TryGetValue(clientId, out var peer))
            {
                Log.Warn($"Received disconnect for an unknown clientId={clientId}");
                return;
            }

            OnDisconnected.Call(peer);

            try
            {
                peer.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Peer '{clientId}' failed to process disconnect!\n{ex}");
            }

            if (!peers.Remove(clientId))
                Log.Warn($"Failed to remove peer clientId={clientId} from the peering dictionary!");

            Log.Info($"Client clientId={clientId} has disconnected from address={peer.RemoteAddress}");
        }

        private void OnClientData(int clientId, ArraySegment<byte> data, KcpChannel channel)
        {
            if (!peers.TryGetValue(clientId, out var peer))
            {
                Log.Warn($"Received data for an unknown clientId={clientId}");
                return;
            }

            if (channel != KcpChannel.Reliable)
            {
                Log.Warn($"Received {data.Count} bytes for clientId={clientId} on the unreliable channel.");
                return;
            }

            Log.Verbose($"Received {data.Count} bytes from clientId={clientId} @ {channel}");

            try
            {
                peer.Process(data.ToArray());
            }
            catch (Exception ex)
            {
                Log.Error($"Peer '{peer.Id}' failed to process incoming data!\n{ex}");
            }
        }

        private void OnClientError(int clientId, KcpErrorCode errorCode, string msg)
        {
            Log.Warn($"Caught an error in clientId={clientId} ({errorCode}): {msg ?? "no message"}");
        }

        private void Tick()
        {
            if (server is null)
                return;

            try
            {
                server.Tick();
            }
            catch { }
        }
    }
}