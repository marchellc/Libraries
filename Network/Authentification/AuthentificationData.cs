using Common.IO.Data;

namespace Network.Authentification
{
    public struct AuthentificationData : IAuthentificationData, IData
    {
        private string _key;

        public string ClientKey => _key;

        public void Deserialize(DataReader reader)
            => _key = reader.ReadString();

        public void Serialize(DataWriter writer)
            => writer.WriteString(_key);
    }
}