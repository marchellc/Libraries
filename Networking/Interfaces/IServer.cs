using System.Collections.Generic;

namespace Networking.Interfaces
{
    public interface IServer : IClient
    {
        IReadOnlyCollection<IPeer> Peers { get; }
    }
}
