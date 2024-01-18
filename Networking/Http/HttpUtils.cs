using Grapevine;

using System.Text.Json;

namespace Networking.Http
{
    public static class HttpUtils
    {
        public static void Respond(this IHttpContext ctx, HttpResponseData responseData)
            => HttpResponseData.Respond(ctx, responseData);

        public static void RespondOk(this IHttpContext ctx, string responseData = null)
            => Respond(ctx, HttpResponseData.Ok(responseData ?? string.Empty));

        public static void RespondOk<TResponse>(this IHttpContext ctx, TResponse response)
            => Respond(ctx, HttpResponseData.Ok<TResponse>(response));

        public static void RespondFail(this IHttpContext ctx, string responseData = null, int responseCode = 401)
            => Respond(ctx, HttpResponseData.Fail(responseData ?? string.Empty, responseCode));

        public static void RespondFail<TResponse>(this IHttpContext ctx, TResponse responseData, int responseCode = 401)
            => Respond(ctx, HttpResponseData.Fail(JsonSerializer.Serialize(responseData), responseCode));

        public static bool TryAccess(this IHttpContext ctx, HttpAuthentificator authentificator, string perm)
        {
            var headerValue = ctx.Request.Headers.GetValue<string>("X-Key");

            if (string.IsNullOrWhiteSpace(headerValue))
            {
                if (string.IsNullOrWhiteSpace(perm) || authentificator is null)
                    return true;

                ctx.Respond(HttpResponseData.MissingKey());
                return false;
            }

            var authResult = authentificator.Authentificate(headerValue, perm);

            switch (authResult)
            {
                case HttpAuthentificationResult.Authorized:
                    return true;

                case HttpAuthentificationResult.Unauthorized:
                    ctx.Respond(HttpResponseData.Fail("UNAUTHORIZED"));
                    return false;

                case HttpAuthentificationResult.InvalidKey:
                    ctx.Respond(HttpResponseData.InvalidKey());
                    return false;

                default:
                    return false;
            }
        }
    }
}
