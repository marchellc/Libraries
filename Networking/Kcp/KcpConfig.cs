namespace Networking.Kcp
{
    public class KcpConfig
    {
        public bool DualMode;
        public int RecvBufferSize;
        public int SendBufferSize;
        public int Mtu;
        public bool NoDelay;
        public uint Interval;
        public int FastResend;
        public bool CongestionWindow;
        public uint SendWindowSize;
        public uint ReceiveWindowSize;
        public int Timeout;
        public uint MaxRetransmits;

        public KcpConfig(
            bool DualMode = true,
            int RecvBufferSize = 1024 * 1024 * 7,
            int SendBufferSize = 1024 * 1024 * 7,
            int Mtu = KcpSocket.MTU_DEF,
            bool NoDelay = true,
            uint Interval = 10,
            int FastResend = 0,
            bool CongestionWindow = false,
            uint SendWindowSize = KcpSocket.WND_SND,
            uint ReceiveWindowSize = KcpSocket.WND_RCV,
            int Timeout = KcpPeer.DEFAULT_TIMEOUT,
            uint MaxRetransmits = KcpSocket.DEADLINK)
        {
            this.DualMode = DualMode;
            this.RecvBufferSize = RecvBufferSize;
            this.SendBufferSize = SendBufferSize;
            this.Mtu = Mtu;
            this.NoDelay = NoDelay;
            this.Interval = Interval;
            this.FastResend = FastResend;
            this.CongestionWindow = CongestionWindow;
            this.SendWindowSize = SendWindowSize;
            this.ReceiveWindowSize = ReceiveWindowSize;
            this.Timeout = Timeout;
            this.MaxRetransmits = MaxRetransmits;
        }
    }
}
