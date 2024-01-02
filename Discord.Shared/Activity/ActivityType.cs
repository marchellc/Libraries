using Networking.Data;

namespace Discord.Shared.Activity
{
    public enum ActivityType
    {
        Playing,
        ListeningTo,
        Streaming,
        Watching,
        Competing,
        Custom
    }

    public static class ActivityTypeWriter
    {
        public static void WriteActivity(Writer writer, ActivityType type)
            => writer.Write((byte)type);

        public static ActivityType ReadActivity(Reader reader)
            => (ActivityType)reader.ReadByte();
    }
}