namespace Networking.Kcp
{
    public enum KcpReliableHeader : byte
    {
        Hello      = 1,
        Ping       = 2,
        Data       = 3,
    }
}