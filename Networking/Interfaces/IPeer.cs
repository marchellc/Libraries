namespace Networking.Interfaces
{
    public interface IPeer : IClient
    {
        IServer Server { get; }

        int Id { get; }

        void Process(byte[] data);
    }
}