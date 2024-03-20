using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;

namespace Networking.Kcp
{
    public class KcpServer
    {
        protected readonly Action<int> OnConnected;
        protected readonly Action<int, ArraySegment<byte>, KcpChannel> OnData;
        protected readonly Action<int> OnDisconnected;
        protected readonly Action<int, KcpErrorCode, string> OnError;

        protected readonly KcpConfig config;
        protected Socket socket;
        
        EndPoint newClientEP;

        public EndPoint LocalEndPoint => socket?.LocalEndPoint;

        protected readonly byte[] rawReceiveBuffer;

        public Dictionary<int, KcpConnection> connections =
            new Dictionary<int, KcpConnection>();

        public KcpServer(Action<int> OnConnected,
                         Action<int, ArraySegment<byte>, KcpChannel> OnData,
                         Action<int> OnDisconnected,
                         Action<int, KcpErrorCode, string> OnError,
                         KcpConfig config)
        {
            this.OnConnected = OnConnected;
            this.OnData = OnData;
            this.OnDisconnected = OnDisconnected;
            this.OnError = OnError;
            this.config = config;

            rawReceiveBuffer = new byte[config.Mtu];
            newClientEP = config.DualMode
                          ? new IPEndPoint(IPAddress.IPv6Any, 0)
                          : new IPEndPoint(IPAddress.Any, 0);
        }

        public virtual bool IsActive() 
            => socket != null;

        static Socket CreateServerSocket(bool DualMode, ushort port)
        {
            if (DualMode)
            {
                Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

                try
                {
                    socket.DualMode = true;
                }
                catch (NotSupportedException e)
                {
                    KcpLog.Warning($"[KCP] Failed to set Dual Mode, continuing with IPv6 without Dual Mode. Error: {e}");
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const uint IOC_IN = 0x80000000U;
                    const uint IOC_VENDOR = 0x18000000U;
                    const int SIO_UDP_CONNRESET = unchecked((int)(IOC_IN | IOC_VENDOR | 12));

                    socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0x00 }, null);
                }

                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                return socket;
            }
            else
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                return socket;
            }
        }

        public virtual void Start(ushort port)
        {
            if (socket != null)
            {
                KcpLog.Warning("[KCP] Server: already started!");
                return;
            }

            socket = CreateServerSocket(config.DualMode, port);
            socket.Blocking = false;

            KcpCommon.ConfigureSocketBuffers(socket, config.RecvBufferSize, config.SendBufferSize);
        }

        public void Send(int connectionId, ArraySegment<byte> segment, KcpChannel channel)
        {
            if (connections.TryGetValue(connectionId, out KcpConnection connection))
            {
                connection.SendData(segment, channel);
            }
        }

        public void Disconnect(int connectionId)
        {
            if (connections.TryGetValue(connectionId, out KcpConnection connection))
            {
                connection.Disconnect();
            }
        }

        // expose the whole IPEndPoint, not just the IP address. some need it.
        public IPEndPoint GetClientEndPoint(int connectionId)
        {
            if (connections.TryGetValue(connectionId, out KcpConnection connection))
            {
                return connection.remoteEndPoint as IPEndPoint;
            }

            return null;
        }

        protected virtual bool RawReceiveFrom(out ArraySegment<byte> segment, out int connectionId)
        {
            segment = default;
            connectionId = 0;

            if (socket == null) 
                return false;

            try
            {
                if (socket.ReceiveFromNonBlocking(rawReceiveBuffer, out segment, ref newClientEP))
                {
                    connectionId = KcpCommon.ConnectionHash(newClientEP);
                    return true;
                }
            }
            catch (SocketException e)
            {
                KcpLog.Info($"[KCP] Server: ReceiveFrom failed: {e}");
            }

            return false;
        }

        protected virtual void RawSend(int connectionId, ArraySegment<byte> data)
        {
            if (!connections.TryGetValue(connectionId, out KcpConnection connection))
            {
                KcpLog.Warning($"[KCP] Server: RawSend invalid connectionId={connectionId}");
                return;
            }

            try
            {
                socket.SendToNonBlocking(data, connection.remoteEndPoint);
            }
            catch (SocketException e)
            {
                KcpLog.Error($"[KCP] Server: SendTo failed: {e}");
            }
        }

        protected virtual KcpConnection CreateConnection(int connectionId)
        {
            uint cookie = KcpCommon.GenerateCookie();

            KcpConnection connection = new KcpConnection(
                OnConnectedCallback,
                (message,  channel) => OnData(connectionId, message, channel),
                OnDisconnectedCallback,
                (error, reason) => OnError(connectionId, error, reason),
                (data) => RawSend(connectionId, data),
                config,
                cookie,
                newClientEP);

            return connection;

            void OnConnectedCallback(KcpConnection conn)
            {
                connections.Add(connectionId, conn);

                KcpLog.Info($"[KCP] Server: added connection({connectionId})");
                KcpLog.Info($"[KCP] Server: OnConnected({connectionId})");

                OnConnected(connectionId);
            }

            void OnDisconnectedCallback()
            {
                connectionsToRemove.Add(connectionId);
                KcpLog.Info($"[KCP] Server: OnDisconnected({connectionId})");
                OnDisconnected(connectionId);
            }
        }

        void ProcessMessage(ArraySegment<byte> segment, int connectionId)
        {
            if (!connections.TryGetValue(connectionId, out KcpConnection connection))
            {
                connection = CreateConnection(connectionId);

                connection.RawInput(segment);
                connection.TickIncoming();
            }
            else
            {
                connection.RawInput(segment);
            }
        }

        readonly HashSet<int> connectionsToRemove = new HashSet<int>();

        public virtual void TickIncoming()
        {
            while (RawReceiveFrom(out ArraySegment<byte> segment, out int connectionId))
            {
                ProcessMessage(segment, connectionId);
            }

            foreach (KcpConnection connection in connections.Values)
            {
                connection.TickIncoming();
            }

            foreach (int connectionId in connectionsToRemove)
            {
                connections.Remove(connectionId);
            }

            connectionsToRemove.Clear();
        }

        public virtual void TickOutgoing()
        {
            foreach (KcpConnection connection in connections.Values)
            {
                connection.TickOutgoing();
            }
        }

        public virtual void Tick()
        {
            TickIncoming();
            TickOutgoing();
        }

        public virtual void Stop()
        {
            connections.Clear();
            socket?.Close();
            socket = null;
        }
    }
}
