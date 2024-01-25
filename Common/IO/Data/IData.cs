namespace Common.IO.Data
{
    public interface IData
    {
        void Deserialize(DataReader reader);
        void Serialize(DataWriter writer);
    }
}