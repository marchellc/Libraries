using Network.Data;

using System.Collections.Generic;

namespace Network.Features
{
    public interface INetworkFeatureManager
    {
        INetworkObject Controller { get; }
        IReadOnlyCollection<INetworkFeature> AllFeatures { get; }

        TFeature AddFeature<TFeature>() where TFeature : INetworkFeature;
        TFeature GetFeature<TFeature>() where TFeature : INetworkFeature;

        IList<IDataTarget> GetDataTargets();

        bool RemoveFeature<TFeature>() where TFeature : INetworkFeature;
    }
}