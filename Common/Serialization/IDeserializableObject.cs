namespace Common.Serialization
{
    public interface IDeserializableObject
    {
        void Deserialize(Deserializer deserializer);
    }
}