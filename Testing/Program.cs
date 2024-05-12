using Common.Configs;
using Common.Logging;
using Common.Utilities;

using System.IO;
using System.Threading.Tasks;

namespace Testing
{
    public static class Program
    {
        [Config("Test String", "A", "Test string")]
        public static string TestString = "hello";

        [Config("Test Number", "A test number")]
        public static int TestNum = 15645;

        public static async Task Main(string[] args)
        {
            var file = $"{Directory.GetCurrentDirectory()}/config.json";
            var cfg = new ConfigFile(file);

            cfg.Serializer = value => value.JsonSerialize();
            cfg.Deserializer = (value, type) => JsonUtils.JsonDeserialize(value, type);

            cfg.IsWatched = true;

            cfg.OnLoaded += failures =>
            {
                Logger.Info("Config file reloaded");

                foreach (var fail in failures)
                    Logger.Error($"Config key {fail.Key} failed to load: {fail.Value}");

                Logger.Info($"Test String: {TestString}");
                Logger.Info($"Test Num: {TestNum}");
            };

            cfg.Bind();
            cfg.Load();

            await Task.Delay(-1);
        }
    }
}