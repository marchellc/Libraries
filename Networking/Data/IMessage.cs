namespace Networking.Data
{
    public interface IMessage
    {
        void Serialize(Writer writer);
        void Deserialize(Reader reader);
    }
}