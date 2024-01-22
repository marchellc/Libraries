using Common.Extensions;
using Common.Logging;
using Common.Patching;
using Common.Utilities;

using Networking.Http;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Test
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            LogOutput.Raw("before patching");

            method();

            LogOutput.Raw("patching");

            PatchManager.Patch<Func<bool>>(typeof(Program).Method("method"), () => patchmethod(), PatchType.Prefix);

            LogOutput.Raw("after patching");

            method();

            /*
            try
            {
                var log = new LogOutput("Test").Setup();

                var step = 0;

                step++;
                log.Info(step);

                var server = new HttpServer();

                step++;
                log.Info(step);

                server.Start("http://127.0.0.1/");

                step++;
                log.Info(step);

                CodeUtils.Delay(() =>
                {
                    var id = server.CreateRoute(ctx =>
                    {
                        ctx.RespondOk("test");
                        return Task.CompletedTask;
                    }, HttpMethod.Get, "/test", "A test route");

                    log.Info($"Created route ID: {id}");
                }, 100);

                step++;
                log.Info(step);
            }
            catch (Exception ex)
            {
                LogOutput.Common.Error(ex);
            }
            */

            await Task.Delay(-1);
        }

        public static bool patchmethod()
        {
            LogOutput.Raw("patch method");
            return false;
        }

        public static void method() => LogOutput.Raw("method");
    }
}