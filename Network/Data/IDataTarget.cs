namespace Network.Data
{
    public interface IDataTarget
    {
        bool Process(object data);
        bool Accepts(object data);
    }
}