using System.Text.Json;

namespace Common.Extensions
{
    public static class JsonExtensions
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IncludeFields = false,
            WriteIndented = true,
        };

        public static T Deserialize<T>(this string json)
            => JsonSerializer.Deserialize<T>(json, Options);

        public static string Serialize(this object obj)
            => JsonSerializer.Serialize(obj, Options);
    }
}
