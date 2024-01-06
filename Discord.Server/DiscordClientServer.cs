using Discord.Shared.Errors;
using Discord.Shared.Logging;

using Discord.Server.Utilities;
using Discord.Server.Guilds;

using Networking.Objects;
using Networking.Requests;

using DSharpPlus;

using Common.Extensions;

namespace Discord
{
    public class DiscordClient : NetworkObject
    {
        private static ushort NetworkOnErrorHash;
        private static ushort NetworkOnLogHash;

        private static ushort RpcStopHash;
        private static ushort RpcReadyHash;

        private RequestManager requests;
        private GuildManager guilds;

        private DSharpPlus.DiscordClient client;

        public NetworkField<string> NetworkToken { get; private set; }
        public NetworkField<DiscordLog.Severity> NetworkLogSeverity { get; private set; }

        public GuildManager Guilds => guilds;

        public event Action<DiscordException> NetworkOnError;
        public event Action<DiscordLog> NetworkOnLog;

        public DiscordClient(NetworkManager manager) : base(manager)
        {

        }

        public override void OnStart()
        {
            requests = manager.Get<RequestManager>();
            guilds = manager.Get<GuildManager>();

            CallRpcReady();
        }

        public override void OnStop()
        {
            requests = null;

            CmdStop();
        }

        public void CallRpcStop()
            => SendRpc(RpcStopHash);

        public void CallRpcReady()
            => SendRpc(RpcReadyHash);

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

            client.ApplicationCommandPermissionsUpdated += OnApplicationCommandPermissionsUpdated;

            client.AutoModerationRuleCreated += OnAutoModerationRuleCreated;
            client.AutoModerationRuleDeleted += OnAutoModerationRuleDeleted;
            client.AutoModerationRuleExecuted += OnAutoModerationRuleExecuted;
            client.AutoModerationRuleUpdated += OnAutoModerationRuleUpdated;

            client.ChannelCreated += OnChannelCreated;
            client.ChannelDeleted += OnChannelDeleted;
            client.ChannelPinsUpdated += OnChannelPinsUpdated;
            client.ChannelUpdated += OnChannelUpdated;

            client.ClientErrored += OnClientErrored;
            client.ComponentInteractionCreated += OnComponentInteractionCreated;
            client.ContextMenuInteractionCreated += OnContextMenuInteractionCreated;

            client.DmChannelDeleted += OnDmChannelDeleted;

            client.IntegrationCreated += OnIntegrationCreated;
            client.IntegrationDeleted += OnIntegrationDeleted;
            client.IntegrationUpdated += OnIntegrationUpdated;
            client.InteractionCreated += OnInteractionCreated;

            client.InviteCreated += OnInviteCreated;
            client.InviteDeleted += OnInviteDeleted;

            client.MessageCreated += OnMessageCreated;
            client.MessageDeleted += OnMessageDeleted;

            client.MessageReactionAdded += OnMessageReactionAdded;
            client.MessageReactionRemoved += OnMessageReactionRemoved;
            client.MessageReactionRemovedEmoji += OnMessageReactionRemovedEmoji;
            client.MessageReactionsCleared += OnMessageReactionsCleared;
            client.MessagesBulkDeleted += OnMessagesBulkDeleted;
            client.MessageUpdated += OnMessageUpdated;

            client.ModalSubmitted += OnModalSubmitted;

            client.PresenceUpdated += OnPresenceUpdated;

            client.ScheduledGuildEventCompleted += OnScheduledGuildEventCompleted;
            client.ScheduledGuildEventCreated += OnScheduledGuildEventCreated;
            client.ScheduledGuildEventDeleted += OnScheduledGuildEventDeleted;
            client.ScheduledGuildEventUpdated += OnScheduledGuildEventUpdated;
            client.ScheduledGuildEventUserAdded += OnScheduledGuildEventUserAdded;
            client.ScheduledGuildEventUserRemoved += OnScheduledGuildEventUserRemoved;

            client.SessionCreated += OnSessionCreated;
            client.SessionResumed += OnSessionResumed;

