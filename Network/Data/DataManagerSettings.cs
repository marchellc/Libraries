namespace Network.Data
{
    public struct DataManagerSettings
    {
        public bool ReadSegments { get; set; }

        public DataManagerSettings(bool readSegments)
            => ReadSegments = readSegments;
    }
}