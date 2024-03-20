using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Networking.Kcp
{
    public abstract class KcpPeer
    {
        internal KcpSocket kcp;
        internal uint cookie;

        protected KcpState state = KcpState.Connected;

        public const int DEFAULT_TIMEOUT = 10000;
        public int timeout;

        uint lastReceiveTime;

        readonly Stopwatch watch = new Stopwatch();

        readonly byte[] kcpMessageBuffer;
        readonly byte[] kcpSendBuffer;
        readonly byte[] rawSendBuffer;

        public const int PING_INTERVAL = 100;

        uint lastPingTime;

        internal const int QueueDisconnectThreshold = 10000;

        public int SendQueueCount => kcp.snd_queue.Count;
        public int ReceiveQueueCount => kcp.rcv_queue.Count;
        public int SendBufferCount => kcp.snd_buf.Count;
        public int ReceiveBufferCount => kcp.rcv_buf.Count;

        public const int CHANNEL_HEADER_SIZE = 1;
        public const int COOKIE_HEADER_SIZE = 4;
        public const int METADATA_SIZE = CHANNEL_HEADER_SIZE + COOKIE_HEADER_SIZE;

        static int ReliableMaxMessageSize_Unconstrained(int mtu, uint rcv_wnd) =>
            (mtu - KcpSocket.OVERHEAD - METADATA_SIZE) * ((int)rcv_wnd - 1) - 1;

        public static int ReliableMaxMessageSize(int mtu, uint rcv_wnd) =>
            ReliableMaxMessageSize_Unconstrained(mtu, Math.Min(rcv_wnd, KcpSocket.FRG_MAX));

        public static int UnreliableMaxMessageSize(int mtu) =>
            mtu - METADATA_SIZE - 1;

        public uint MaxSendRate => kcp.snd_wnd * kcp.mtu * 1000 / kcp.interval;
        public uint MaxReceiveRate => kcp.rcv_wnd * kcp.mtu * 1000 / kcp.interval;

        public readonly int unreliableMax;
        public readonly int reliableMax;

        protected KcpPeer(KcpConfig config, uint cookie)
        {
            Reset(config);

            this.cookie = cookie;

            KcpLog.Info($"[KCP] {GetType()}: created with cookie={cookie}");

            rawSendBuffer = new byte[config.Mtu];

            unreliableMax = UnreliableMaxMessageSize(config.Mtu);
            reliableMax = ReliableMaxMessageSize(config.Mtu, config.ReceiveWindowSize);

            kcpMessageBuffer = new byte[1 + reliableMax];
            kcpSendBuffer = new byte[1 + reliableMax];
        }

        protected void Reset(KcpConfig config)
        {
            cookie = 0;
            state = KcpState.Connected;

            lastReceiveTime = 0;
            lastPingTime = 0;

            watch.Restart(); 

            kcp = new KcpSocket(0, RawSendReliable);
            kcp.SetNoDelay(config.NoDelay ? 1u : 0u, config.Interval, config.FastResend, !config.CongestionWindow);
            kcp.SetWindowSize(config.SendWindowSize, config.ReceiveWindowSize);
            kcp.SetMtu((uint)config.Mtu - METADATA_SIZE);
            kcp.dead_link = config.MaxRetransmits;

            timeout = config.Timeout;
        }

        protected abstract void OnAuthenticated();
        protected abstract void OnData(ArraySegment<byte> message, KcpChannel channel);
        protected abstract void OnDisconnected();
        protected abstract void OnError(KcpErrorCode error, string message);
        protected abstract void RawSend(ArraySegment<byte> data);

        void HandleTimeout(uint time)
        {
            if (time >= lastReceiveTime + timeout)
            {
                OnError(KcpErrorCode.Timeout, $"{GetType()}: Connection timed out after not receiving any message for {timeout} ms. Disconnecting.");
                Disconnect();
            }
        }

        void HandleDeadLink()
        {
            if (kcp.state == -1)
            {
                OnError(KcpErrorCode.Timeout, $"{GetType()}: dead_link detected: a message was retransmitted {kcp.dead_link} times without ack. Disconnecting.");
                Disconnect();
            }
        }

        void HandlePing(uint time)
        {
            if (time >= lastPingTime + PING_INTERVAL)
            {
                SendPing();
                lastPingTime = time;
            }
        }

        void HandleChoked()
        {
            int total = kcp.rcv_queue.Count + kcp.snd_queue.Count +
                        kcp.rcv_buf.Count   + kcp.snd_buf.Count;

            if (total >= QueueDisconnectThreshold)
            {
                OnError(KcpErrorCode.Congestion,
                        $"{GetType()}: disconnecting connection because it can't process data fast enough.\n" +
                        $"Queue total {total}>{QueueDisconnectThreshold}. rcv_queue={kcp.rcv_queue.Count} snd_queue={kcp.snd_queue.Count} rcv_buf={kcp.rcv_buf.Count} snd_buf={kcp.snd_buf.Count}\n" +
                        $"* Try to Enable NoDelay, decrease INTERVAL, disable Congestion Window (= enable NOCWND!), increase SEND/RECV WINDOW or compress data.\n" +
                        $"* Or perhaps the network is simply too slow on our end, or on the other end.");

                kcp.snd_queue.Clear();
                Disconnect();
            }
        }

        bool ReceiveNextReliable(out KcpReliableHeader header, out ArraySegment<byte> message)
        {
            message = default;
            header = KcpReliableHeader.Ping;

            int msgSize = kcp.PeekSize();

            if (msgSize <= 0) 
                return false;

            if (msgSize > kcpMessageBuffer.Length)
            {
                OnError(KcpErrorCode.InvalidReceive, $"{GetType()}: possible allocation attack for msgSize {msgSize} > buffer {kcpMessageBuffer.Length}. Disconnecting the connection.");
                Disconnect();
                return false;
            }

            int received = kcp.Receive(kcpMessageBuffer, msgSize);

            if (received < 0)
            {
                OnError(KcpErrorCode.InvalidReceive, $"{GetType()}: Receive failed with error={received}. closing connection.");
                Disconnect();
                return false;
            }

            byte headerByte = kcpMessageBuffer[0];

            if (!KcpHeader.ParseReliable(headerByte, out header))
            {
                OnError(KcpErrorCode.InvalidReceive, $"{GetType()}: Receive failed to parse header: {headerByte} is not defined in {typeof(KcpReliableHeader)}.");
                Disconnect();
                return false;
            }

            message = new ArraySegment<byte>(kcpMessageBuffer, 1, msgSize - 1);
            lastReceiveTime = (uint)watch.ElapsedMilliseconds;
            return true;
        }

        void TickIncoming_Connected(uint time)
        {
            HandleTimeout(time);
            HandleDeadLink();
            HandlePing(time);
            HandleChoked();

            if (ReceiveNextReliable(out KcpReliableHeader header, out ArraySegment<byte> message))
            {
                switch (header)
                {
                    case KcpReliableHeader.Hello:
                        {
                            KcpLog.Info($"[KCP] {GetType()}: received hello with cookie={cookie}");
                            state = KcpState.Authenticated;
                            OnAuthenticated();
                            break;
                        }
                    case KcpReliableHeader.Ping:
                        {
                            break;
                        }
                    case KcpReliableHeader.Data:
                        {
                            OnError(KcpErrorCode.InvalidReceive, $"[KCP] {GetType()}: received invalid header {header} while Connected. Disconnecting the connection.");
                            Disconnect();
                            break;
                        }
                }
            }
        }

        void TickIncoming_Authenticated(uint time)
        {
            HandleTimeout(time);
            HandleDeadLink();
            HandlePing(time);
            HandleChoked();

            while (ReceiveNextReliable(out KcpReliableHeader header, out ArraySegment<byte> message))
            {
                switch (header)
                {
                    case KcpReliableHeader.Hello:
                        {
                            KcpLog.Warning($"{GetType()}: received invalid header {header} while Authenticated. Disconnecting the connection.");
                            Disconnect();
                            break;
                        }
                    case KcpReliableHeader.Data:
                        {
                            if (message.Count > 0)
                            {
                                OnData(message, KcpChannel.Reliable);
                            }
                            else
                            {
                                OnError(KcpErrorCode.InvalidReceive, $"{GetType()}: received empty Data message while Authenticated. Disconnecting the connection.");
                                Disconnect();
                            }
                            break;
                        }
                    case KcpReliableHeader.Ping:
                        {
                            break;
                        }
                }
            }
        }

        public virtual void TickIncoming()
        {
            uint time = (uint)watch.ElapsedMilliseconds;

            try
            {
                switch (state)
                {
                    case KcpState.Connected:
                        {
                            TickIncoming_Connected(time);
                            break;
                        }
                    case KcpState.Authenticated:
                        {
                            TickIncoming_Authenticated(time);
                            break;
                        }
                    case KcpState.Disconnected:
                        {
                            break;
                        }
                }
            }
            catch (SocketException exception)
            {
                OnError(KcpErrorCode.ConnectionClosed, $"{GetType()}: Disconnecting because {exception}. This is fine.");
                Disconnect();
            }
            catch (ObjectDisposedException exception)
            {
                OnError(KcpErrorCode.ConnectionClosed, $"{GetType()}: Disconnecting because {exception}. This is fine.");
                Disconnect();
            }
            catch (Exception exception)
            {
                OnError(KcpErrorCode.Unexpected, $"{GetType()}: unexpected Exception: {exception}");
                Disconnect();
            }
        }

        public virtual void TickOutgoing()
        {
            uint time = (uint)watch.ElapsedMilliseconds;

            try
            {
                switch (state)
                {
                    case KcpState.Connected:
                    case KcpState.Authenticated:
                        {
                            kcp.Update(time);
                            break;
                        }
                    case KcpState.Disconnected:
                        {
                            break;
                        }
                }
            }
            catch (SocketException exception)
            {
                OnError(KcpErrorCode.ConnectionClosed, $"{GetType()}: Disconnecting because {exception}. This is fine.");
                Disconnect();
            }
            catch (ObjectDisposedException exception)
            {
                OnError(KcpErrorCode.ConnectionClosed, $"{GetType()}: Disconnecting because {exception}. This is fine.");
                Disconnect();
            }
            catch (Exception exception)
            {
                OnError(KcpErrorCode.Unexpected, $"{GetType()}: unexpected exception: {exception}");
                Disconnect();
            }
        }

        protected void OnRawInputReliable(ArraySegment<byte> message)
        {
            int input = kcp.Input(message.Array, message.Offset, message.Count);

            if (input != 0)
            {
                KcpLog.Warning($"[KCP] {GetType()}: Input failed with error={input} for buffer with length={message.Count - 1}");
            }
        }

        protected void OnRawInputUnreliable(ArraySegment<byte> message)
        {
            if (message.Count < 1) 
                return;

            byte headerByte = message.Array[message.Offset + 0];

            if (!KcpHeader.ParseUnreliable(headerByte, out KcpUnreliableHeader header))
            {
                OnError(KcpErrorCode.InvalidReceive, $"{GetType()}: Receive failed to parse header: {headerByte} is not defined in {typeof(KcpUnreliableHeader)}.");
                Disconnect();
                return;
            }

            message = new ArraySegment<byte>(message.Array, message.Offset + 1, message.Count - 1);

            switch (header)
            {
                case KcpUnreliableHeader.Data:
                    {
                        if (state == KcpState.Authenticated)
                        {
                            OnData(message, KcpChannel.Unreliable);
                            lastReceiveTime = (uint)watch.ElapsedMilliseconds;
                        }
                        else
                        {
                        }

                        break;
                    }
                case KcpUnreliableHeader.Disconnect:
                    {
                        KcpLog.Info($"[KCP] {GetType()}: received disconnect message");
                        Disconnect();
                        break;
                    }
            }
        }

        void RawSendReliable(byte[] data, int length)
        {
            rawSendBuffer[0] = (byte)KcpChannel.Reliable;
            KcpUtils.Encode32U(rawSendBuffer, 1, cookie);
            Buffer.BlockCopy(data, 0, rawSendBuffer, 1 + 4, length);
            ArraySegment<byte> segment = new ArraySegment<byte>(rawSendBuffer, 0, length + 1+4);
            RawSend(segment);
        }

        void SendReliable(KcpReliableHeader header, ArraySegment<byte> content)
        {
            if (1 + content.Count > kcpSendBuffer.Length) 
            {
                OnError(KcpErrorCode.InvalidSend, $"{GetType()}: Failed to send reliable message of size {content.Count} because it's larger than ReliableMaxMessageSize={reliableMax}");
                return;
            }

            kcpSendBuffer[0] = (byte)header;

            if (content.Count > 0)
                Buffer.BlockCopy(content.Array, content.Offset, kcpSendBuffer, 1, content.Count);

            int sent = kcp.Send(kcpSendBuffer, 0, 1 + content.Count);

            if (sent < 0)
            {
                OnError(KcpErrorCode.InvalidSend, $"{GetType()}: Send failed with error={sent} for content with length={content.Count}");
            }
        }

        void SendUnreliable(KcpUnreliableHeader header, ArraySegment<byte> content)
        {
            if (content.Count > unreliableMax)
            {
                KcpLog.Error($"[KCP] {GetType()}: Failed to send unreliable message of size {content.Count} because it's larger than UnreliableMaxMessageSize={unreliableMax}");
                return;
            }

            rawSendBuffer[0] = (byte)KcpChannel.Unreliable;
            KcpUtils.Encode32U(rawSendBuffer, 1, cookie); 
            rawSendBuffer[5] = (byte)header;

            if (content.Count > 0)
                Buffer.BlockCopy(content.Array, content.Offset, rawSendBuffer, 1 + 4 + 1, content.Count);

            ArraySegment<byte> segment = new ArraySegment<byte>(rawSendBuffer, 0, content.Count + 1 + 4 + 1);
            RawSend(segment);
        }

        public void SendHello()
        {
            KcpLog.Info($"[KCP] {GetType()}: sending handshake to other end with cookie={cookie}");
            SendReliable(KcpReliableHeader.Hello, default);
        }

        public void SendData(ArraySegment<byte> data, KcpChannel channel)
        {
            if (data.Count == 0)
            {
                OnError(KcpErrorCode.InvalidSend, $"{GetType()}: tried sending empty message. This should never happen. Disconnecting.");
                Disconnect();
                return;
            }

            switch (channel)
            {
                case KcpChannel.Reliable:
                    SendReliable(KcpReliableHeader.Data, data);
                    break;
                case KcpChannel.Unreliable:
                    SendUnreliable(KcpUnreliableHeader.Data, data);
                    break;
            }
        }

        void SendPing() 
            => SendReliable(KcpReliableHeader.Ping, default);

        void SendDisconnect()
        {
            for (int i = 0; i < 5; ++i)
                SendUnreliable(KcpUnreliableHeader.Disconnect, default);
        }

        public virtual void Disconnect()
        {
            if (state == KcpState.Disconnected)
                return;

            try
            {
                SendDisconnect();
            }
            catch (SocketException)
            {

            }
            catch (ObjectDisposedException)
            {

            }

            KcpLog.Info($"[KCP] {GetType()}: Disconnected.");
            state = KcpState.Disconnected;
            OnDisconnected();
        }
    }
}
