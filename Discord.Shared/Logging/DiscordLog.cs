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
            message = reader.ReadCleanString();
            severity = reader.Read<Severity>();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteString(message);
            writer.Write(severity);
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
    }
}
