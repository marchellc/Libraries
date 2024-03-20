using System.IO;

namespace Networking.Kcp
{
    public class KcpSegment
    {
        internal uint conv;    
        internal uint cmd;      

        internal uint frg;
        internal uint wnd;  
        internal uint ts;  
        internal uint sn;    
        internal uint una;
        internal uint resendts; 
        internal int  rto;
        internal uint fastack;
        internal uint xmit;    

        internal MemoryStream data = new MemoryStream(KcpSocket.MTU_DEF);

        internal int Encode(byte[] ptr, int offset)
        {
            int previousPosition = offset;

            offset += KcpUtils.Encode32U(ptr, offset, conv);
            offset += KcpUtils.Encode8u(ptr, offset, (byte)cmd);
            offset += KcpUtils.Encode8u(ptr, offset, (byte)frg);
            offset += KcpUtils.Encode16U(ptr, offset, (ushort)wnd);
            offset += KcpUtils.Encode32U(ptr, offset, ts);
            offset += KcpUtils.Encode32U(ptr, offset, sn);
            offset += KcpUtils.Encode32U(ptr, offset, una);
            offset += KcpUtils.Encode32U(ptr, offset, (uint)data.Position);

            int written = offset - previousPosition;
            return written;
        }

        internal void Reset()
        {
            conv = 0;
            cmd = 0;
            frg = 0;
            wnd = 0;
            ts = 0;
            sn = 0;
            una = 0;
            rto = 0;
            xmit = 0;
            resendts = 0;
            fastack = 0;

            data.SetLength(0);
        }
    }
}
