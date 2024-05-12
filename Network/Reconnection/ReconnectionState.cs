namespace Network.Reconnection
{
    public enum ReconnectionState
    {
        Connected,

        Reconnecting,

        Cooldown,
        ColldownFailure,
    }
}