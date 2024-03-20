using Common.Extensions;
using Common.Pooling.Pools;

using Discord.WebSocket;
using Discord;

using System.Reflection;

namespace DiscordUtilities.Commands
{
    public class SlashCommandInfo
    {
        public bool IsGlobal { get; }

        public string Name { get; }
        public string Description { get; }

        public object Target { get; }
        public object[] Buffer { get; }

        public MethodInfo Method { get; }

        public ParameterInfo[] Parameters { get; }
        public SlashCommandParameter[] ConvertedParameters { get; }

        public SlashCommandInfo(string name, string description, object target, MethodInfo method, SlashCommandParameter[] convertedParameters, bool isGlobal)
        {
            Name = name;
            Description = description;

            Target = target;
            Method = method;

            ConvertedParameters = convertedParameters;

            Parameters = method.Parameters();
            Buffer = new object[Parameters.Length];

            IsGlobal = isGlobal;        
        }

        public static bool TryGet(MethodInfo method, string name, string description, object target, bool isGlobal, out SlashCommandInfo slashCommand)
        {
            var parameters = method.Parameters();
            var converted = ListPool<SlashCommandParameter>.Shared.Rent();

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == typeof(SocketSlashCommand))
                    continue;

                if (!SlashCommandParameter.TryGetParameter(parameters[i], out var convertedParameter))
                {
                    slashCommand = null;
                    return false;
                }

                converted.Add(convertedParameter);
            }

            slashCommand = new SlashCommandInfo(name, description, target, method, ListPool<SlashCommandParameter>.Shared.ToArrayReturn(converted), isGlobal);
            return true;
        }

        public ApplicationCommandProperties GetApplicationCommandProperties()
        {
            var builder = new SlashCommandBuilder();

            builder.WithName(Name);
            builder.WithDescription(Description);

            for (int i = 0; i < ConvertedParameters.Length; i++)
            {
                var paramBuilder = new SlashCommandOptionBuilder();

                paramBuilder.WithName(ConvertedParameters[i].Name);
                paramBuilder.WithDescription(ConvertedParameters[i].Description);
                paramBuilder.WithType(ConvertedParameters[i].OptionType);
                paramBuilder.WithRequired(!ConvertedParameters[i].IsOptional);

                builder.AddOption(paramBuilder);
            }

            return builder.Build();
        }
    }
}