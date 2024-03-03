using Newtonsoft.Json;

namespace Common.Utilities
{
    public static class JsonUtils
    {
        private static readonly JsonSerializerSettings JsonSettings;

        static JsonUtils()
        {
            JsonSettings = new JsonSerializerSettings()
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented,

                CheckAdditionalContent = true
            };
        }

        public static T JsonDeserialize<T>(this string json)
            => JsonConvert.DeserializeObject<T>(json, JsonSettings);

        public static string JsonSerialize(this object value)
            => JsonConvert.SerializeObject(value, JsonSettings);
    }
}