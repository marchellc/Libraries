using Grapevine;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Networking.Http
{
    public class HttpResponseData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }

        public static HttpResponseData MissingKey()
            => new HttpResponseData
            {
                Code = 401,

                Data = "MISSING_AUTH",

                Success = false
            };

        public static HttpResponseData InvalidKey()
            => new HttpResponseData
            {
                Code = 401,

                Data = "INVALID_AUTH",

                Success = false
            };

        public static HttpResponseData Ok<TObject>(TObject response, int responseCode = 200)
            => new HttpResponseData
            {
                Code = responseCode,

                Data = JsonSerializer.Serialize(response),

                Success = false
            };

        public static HttpResponseData Ok(string response, int responseCode = 200)
            => new HttpResponseData
            {
                Code = responseCode,
                Data = response,

                Success = false
            };

        public static HttpResponseData Fail(string response = null, int responseCode = 403)
            => new HttpResponseData
            {
                Code = responseCode,
                Data = response,

                Success = false
            };

        public static void Respond(IHttpContext context, HttpResponseData responseData)
        {
            if (context.WasRespondedTo)
                return;

            context.Response.StatusCode = responseData.Code;
            context.Response.StatusDescription = responseData.Data;

            context.Response.SendResponseAsync(JsonSerializer.Serialize(responseData));
        }
    }
}