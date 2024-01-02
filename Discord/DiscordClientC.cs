using Common.Extensions;

using Discord.Shared.Activity;
using Discord.Shared.Errors;
using Discord.Shared.Logging;

using Networking.Objects;
using Networking.Requests;

using System;

namespace Discord
{
    public class DiscordClient : NetworkObject
    {
        private static readonly ushort CmdStartHash = "DiscordClient+CmdStart".GetStableHash();
        private static readonly ushort CmdStopHash = "DiscordClient+CmdStop".GetStableHash();

        private RequestManager requests;
        private DiscordActivity activity;

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
        }

        public void CallCmdStart()
            => SendCmd(CmdStartHash);

        public void CallCmdStop()
            => SendCmd(CmdStopHash);

        private void RpcStop()
        {
            manager.Destroy<RequestManager>();
            manager.Destroy<DiscordActivity>();

            requests = null;
            activity = null;
        }
    }
}