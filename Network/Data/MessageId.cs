namespace Network.Data
{
    public struct MessageId
    {
        public const short TYPE_SYNC_ID = 0x245;
        public const short TYPE_BEAT_ID = 0x250;

        public byte Header;
        public byte Channel;

        public short Id;

        public bool IsInternal;

        public MessageId(byte header, byte channel, short id, bool isInternal = false)
        {
            Header = header;
            Channel = channel;
            Id = id;
            IsInternal = isInternal;
        }

        public static readonly MessageId BEAT_MESSAGE = new MessageId
        {
            Header = 0,
            Channel = 0,

            Id = TYPE_BEAT_ID,

            IsInternal = true,
        };

        public static readonly MessageId SYNC_MESSAGE = new MessageId
        {
            Header = 0,
            Channel = 0,

            Id = TYPE_SYNC_ID,

            IsInternal = true,
        };
    }
}