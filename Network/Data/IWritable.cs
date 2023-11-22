using System.IO;

namespace Network.Data
{
    public interface IWritable 
    {
        void Write(BinaryWriter writer, NetworkPeer peer);
    }
}