namespace Cef.OIDC.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Http;

    [ExcludeFromCodeCoverage]
    public static class HttpRequestExtensions
    {
        public static string GetOrigin(this HttpRequest request)
        {
            string origin;

            if (request.Headers.TryGetValue("Referer", out var referrer))
            {
                var uri = new Uri(referrer);
                origin = $"{uri.GetLeftPart(UriPartial.Scheme)}{uri.Host}";
            }
            else
            {
                origin = $"{request.Scheme}://{request.Host}";
            }

            return origin;
        }
    }
}