            client.SocketClosed += OnSocketClosed;
            client.SocketErrored += OnSocketErrored;
            client.SocketOpened += OnSocketOpened;

            client.StageInstanceCreated += OnStageInstanceCreated;
            client.StageInstanceDeleted += OnStageInstanceDeleted;
            client.StageInstanceUpdated += OnStageInstanceUpdated;

            client.ThreadCreated += OnThreadCreated;
            client.ThreadDeleted += OnThreadDeleted;
            client.ThreadListSynced += OnThreadListSynced;
            client.ThreadMembersUpdated += OnThreadMembersUpdated;
            client.ThreadMemberUpdated += OnThreadMemberUpdated;
            client.ThreadUpdated += OnThreadUpdated;

            client.TypingStarted += OnTypingStarted;

            client.UnknownEvent += OnUnknownEvent;

            client.UserSettingsUpdated += OnUserSettingsUpdated;

            client.UserUpdated += OnUserUpdated;

            client.VoiceServerUpdated += OnVoiceServerUpdated;
            client.VoiceStateUpdated += OnVoiceStateUpdated;

            client.WebhooksUpdated += OnWebhooksUpdated;

            client.Zombied += OnZombied;

            guilds.Init(client);

            Task.Run(async () =>
            {
                try
                {
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
                client.ApplicationCommandPermissionsUpdated -= OnApplicationCommandPermissionsUpdated;

                client.AutoModerationRuleCreated -= OnAutoModerationRuleCreated;
                client.AutoModerationRuleDeleted -= OnAutoModerationRuleDeleted;
                client.AutoModerationRuleExecuted -= OnAutoModerationRuleExecuted;
                client.AutoModerationRuleUpdated -= OnAutoModerationRuleUpdated;

                client.ChannelCreated -= OnChannelCreated;
                client.ChannelDeleted -= OnChannelDeleted;
                client.ChannelPinsUpdated -= OnChannelPinsUpdated;
                client.ChannelUpdated -= OnChannelUpdated;

                client.ClientErrored -= OnClientErrored;
                client.ComponentInteractionCreated -= OnComponentInteractionCreated;
                client.ContextMenuInteractionCreated -= OnContextMenuInteractionCreated;

                client.DmChannelDeleted -= OnDmChannelDeleted;

                client.IntegrationCreated -= OnIntegrationCreated;
                client.IntegrationDeleted -= OnIntegrationDeleted;
                client.IntegrationUpdated -= OnIntegrationUpdated;
                client.InteractionCreated -= OnInteractionCreated;

                client.InviteCreated -= OnInviteCreated;
                client.InviteDeleted -= OnInviteDeleted;

                client.MessageCreated -= OnMessageCreated;
                client.MessageDeleted -= OnMessageDeleted;

                client.MessageReactionAdded -= OnMessageReactionAdded;
                client.MessageReactionRemoved -= OnMessageReactionRemoved;
                client.MessageReactionRemovedEmoji -= OnMessageReactionRemovedEmoji;
                client.MessageReactionsCleared -= OnMessageReactionsCleared;
                client.MessagesBulkDeleted -= OnMessagesBulkDeleted;
                client.MessageUpdated -= OnMessageUpdated;

                client.ModalSubmitted -= OnModalSubmitted;

                client.PresenceUpdated -= OnPresenceUpdated;

                client.ScheduledGuildEventCompleted -= OnScheduledGuildEventCompleted;
                client.ScheduledGuildEventCreated -= OnScheduledGuildEventCreated;
                client.ScheduledGuildEventDeleted -= OnScheduledGuildEventDeleted;
                client.ScheduledGuildEventUpdated -= OnScheduledGuildEventUpdated;
                client.ScheduledGuildEventUserAdded -= OnScheduledGuildEventUserAdded;
                client.ScheduledGuildEventUserRemoved -= OnScheduledGuildEventUserRemoved;

                client.SessionCreated -= OnSessionCreated;
                client.SessionResumed -= OnSessionResumed;

                client.SocketClosed -= OnSocketClosed;
                client.SocketErrored -= OnSocketErrored;
                client.SocketOpened -= OnSocketOpened;

                client.StageInstanceCreated -= OnStageInstanceCreated;
                client.StageInstanceDeleted -= OnStageInstanceDeleted;
                client.StageInstanceUpdated -= OnStageInstanceUpdated;

                client.ThreadCreated -= OnThreadCreated;
                client.ThreadDeleted -= OnThreadDeleted;
                client.ThreadListSynced -= OnThreadListSynced;
                client.ThreadMembersUpdated -= OnThreadMembersUpdated;
                client.ThreadMemberUpdated -= OnThreadMemberUpdated;
                client.ThreadUpdated -= OnThreadUpdated;

                client.TypingStarted -= OnTypingStarted;

                client.UnknownEvent -= OnUnknownEvent;

                client.UserSettingsUpdated -= OnUserSettingsUpdated;

                client.UserUpdated -= OnUserUpdated;

                client.VoiceServerUpdated -= OnVoiceServerUpdated;
                client.VoiceStateUpdated -= OnVoiceStateUpdated;

                client.WebhooksUpdated -= OnWebhooksUpdated;

                client.Zombied -= OnZombied;

                client.Dispose();
                client = null;

                CallRpcStop();
            });
        }

