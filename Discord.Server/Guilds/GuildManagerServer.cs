using Networking.Objects;

using Discord.Shared.Entities;
using Discord.Server.Utilities;

namespace Discord.Server.Guilds
{
    public class GuildManager : NetworkObject
    {
        private DSharpPlus.DiscordClient client;

        private static ushort RpcUpdateGuildHash;
        private static ushort RpcUpdateGuildMembers;

        public GuildManager(NetworkManager manager) : base(manager) { }

        internal void Init(DSharpPlus.DiscordClient client)
        {
            this.client = client;

            client.GuildAuditLogCreated += OnGuildAuditLogCreated;
            client.GuildAvailable += OnGuildAvailable;
            client.GuildBanAdded += OnGuildBanAdded;
            client.GuildBanRemoved += OnGuildBanRemoved;
            client.GuildCreated += OnGuildCreated;
            client.GuildDeleted += OnGuildDeleted;
            client.GuildDownloadCompleted += OnGuildDownloadCompleted;
            client.GuildEmojisUpdated += OnGuildEmojisUpdated;
            client.GuildIntegrationsUpdated += OnGuildIntegrationsUpdated;
            client.GuildMemberAdded += OnGuildMemberAdded;
            client.GuildMemberRemoved += OnGuildMemberRemoved;
            client.GuildMembersChunked += OnGuildMembersChunked;
            client.GuildMemberUpdated += OnGuildMemberUpdated;
            client.GuildRoleCreated += OnGuildRoleCreated;
            client.GuildRoleDeleted += OnGuildRoleDeleted;
            client.GuildRoleUpdated += OnGuildRoleUpdated;
            client.GuildStickersUpdated += OnGuildStickersUpdated;
            client.GuildUnavailable += OnGuildUnavailable;
            client.GuildUpdated += OnGuildUpdated;
        }

        public override void OnStop()
        {
            client.GuildAuditLogCreated -= OnGuildAuditLogCreated;
            client.GuildAvailable -= OnGuildAvailable;
            client.GuildBanAdded -= OnGuildBanAdded;
            client.GuildBanRemoved -= OnGuildBanRemoved;
            client.GuildCreated -= OnGuildCreated;
            client.GuildDeleted -= OnGuildDeleted;
            client.GuildDownloadCompleted -= OnGuildDownloadCompleted;
            client.GuildEmojisUpdated -= OnGuildEmojisUpdated;
            client.GuildIntegrationsUpdated -= OnGuildIntegrationsUpdated;
            client.GuildMemberAdded -= OnGuildMemberAdded;
            client.GuildMemberRemoved -= OnGuildMemberRemoved;
            client.GuildMembersChunked -= OnGuildMembersChunked;
            client.GuildMemberUpdated -= OnGuildMemberUpdated;
            client.GuildRoleCreated -= OnGuildRoleCreated;
            client.GuildRoleDeleted -= OnGuildRoleDeleted;
            client.GuildRoleUpdated -= OnGuildRoleUpdated;
            client.GuildStickersUpdated -= OnGuildStickersUpdated;
            client.GuildUnavailable -= OnGuildUnavailable;
            client.GuildUpdated -= OnGuildUpdated;

            client = null;
        }

        public void CmdDownloadMembers(ulong guildId)
        {
            Task.Run(async () =>
            {
                var guild = await client.GetGuildAsync(guildId, true);

                if (guild is null)
                    return;

                var members = guild.GetAllMembersAsync().ToBlockingEnumerable();

                if (members is null)
                    return;

                CallRpcUpdateGuildMembers(guildId, members.Select(mem => UserUtils.ToApiUser(mem)));
            });
        }

        private void CallRpcUpdateGuildMembers(ulong guildId, IEnumerable<DiscordUser> users)
            => SendRpc(RpcUpdateGuildMembers, guildId, users);

        private void CallRpcUpdateGuild(DiscordGuild guild)
            => SendRpc(RpcUpdateGuildHash, guild);

        private Task OnGuildUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildUnavailable(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildStickersUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildStickersUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildRoleUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildRoleUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildRoleDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildRoleDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildRoleCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildRoleCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildMemberUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildMemberUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildMembersChunked(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildMembersChunkEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildMemberRemoved(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildMemberAdded(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildMemberAddEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildIntegrationsUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildIntegrationsUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildEmojisUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildEmojisUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildDownloadCompleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildDownloadCompletedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildBanRemoved(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildBanRemoveEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildBanAdded(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildBanAddEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnGuildAvailable(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildCreateEventArgs args)
        {
            CallRpcUpdateGuild(GuildUtils.ToApiGuild(args.Guild));
            return Task.CompletedTask;
        }

        private Task OnGuildAuditLogCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.GuildAuditLogCreatedEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}