using Common.Logging;

using System.Threading.Tasks;
using System;

using Common.Pooling.Pools;
using Common.IO.Data;

namespace Test
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var log = new LogOutput("Test").Setup();

            try
            {
                var writer = PoolablePool<DataWriter>.Shared.Rent();

                writer.Write("hello");
                writer.Write(typeof(Program));

                var data = writer.Data;

                log.Info($"Writer: {data.Length} bytes");

                PoolablePool<DataWriter>.Shared.Return(writer);

                var reader = PoolablePool<DataReader>.Shared.Rent();

                reader.Set(data);

                log.Info($"Reader: {reader.Buffer.DataSize} bytes");

                var str = reader.Read<string>();
                var type = reader.Read<Type>();

                log.Info($"String: {str}");
                log.Info($"Type: {type.FullName}");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            await Task.Delay(-1);
        }
    }
}