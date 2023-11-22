using System;

namespace Network.Requests
{
    public struct ResponseHandler
    {
        public RequestMessage Request;
        public Delegate Handler;
        public byte Id;
        public DateTime Added;
        public byte Timeout;

        public ResponseHandler(RequestMessage request, Delegate handler, byte id, byte timeout)
        {
            Request = request;
            Handler = handler;
            Id = id;
            Timeout = timeout;
            Added = DateTime.Now;
        }
    }
}