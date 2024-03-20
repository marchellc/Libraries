using Discord.WebSocket;
using Discord;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace DiscordUtilities.Commands
{
    public class SlashCommandParameter
    {
        public static IReadOnlyDictionary<Type, ApplicationCommandOptionType> Conversions { get; } = new Dictionary<Type, ApplicationCommandOptionType>()
        {
            [typeof(string)] = ApplicationCommandOptionType.String,
            [typeof(int)] = ApplicationCommandOptionType.Integer,
            [typeof(bool)] = ApplicationCommandOptionType.Boolean,

            [typeof(Attachment)] = ApplicationCommandOptionType.Attachment,
            [typeof(SocketUser)] = ApplicationCommandOptionType.User,
            [typeof(SocketGuildUser)] = ApplicationCommandOptionType.User,
            [typeof(SocketRole)] = ApplicationCommandOptionType.Role,
            [typeof(SocketChannel)] = ApplicationCommandOptionType.Channel,

            [typeof(IMentionable)] = ApplicationCommandOptionType.Mentionable,
        };

        public Type Type { get; }

        public ApplicationCommandOptionType OptionType { get; }

        public string Name { get; }
        public string Description { get; }

        public bool IsOptional { get; }

        public SlashCommandParameter(Type type, ApplicationCommandOptionType applicationCommandOptionType, string name, string description, bool isOptional)
        {
            Type = type;
            Name = name;
            Description = description;
            OptionType = applicationCommandOptionType;
            IsOptional = isOptional;
        }

        public static bool TryGetParameter(ParameterInfo info, out SlashCommandParameter commandParameter)
        {
            if (!Conversions.TryGetValue(info.ParameterType, out var optionType))
            {
                commandParameter = null;
                return false;
            }

            var attribute = info.GetCustomAttribute<CommandParameterAttribute>();

            if (attribute != null)
            {
                commandParameter = new SlashCommandParameter(info.ParameterType, optionType, attribute.Name, attribute.Description, attribute.IsOptional);
                return true;
            }

            commandParameter = new SlashCommandParameter(info.ParameterType, optionType, info.Name, "No description.", info.HasDefaultValue);
            return true;
        }
    }
}