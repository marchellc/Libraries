using Networking.Data;

namespace Discord.Shared.Entities
{
    public class DiscordUser : DiscordEntity
    {
        public DiscordUser(string name, ulong id) : base(id)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public override void Deserialize(Reader reader)
        {
            base.Deserialize(reader);

            Name = reader.ReadCleanString();
        }

        public override void Serialize(Writer writer)
        {
            base.Serialize(writer);

            writer.WriteString(Name);
        }
    }
}
