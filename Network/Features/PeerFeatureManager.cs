using Common.IO.Collections;

using Network.Interfaces.Controllers;
using Network.Interfaces.Features;

using System;
using System.Linq;

namespace Network.Features
{
    public class PeerFeatureManager : IFeatureManager
    {
        private LockedDictionary<Type, IFeature> features;
        private IPeer peer;

        public PeerFeatureManager(IPeer peer)
            => this.peer = peer;

        public void Disable()
        {
            if (features is null)
                return;

            foreach (var feature in features.Reverse())
                feature.Value?.Stop();

            features.Clear();
            features = null;
        }

        public void Enable(IFeatureManager parent)
        {
            if (parent is null)
                return;

            features = new LockedDictionary<Type, IFeature>();

            var types = parent.GetLoadedFeatures();

            if (types is null || types.Length <= 0)
                return;

            foreach (var type in types)
            {
                if (features.ContainsKey(type))
                    continue;

                var feature = parent.GetFeature(type);

                if (feature is null) 
                    continue;

                features[type] = feature;

                feature.Start(peer);
            }
        }

        public TFeature AddFeature<TFeature>() where TFeature : IFeature, new()
        {
            if (features.TryGetValue(typeof(TFeature), out var feature))
                return (TFeature)feature;

            feature = features[typeof(TFeature)] = new TFeature();
            feature.Start(peer);

            return (TFeature)feature;
        }

        public IFeature AddFeature(Type type)
        {
            if (features.TryGetValue(type, out var feature))
                return feature;

            feature = features[type] = Activator.CreateInstance(type) as IFeature;
            feature.Start(peer);

            return feature;
        }

        public TFeature GetFeature<TFeature>() where TFeature : IFeature
        {
            if (features.TryGetValue(typeof(TFeature), out var feature))
                return (TFeature)feature;

            return default;
        }

        public IFeature GetFeature(Type type)
        {
            if (features.TryGetValue(type, out var feature))
                return feature;

            return null;
        }

        public TFeature RemoveFeature<TFeature>() where TFeature : IFeature
        {
            if (features.TryGetValue(typeof(TFeature), out var feature))
                feature.Stop();

            features.Remove(typeof(TFeature));

            return feature is null ? default : (TFeature)feature;
        }

        public IFeature RemoveFeature(Type type)
        {
            if (features.TryGetValue(type, out var feature))
                feature.Stop();

            features.Remove(type);

            return feature;
        }

        public IFeature[] GetEnabledFeatures()
            => features.Values.ToArray();

        public Type[] GetLoadedFeatures()
            => features.Keys.ToArray();
    }
}
