using Common.Extensions;

using Network.Controllers;
using Network.Peers;

using System;

namespace Network.Features
{
    public static class FeatureExtensions
    {
        public static void ExecuteFeature<TFeature>(this INetworkObject networkObject, Action<TFeature> action) where TFeature : INetworkFeature
        {
            if (networkObject is null)
                throw new ArgumentNullException(nameof(networkObject));

            if (action is null)
                throw new ArgumentNullException(nameof(action));

            var feature = networkObject.GetFeature<TFeature>();

            if (feature != null)
                action.Call(feature);
        }

        public static TFeature GetFeature<TFeature>(this INetworkObject networkObject) where TFeature : INetworkFeature
        {
            if (networkObject is null)
                throw new ArgumentNullException(nameof(networkObject));

            var manager = networkObject.GetFeatures();

            if (manager is null)
                return default;

            return manager.GetFeature<TFeature>();
        }

        public static INetworkFeatureManager GetFeatures(this INetworkObject networkObject)
        {
            if (networkObject is null)
                throw new ArgumentNullException(nameof(networkObject));

            var manager = default(INetworkFeatureManager);

            if (networkObject is INetworkPeer networkPeer)
                manager = networkPeer.Features;
            else if (networkObject is INetworkController networkController)
                manager = networkController.Features;
            else
                manager = null;

            return manager;
        }

        public static void ExecuteIfServerPeer(this INetworkFeature feature, Action<INetworkPeer> execute)
        {
            if (feature is null)
                throw new ArgumentNullException(nameof(feature));

            if (execute is null)
                throw new ArgumentNullException(nameof(execute));

            if (feature.Controller is null || feature.Controller is not INetworkPeer networkPeer)
                return;

            execute.Call(networkPeer);
        }

        public static void ExecuteIfServer(this INetworkFeature feature, Action<INetworkController> execute)
        {
            if (feature is null)
                throw new ArgumentNullException(nameof(feature));

            if (execute is null)
                throw new ArgumentNullException(nameof(execute));

            if (feature.Controller is null || feature.Controller is not INetworkController networkController || networkController.Type != ControllerType.Server)
                return;

            execute.Call(networkController);
        }

        public static void ExecuteIfClient(this INetworkFeature feature, Action<INetworkController> execute)
        {
            if (feature is null)
                throw new ArgumentNullException(nameof(feature));

            if (execute is null)
                throw new ArgumentNullException(nameof(execute));

            if (feature.Controller is null || feature.Controller is not INetworkController networkController || networkController.Type != ControllerType.Client)
                return;

            execute.Call(networkController);
        }
    }
}
