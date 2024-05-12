using Common.Extensions;

using Network.Controllers;
using Network.Data;
using Network.Events;
using Network.Peers;

using System.Collections.Generic;

namespace Network.Features
{
    public class NetworkFeatureManager : INetworkFeatureManager
    {
        private readonly INetworkObject m_Controller;
        private readonly INetworkEvents m_Events;
        private readonly HashSet<INetworkFeature> m_Features;

        public INetworkObject Controller => m_Controller;
        public IReadOnlyCollection<INetworkFeature> AllFeatures => m_Features;

        public NetworkFeatureManager(INetworkObject controller, INetworkEvents privateEvents = null)
        {
            m_Controller = controller;
            m_Events = privateEvents;
            m_Features = new HashSet<INetworkFeature>();

            if (controller is INetworkController networkController && networkController.Type is ControllerType.Server
                && privateEvents != null && privateEvents is BasicEvents basicEvents)
            {
                basicEvents.OnStarting += OnStarting;
                basicEvents.OnStopping += OnStopping;
            }
        }

        public TFeature AddFeature<TFeature>() where TFeature : INetworkFeature
        {
            if (m_Features.TryGetFirst(f => f.IsEnabled && f is TFeature, out var networkFeature))
                return (TFeature)networkFeature;

            var feature = typeof(TFeature).Construct<TFeature>();

            if (m_Features.Add(feature))
            {
                if (feature.HasPriority && m_Events != null)
                    feature.OnInstalled(m_Controller, m_Events);
                else
                    feature.OnInstalled(m_Controller, (m_Controller as INetworkController).Events);

                if (m_Controller is INetworkPeer || m_Controller is INetworkController controller && controller.IsRunning)
                    feature.Enable();
            }

            return feature;
        }

        public TFeature GetFeature<TFeature>() where TFeature : INetworkFeature
            => m_Features.TryGetFirst(f => f is TFeature, out var networkFeature) ? (TFeature)networkFeature : default;

        public bool RemoveFeature<TFeature>() where TFeature : INetworkFeature
        {
            if (m_Features.TryGetFirst(f => f is TFeature, out var networkFeature))
            {
                if (networkFeature.IsEnabled)
                    networkFeature.Disable();

                if (networkFeature.HasPriority && m_Events != null)
                    networkFeature.OnUninstalled(m_Controller, m_Events);
                else
                    networkFeature.OnUninstalled(m_Controller, (m_Controller as INetworkController).Events);

                m_Features.RemoveWhere(f => !f.IsEnabled);
                return true;
            }

            return false;
        }

        public IList<IDataTarget> GetDataTargets()
            => m_Features.Where<IDataTarget>();

        private void OnStarting()
            => m_Features.ForEach(f => f.Enable());

        private void OnStopping()
            => m_Features.ForEach(f => f.Disable());
    }
}
