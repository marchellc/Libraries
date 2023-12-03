namespace Network
{
    public interface IFeature
    {
        ITransport Transport { get; }
        IPeer Peer { get; }
        IController Controller { get; }

        void Stop();
        void Start(IPeer peer);
    }
}