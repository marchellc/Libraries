using System.IO;

namespace Network.Interfaces.Transporting
{
    public interface IMessage
    {
        void Read(BinaryReader reader, ITransport transport);
        void Write(BinaryWriter writer, ITransport transport);
    }
}