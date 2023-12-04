using Network.Interfaces.Features;

using System;
using System.Collections.Generic;

namespace Network.Features
{
    public class ControllerFeatureManager : IFeatureManager
    {
        private List<Type> features = new List<Type>();

        public void Enable(IFeatureManager manager) { }

        public void Disable() 
        {
            features.Clear();
        }

        public TFeature AddFeature<TFeature>() where TFeature : IFeature, new()
        {
            if (features.Contains(typeof(TFeature)))
                return default;

            features.Add(typeof(TFeature));
            return default;
        }

        public IFeature AddFeature(Type type)
        {
            if (features.Contains(type))
                return null;

            features.Add(type);
            return null;
        }

        public TFeature GetFeature<TFeature>() where TFeature : IFeature
        {
            if (!features.Contains(typeof(TFeature)))
                return default;

            return Activator.CreateInstance<TFeature>();
        }

        public IFeature GetFeature(Type type)
        {
            if (!features.Contains(type))
                return null;

            if (typeof(IFeature).IsAssignableFrom(type))
                return Activator.CreateInstance(type) as IFeature;

            return null;
        }

        public TFeature RemoveFeature<TFeature>() where TFeature : IFeature
        {
            RemoveFeature(typeof(TFeature));
            return default;
        }

        public IFeature RemoveFeature(Type type)
        {
            features.Remove(type);
            return null;
        }

        public Type[] GetLoadedFeatures()
            => features.ToArray();

        public IFeature[] GetEnabledFeatures()
            => null;
    }
}