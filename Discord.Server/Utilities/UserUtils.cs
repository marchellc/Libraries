using Discord.Shared.Entities;

namespace Discord.Server.Utilities
{
    public static class UserUtils
    {
        public static DiscordUser ToApiUser(this DSharpPlus.Entities.DiscordUser discordUser)
            => new DiscordUser(discordUser.Username, discordUser.Id);
    }
}