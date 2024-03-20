using Common.IO.Data;

namespace Networking.Interfaces
{
    public interface ITarget
    {
        bool TryProcess(IData data);
    }
}