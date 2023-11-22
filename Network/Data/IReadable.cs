using System.IO;

namespace Network.Data
{
    public interface IReadable
    {
        void Read(BinaryReader reader, NetworkPeer peer);
    }
}
