using System;
using System.Net.Sockets;
using System.Net;

namespace Networking.Kcp
{
    public class KcpClient : KcpPeer
    {
        protected Socket socket;
        public EndPoint remoteEndPoint;

        public EndPoint LocalEndPoint => socket?.LocalEndPoint;

        protected readonly KcpConfig config;
        protected readonly byte[] rawReceiveBuffer;

        protected readonly Action OnConnectedCallback;
        protected readonly Action<ArraySegment<byte>, KcpChannel> OnDataCallback;
        protected readonly Action OnDisconnectedCallback;
        protected readonly Action<KcpErrorCode, string> OnErrorCallback;

        bool active = false;
        public bool connected;

        public KcpClient(Action OnConnected,
                         Action<ArraySegment<byte>, KcpChannel> OnData,
                         Action OnDisconnected,
                         Action<KcpErrorCode, string> OnError,
                         KcpConfig config)
                         : base(config, 0) 
        {
            OnConnectedCallback = OnConnected;
            OnDataCallback = OnData;
            OnDisconnectedCallback = OnDisconnected;
            OnErrorCallback = OnError;

            this.config = config;

            rawReceiveBuffer = new byte[config.Mtu];
        }

        protected override void OnAuthenticated()
        {
            KcpLog.Info($"[KCP] Client: OnConnected");
            connected = true;
            OnConnectedCallback();
        }

        protected override void OnData(ArraySegment<byte> message, KcpChannel channel) =>
            OnDataCallback(message, channel);

        protected override void OnError(KcpErrorCode error, string message) =>
            OnErrorCallback(error, message);

        protected override void OnDisconnected()
        {
            KcpLog.Info($"[KCP] Client: OnDisconnected");
            connected = false;
            socket?.Close();
            socket = null;
            remoteEndPoint = null;
            OnDisconnectedCallback();
            active = false;
        }

        public void Connect(string address, ushort port)
        {
            if (connected)
            {
                KcpLog.Warning("[KCP] Client: already connected!");
                return;
            }

            if (!KcpCommon.ResolveHostname(address, out IPAddress[] addresses))
            {
                OnError(KcpErrorCode.DnsResolve, $"Failed to resolve host: {address}");
                OnDisconnectedCallback();
                return;
            }

            Reset(config);

            KcpLog.Info($"[KCP] Client: connect to {address}:{port}");

            remoteEndPoint = new IPEndPoint(addresses[0], port);
            socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            active = true;

            socket.Blocking = false;

            KcpCommon.ConfigureSocketBuffers(socket, config.RecvBufferSize, config.SendBufferSize);

            socket.Connect(remoteEndPoint);

            SendHello();
        }

        protected virtual bool RawReceive(out ArraySegment<byte> segment)
        {
            segment = default;

            if (socket == null) 
                return false;

            try
            {
                return socket.ReceiveNonBlocking(rawReceiveBuffer, out segment);
            }
            catch (SocketException e)
            {
                KcpLog.Info($"[KCP] Client.RawReceive: looks like the other end has closed the connection. This is fine: {e}");
                base.Disconnect();
                return false;
            }
        }

        protected override void RawSend(ArraySegment<byte> data)
        {
            if (socket == null) 
                return;

            try
            {
                socket.SendNonBlocking(data);
            }
            catch (SocketException e)
            {
                KcpLog.Info($"[KCP] Client.RawSend: looks like the other end has closed the connection. This is fine: {e}");
            }
        }

        public void Send(ArraySegment<byte> segment, KcpChannel channel)
        {
            if (!connected)
            {
                KcpLog.Warning("[KCP] Client: can't send because not connected!");
                return;
            }

            SendData(segment, channel);
        }

        public void RawInput(ArraySegment<byte> segment)
        {
            if (segment.Count <= 5) 
                return;

            byte channel = segment.Array[segment.Offset + 0];

            KcpUtils.Decode32U(segment.Array, segment.Offset + 1, out uint messageCookie);

            if (messageCookie == 0)
            {
                KcpLog.Error($"[KCP] Client: received message with cookie=0, this should never happen. Server should always include the security cookie.");
            }

            if (cookie == 0)
            {
                cookie = messageCookie;
                KcpLog.Info($"[KCP] Client: received initial cookie: {cookie}");
            }
            else if (cookie != messageCookie)
            {
                KcpLog.Warning($"[KCP] Client: dropping message with mismatching cookie: {messageCookie} expected: {cookie}.");
                return;
            }

            ArraySegment<byte> message = new ArraySegment<byte>(segment.Array, segment.Offset + 1+4, segment.Count - 1-4);

            switch (channel)
            {
                case (byte)KcpChannel.Reliable:
                    {
                        OnRawInputReliable(message);
                        break;
                    }
                case (byte)KcpChannel.Unreliable:
                    {
                        OnRawInputUnreliable(message);
                        break;
                    }
                default:
                    {
                        KcpLog.Warning($"[KCP] Client: invalid channel header: {channel}, likely internet noise");
                        break;
                    }
            }
        }

        public override void TickIncoming()
        {
            if (active)
            {
                while (RawReceive(out ArraySegment<byte> segment))
                    RawInput(segment);
            }

            if (active) 
                base.TickIncoming();
        }

        public override void TickOutgoing()
        {
            if (active) 
                base.TickOutgoing();
        }

        public virtual void Tick()
        {
            TickIncoming();
            TickOutgoing();
        }
    }
}