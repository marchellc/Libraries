using Common;
using Common.Logging;
using Common.Extensions;
using Common.Pooling.Pools;
using Common.Utilities;

using Grapevine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Networking.Http
{
    public class HttpServer
    {
        private static readonly IConfigurationRoot defaultConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        private volatile bool stopReq;
        private volatile IRestServer restServer;

        public bool IsRunning => !stopReq && restServer != null && restServer.IsListening;

        public IReadOnlyList<Route> Routes
        {
            get
            {
                if (!IsRunning)
                    throw new InvalidOperationException($"Cannot get routes on a not running server");

                return restServer.Router.RoutingTable.Where<Route>();
            }
        }

        public IRestServer RestServer
        {
            get => restServer;
        }

        public HttpAuthentificator Authentificator { get; set; } = new HttpAuthentificator();
        public LogOutput Log { get; set; } = new LogOutput("Http Server").Setup();

        public void Start(params string[] prefixes)
        {
            if (IsRunning)
                Stop();

            while (stopReq)
                continue;

            var collection = new ServiceCollection();
            var logger = new HttpLogger();

            if (Log != null)
                logger.Output = Log;

            collection.AddSingleton(typeof(IConfiguration), defaultConfig);

            collection.AddSingleton<IRestServer, RestServer>();
            collection.AddSingleton<IRouter, Router>();
            collection.AddSingleton<IRouteScanner, RouteScanner>();

            collection.AddTransient<IContentFolder, ContentFolder>();

            collection.AddLogging(b => b.AddProvider(logger));

            collection.Configure<LoggerFilterOptions>(log => log.MinLevel = LogLevel.Warning);

            var provider = collection.BuildServiceProvider();
            var server = provider.GetService<IRestServer>();

            server.Router.Services = collection;
            server.RouteScanner.Services = collection;

            server.GlobalResponseHeaders.Add("Server", $"{ModuleInitializer.GetAppName()}/1.0.0 ({RuntimeInformation.OSDescription})");
            server.Prefixes.AddRange(prefixes);

            collection.AddSingleton<IRestServer>(server);
            collection.AddSingleton<IRouter>(server.Router);
            collection.AddSingleton<IRouteScanner>(server.RouteScanner);

            server.SetDefaultLogger(logger);

            stopReq = false;
            restServer = server;

            CodeUtils.OnThread(async () =>
            {
                restServer.Start();

                while (!stopReq)
                    await Task.Delay(10);

                restServer.Stop();
                restServer.Dispose();
                restServer = null;

                stopReq = false;
            });
        }

        public void Stop()
        {
            if (!IsRunning)
                throw new InvalidOperationException($"Cannot stop the HTTP server; not running");

            if (stopReq)
                throw new InvalidOperationException($"A stop has already been requested");

            stopReq = true;
        }

        public void FindRoutes()
            => FindRoutes(Assembly.GetCallingAssembly());

        public void FindRoutes<TRoutes>()
            => FindRoutes(typeof(TRoutes));

        public void FindRoutes(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                FindRoutes(type);
        }

        public void FindRoutes(Type type)
        {
            if (!IsRunning)
                throw new InvalidOperationException($"Cannot create route on a not running server");

            var routes = restServer.RouteScanner.Scan(type);

            if (routes is null || routes.Count == 0)
                return;

            restServer.Router.Register(routes);
        }

        public string CreateRoute(Func<IHttpContext, Task> route, HttpMethod method, string url, string description = null)
        {
            if (!IsRunning)
                throw new InvalidOperationException($"Cannot create route on a not running server");

            description ??= "No description.";

            var routeId = Generator.Instance.GetString(60);
            var routeObj = new Route(route, method, url, true, routeId, description);

            restServer.Router.Register(routeObj);

            return routeId;
        }

        public string CreateRoute(Func<IHttpContext, Task> route, HttpMethod method, string url, string perm, string description = null)
        {
            if (!IsRunning)
                throw new InvalidOperationException($"Cannot create route on a not running server");

            if (string.IsNullOrWhiteSpace(perm))
                return CreateRoute(route, method, url, description);

            description ??= "No description.";

            Func<IHttpContext, Task> routeMethod = ctx =>
            {
                if (!ctx.TryAccess(Authentificator, perm))
                    return Task.CompletedTask;

                return route.Call(ctx);
            };

            var routeId = Generator.Instance.GetString(60);
            var routeObj = new Route(route, method, url, true, routeId, description);

            restServer.Router.Register(routeObj);

            return routeId;
        }

        public bool DeleteRoute(string id)
        {
            if (!IsRunning)
                throw new InvalidOperationException($"Cannot delete route on a not running server");

            if (!restServer.Router.RoutingTable.TryGetFirst(route => route.Name == id, out var route))
                return false;

            route.Disable();

            return restServer.Router.RoutingTable.Remove(route);
        }

        public void DeleteRoutes(Type type)
        {
            if (!IsRunning)
                throw new InvalidOperationException($"Cannot delete route on a not running server");

            var routeField = typeof(Route).Field("RouteAction");
            var routes = ListPool<IRoute>.Shared.Rent();

            foreach (var route in restServer.Router.RoutingTable)
            {
                if (route is not Route aRoute)
                    continue;

                var routeAction = routeField.GetValueFast<Func<IHttpContext, Task>>(aRoute);

                if (routeAction is null)
                    continue;

                var routeMethod = routeAction.Method;

                if (routeMethod is null)
                    continue;

                if (routeMethod.DeclaringType == type)
                {
                    routes.Add(aRoute);
                    aRoute.Disable();
                }
            }

            foreach (var route in routes)
                restServer.Router.RoutingTable.Remove(route);
        }

        public void DeleteRoutes<TRoutes>()
            => DeleteRoutes(typeof(TRoutes));
        
        public void DeleteRoutes(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                DeleteRoutes(type);
        }

        public void DeleteRoutes()
            => DeleteRoutes(Assembly.GetCallingAssembly());
    }
}