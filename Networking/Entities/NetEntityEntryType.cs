namespace Networking.Entities
{
    public enum NetEntityEntryType : byte
    {
        ServerCode = 0,
        ClientCode = 2,

        NetworkProperty = 4,
        NetworkEvent = 8,
    }
}