        private Task OnZombied(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ZombiedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnWebhooksUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.WebhooksUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnVoiceStateUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnVoiceServerUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.VoiceServerUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnUserUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.UserUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnUserSettingsUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.UserSettingsUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnUnknownEvent(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.UnknownEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnTypingStarted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.TypingStartEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnThreadUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ThreadUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnThreadMemberUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ThreadMemberUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnThreadMembersUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ThreadMembersUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnThreadListSynced(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ThreadListSyncEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnThreadDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ThreadDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnThreadCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ThreadCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnStageInstanceUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.StageInstanceUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnStageInstanceDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.StageInstanceDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnStageInstanceCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.StageInstanceCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnSocketOpened(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.SocketEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnSocketErrored(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.SocketErrorEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnSocketClosed(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.SocketCloseEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnSessionResumed(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.SessionReadyEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnSessionCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.SessionReadyEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnScheduledGuildEventUserRemoved(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ScheduledGuildEventUserRemoveEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnScheduledGuildEventUserAdded(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ScheduledGuildEventUserAddEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnScheduledGuildEventUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ScheduledGuildEventUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnScheduledGuildEventDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ScheduledGuildEventDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnScheduledGuildEventCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ScheduledGuildEventCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnScheduledGuildEventCompleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ScheduledGuildEventCompletedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnPresenceUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.PresenceUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnModalSubmitted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ModalSubmitEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnMessageUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnMessagesBulkDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageBulkDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnMessageReactionsCleared(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageReactionsClearEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnMessageReactionRemovedEmoji(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageReactionRemoveEmojiEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnMessageReactionRemoved(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageReactionRemoveEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnMessageReactionAdded(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageReactionAddEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnMessageDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnMessageCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnInviteDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.InviteDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnInviteCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.InviteCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnInteractionCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.InteractionCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnIntegrationUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.IntegrationUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnIntegrationDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.IntegrationDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnIntegrationCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.IntegrationCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnDmChannelDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.DmChannelDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnContextMenuInteractionCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ContextMenuInteractionCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnComponentInteractionCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnClientErrored(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ClientErrorEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnChannelUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ChannelUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnChannelPinsUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ChannelPinsUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnChannelDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ChannelDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnChannelCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ChannelCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnAutoModerationRuleUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.AutoModerationRuleUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnAutoModerationRuleExecuted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.AutoModerationRuleExecuteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnAutoModerationRuleDeleted(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.AutoModerationRuleDeleteEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnAutoModerationRuleCreated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.AutoModerationRuleCreateEventArgs args)
        {
            throw new NotImplementedException();
        }

        private Task OnApplicationCommandPermissionsUpdated(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.ApplicationCommandPermissionsUpdatedEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}