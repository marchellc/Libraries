using Discord.Shared.Entities;

namespace Discord.Server.Utilities
{
    public static class GuildUtils
    {
        public static DiscordGuild ToApiGuild(DSharpPlus.Entities.DiscordGuild guild)
            => new DiscordGuild(guild.Name, guild.Id);
    }
}