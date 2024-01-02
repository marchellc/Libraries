using Networking.Objects;

namespace Discord.Shared.Activity
{
    public class DiscordActivity : NetworkObject
    {
        public DiscordActivity(NetworkManager manager) : base(manager) { }

        public NetworkField<string> NetworkName { get; private set; }
        public NetworkField<string> NetworkStreamUrl { get; private set; }

        public NetworkField<ActivityStatus> NetworkStatus { get; private set; }
        public NetworkField<ActivityType> NetworkType { get; private set; }
    }
}