using Networking.Data;

namespace Discord.Shared.Logging
{
    public struct DiscordLog : IMessage
    {
        public Severity severity;
        public string message;

        public DiscordLog(Severity severity, string message)
        {
            this.severity = severity;
            this.message = message;
        }

        public void Deserialize(Reader reader)
        {
            severity = (Severity)reader.ReadByte();
            message = reader.ReadCleanString();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteByte((byte)severity);
            writer.WriteString(message);
        }

        public enum Severity
        {
            Error,
            Warning,
            Information,
            Debug,
            Trace,
            Verbose
        }

        public static void WriteSeverity(Writer writer, Severity severity)
            => writer.WriteByte((byte)severity);

        public static Severity ReadSeverity(Reader reader)
            => (Severity)reader.ReadByte();
    }
}
