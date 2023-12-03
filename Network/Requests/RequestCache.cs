using Network.Interfaces.Requests;

using System;

namespace Network.Requests
{
    public struct RequestCache
    {
        public IRequest Request;
        public DateTime Requested;
        public byte Timeout;
        public Delegate ResponseHandler;
    }
}