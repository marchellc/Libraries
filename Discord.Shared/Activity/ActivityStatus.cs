using Networking.Data;

namespace Discord.Shared.Activity
{
    public enum ActivityStatus
    {
        Online,
        Idle,
        DoNotDisturb,
        Offline,
        Invisible
    }

    public static class ActivityStatusWriter
    {
        public static void WriteStatus(Writer writer, ActivityStatus status)
            => writer.WriteByte((byte)status);

        public static ActivityStatus ReadStatus(Reader reader)
            => (ActivityStatus)reader.ReadByte();
    }
}