using Common.IO.Data;

using System.Collections.Generic;

namespace Networking.Interfaces
{
    public interface ISender
    {
        bool CanSend { get; }

        ulong SentBytes { get; }

        void Send(params IData[] data);
        void Send(IEnumerable<IData> data);

        void SendSingular(IData data);
    }
}