using Discord.Shared.Errors;
using Discord.Shared.Activity;
using Discord.Shared.Logging;

using Networking.Objects;
using Networking.Requests;

using DSharpPlus;

using Discord.Server.Utilities;

using Common.Extensions;

namespace Discord
{
    public class DiscordClient : NetworkObject
    {
        private static readonly ushort NetworkOnErrorHash = "DiscordClient+NetworkOnError".GetStableHash();
        private static readonly ushort NetworkOnLogHash = "DiscordClient+NetworkOnLog".GetStableHash();

        private static readonly ushort RpcStopHash = "DiscordClient+RpcStop".GetStableHash();

        private RequestManager requests;
        private DiscordActivity activity;

        private DSharpPlus.DiscordClient client;

        public NetworkField<string> NetworkToken { get; private set; }
        public NetworkField<DiscordLog.Severity> NetworkLogSeverity { get; private set; }

        public DiscordActivity Activity => activity;

        public event Action<DiscordException> NetworkOnError;
        public event Action<DiscordLog> NetworkOnLog;

        public DiscordClient(NetworkManager manager) : base(manager)
        {

        }

        public override void OnStart()
        {
            requests = manager.Instantiate<RequestManager>();
            activity = manager.Instantiate<DiscordActivity>();

            activity.NetworkName.OnChanged += OnNetworkActivityNameChanged;
            activity.NetworkStatus.OnChanged += OnNetworkActivityStatusChanged;
            activity.NetworkType.OnChanged += OnNetworkActivityTypeChanged;
            activity.NetworkStreamUrl.OnChanged += OnNetworkStreamUrlChanged;
        }

        public override void OnStop()
        {
            activity.NetworkName.OnChanged -= OnNetworkActivityNameChanged;
            activity.NetworkStatus.OnChanged -= OnNetworkActivityStatusChanged;
            activity.NetworkType.OnChanged -= OnNetworkActivityTypeChanged;
            activity.NetworkStreamUrl.OnChanged -= OnNetworkStreamUrlChanged;
            activity = null;

            requests = null;

            CmdStop();
        }

        public void CallRpcStop()
            => SendRpc(RpcStopHash);

        public void CmdStart()
        {
            if (string.IsNullOrWhiteSpace(NetworkToken.Value))
            {
                SendEvent(NetworkOnErrorHash, true, new DiscordException("Token is missing or invalid."));
                return;
            }

            client = new DSharpPlus.DiscordClient(new DiscordConfiguration
            {
                AlwaysCacheMembers = true,
                AutoReconnect = true,
                LogUnknownEvents = true,
                LogUnknownAuditlogs = true,

                MessageCacheSize = 4096,

                Token = NetworkToken.Value,
                MinimumLogLevel = EnumConversion.ToLogLevel(NetworkLogSeverity.Value),

                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                GatewayCompressionLevel = GatewayCompressionLevel.Stream,
            });

            Task.Run(async () =>
            {
                try
                {
                    await client.InitializeAsync();
                    await client.ConnectAsync();
                }
                catch (Exception ex)
                {
                    CmdStop();
                    SendEvent(NetworkOnErrorHash, true, new DiscordException(ex.Message));
                }
            });
        }

        public void CmdStop()
        {
            if (client is null)
                return;

            Task.Run(async () =>
            {
                try { await client.DisconnectAsync(); } catch { }

                client.Dispose();

                CallRpcStop();
            });
        }

        private void RefreshActivity()
        {
            if (client is null)
                return;

            Task.Run(async () => await client.UpdateStatusAsync(new DSharpPlus.Entities.DiscordActivity
            {
                ActivityType = Activity.NetworkType.Value.GetDiscordType(),

                Name = Activity.NetworkName.Value,
                StreamUrl = Activity.NetworkStreamUrl.Value

            }, Activity.NetworkStatus.Value.GetStatus()));
        }

        private void OnNetworkStreamUrlChanged(string previous, string now) => RefreshActivity();
        private void OnNetworkActivityNameChanged(string previous, string now) => RefreshActivity();
        private void OnNetworkActivityTypeChanged(ActivityType previous, ActivityType now) => RefreshActivity();
        private void OnNetworkActivityStatusChanged(ActivityStatus previous, ActivityStatus now) => RefreshActivity();
    }
}