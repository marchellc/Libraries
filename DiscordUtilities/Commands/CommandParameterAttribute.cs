using System;

namespace DiscordUtilities.Commands
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class CommandParameterAttribute : Attribute
    {
        public string Name { get; set; } = "default";
        public string Description { get; set; } = "No description.";

        public bool IsOptional { get; set; } 

        public CommandParameterAttribute(string name, string description = "No description.", bool isOptional = false)
        {
            Name = name;
            Description = description;

            IsOptional = isOptional;

            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentNullException(nameof(Name));
        }
    }
}