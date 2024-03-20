namespace Networking.Interfaces
{
    public interface IManager
    {
        IComponent[] Components { get; }

        T Get<T>() where T : IComponent;

        bool Remove<T>() where T : IComponent;
        void Add<T>() where T : IComponent;
    }
}