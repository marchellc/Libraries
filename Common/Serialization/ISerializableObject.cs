namespace Common.Serialization
{
    public interface ISerializableObject
    {
        void Serialize(Serializer serializer);
    }
}