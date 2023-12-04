using System;

namespace Network.Interfaces.Features
{
    public interface IFeatureManager
    {
        TFeature GetFeature<TFeature>() where TFeature : IFeature;
        IFeature GetFeature(Type type);

        TFeature AddFeature<TFeature>() where TFeature : IFeature, new();
        IFeature AddFeature(Type type);

        TFeature RemoveFeature<TFeature>() where TFeature : IFeature;
        IFeature RemoveFeature(Type type);

        IFeature[] GetEnabledFeatures();

        Type[] GetLoadedFeatures();

        void Enable(IFeatureManager parent);
        void Disable();
    }
}