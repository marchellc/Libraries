using System;

namespace Network.Data
{
    public class Message
    {
        public MessageId Id;
        public MessageChannel Channel;
        public Type Type;
        public object Value;
    }
}