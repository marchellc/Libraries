namespace Network.Interfaces
{
    public interface IFeatures
    {
        T AddFeature<T>() where T : IFeature, new();
        T GetFeature<T>() where T : IFeature;

        void RemoveFeature<T>() where T : IFeature;
    }
}