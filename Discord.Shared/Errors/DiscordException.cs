using Networking.Data;

using System;

namespace Discord.Shared.Errors
{
    public class DiscordException : Exception
    {
        public DiscordException(object message) : base($"Discord Client failed: {message}") { }

        public static void WriteException(Writer writer, DiscordException discordStartException)
        {
            writer.WriteString(discordStartException.Message);
        }

        public static DiscordException ReadException(Reader reader)
        {
            return new DiscordException(reader.ReadString());
        }
    }
}