using Common.Extensions;

using Discord.WebSocket;

namespace DiscordUtilities.Commands
{
    public static class SlashCommandUtils
    {
        public static bool ConvertArgs(SocketSlashCommand socketSlashCommand, SlashCommandInfo slashCommand)
        {
            slashCommand.Buffer[0] = socketSlashCommand;

            for (int i = 0; i < slashCommand.ConvertedParameters.Length; i++)
            {
                if (!socketSlashCommand.Data.Options.TryGetFirst(opt => opt.Name.ToLower() == slashCommand.ConvertedParameters[i].Name.ToLower(), out var matchedOption))
                {
                    if (!slashCommand.ConvertedParameters[i].IsOptional)
                    {
                        return false;
                    }
                    else
                    {
                        slashCommand.Buffer[i] = null;
                        continue;
                    }
                }
                else
                {
                    slashCommand.Buffer[i] = matchedOption.Value;
                }
            }

            return true;
        }
    }
}