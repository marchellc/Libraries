using System;

namespace DiscordUtilities.Commands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; set; } = "default";
        public string Description { get; set; } = "No description.";

        public bool IsGlobal { get; set; } = true;

        public CommandAttribute(string name, string description = "No description.", bool isGlobal = true)
        {
            Name = name;
            Description = description;
            IsGlobal = isGlobal;
        }
    }
}