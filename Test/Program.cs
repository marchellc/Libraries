using Common.Logging;

using System.Threading.Tasks;

using Common.Utilities.Generation;
using Common.Caching;
using Common.IO;

namespace Test
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var log = new LogOutput("Test").Setup();
            var cache = new FileCache<byte>();
            var unique = new UniqueByteGenerator(cache);
            var watcher = new FileWatcher($"{System.IO.Directory.GetCurrentDirectory()}/cache");

            if (!System.IO.File.Exists($"{System.IO.Directory.GetCurrentDirectory()}/cache"))
                System.IO.File.Create($"{System.IO.Directory.GetCurrentDirectory()}/cache").Close();

            cache.SaveOnChange = true;
            cache.DefaultPath = $"{System.IO.Directory.GetCurrentDirectory()}/cache";
            cache.Load($"{System.IO.Directory.GetCurrentDirectory()}/cache");

            watcher.OnChanged += () =>
            {
                log.Info("Watched cache has changed");
            };

            for (int i = 0; i < 10; i++)
            {
                var randomId = unique.Next();
                log.Info($"New random: {randomId}");
            }

            await Task.Delay(-1);
        }
    }
}