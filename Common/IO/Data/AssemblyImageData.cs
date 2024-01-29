using Common.Logging;

using System;
using System.Reflection;

namespace Common.IO.Data
{
    public struct AssemblyImageData : IData
    {
        private Assembly loaded;

        public byte[] Image;

        public AssemblyImageData(byte[] image)
            => Image = image;

        public void Deserialize(DataReader reader)
        {
            Image = reader.ReadBytes();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteBytes(Image);
        }

        public Assembly Load()
        {
            if (loaded != null)
                return loaded;

            return loaded = Assembly.Load(Image);
        }

        public bool TryLoad(out Assembly assembly)
        {
            try
            {
                assembly = Load();
                return assembly != null;
            }
            catch (Exception ex)
            {
                LogOutput.Common.Info($"Failed to load assembly image:\n{ex}");

                assembly = null;
                return false;
            }
        }
    }
}