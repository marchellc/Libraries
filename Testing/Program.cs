using Common.Logging;
using Common.Serialization;
using Common.Utilities;

using System;
using System.Threading.Tasks;

namespace Testing
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var randomId = Generator.Instance.GetString();
                var randomObject = new TestObject() { StringValue = randomId };

                Logger.Info($"Random String: {randomId}");

                var randomBytes = Serializer.Serialize(serializer => serializer.PutSerializable(randomObject));

                Logger.Info($"Bytes: {randomBytes.Length}");

                Deserializer.Deserialize(randomBytes, des =>
                {
                    var randomStr = des.GetDeserializable<TestObject>();
                    Logger.Info($"Random String 3: {randomStr.StringValue}");
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            await Task.Delay(-1);
        }
    }

    public class TestObject : Common.Serialization.Object
    {
        public TestObject() { }

        public string StringValue { get; set; }

        public override void Deserialize(Deserializer deserializer)
        {
            base.Deserialize(deserializer);
            StringValue = deserializer.GetString();
        }

        public override void Serialize(Serializer serializer)
        {
            base.Serialize(serializer);
            serializer.Put(StringValue);
        }
    }
}