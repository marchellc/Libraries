using Networking.Data;

namespace Discord.Shared.Entities
{
    public class DiscordEntity : IMessage
    {
        public DiscordEntity() { }
        public DiscordEntity(ulong id)
            => Id = id;

        public ulong Id { get; private set; }

        public virtual void Deserialize(Reader reader)
        {
            Id = reader.ReadULong();
        }

        public virtual void Serialize(Writer writer)
        {
            writer.WriteULong(Id);
        }
    }
}