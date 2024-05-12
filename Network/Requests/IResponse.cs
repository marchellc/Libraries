using Common.IO.Data;

using System;

namespace Network.Requests
{
    public interface IResponse : IData
    {
        bool IsSuccess { get; }

        string RequestId { get; }

        object Response { get; }

        DateTime ReceivedAt { get; }
        DateTime SentAt { get; }
    }
}