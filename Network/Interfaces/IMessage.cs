using System.IO;

namespace Network.Interfaces
{
    public interface IMessage
    {
        void Read(BinaryReader reader);
        void Write(BinaryWriter writer);
    }
}