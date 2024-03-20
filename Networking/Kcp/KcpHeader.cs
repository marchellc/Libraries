using System;

namespace Networking.Kcp
{
    public static class KcpHeader
    {
        public static bool ParseReliable(byte value, out KcpReliableHeader header)
        {
            if (Enum.IsDefined(typeof(KcpReliableHeader), value))
            {
                header = (KcpReliableHeader)value;
                return true;
            }

            header = KcpReliableHeader.Ping;
            return false;
        }

        public static bool ParseUnreliable(byte value, out KcpUnreliableHeader header)
        {
            if (Enum.IsDefined(typeof(KcpUnreliableHeader), value))
            {
                header = (KcpUnreliableHeader)value;
                return true;
            }

            header = KcpUnreliableHeader.Disconnect;
            return false;
        }
    }
}