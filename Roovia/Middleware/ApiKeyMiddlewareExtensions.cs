// Extensions/ApiKeyMiddlewareExtensions.cs
using Microsoft.AspNetCore.Builder;
using Roovia.Middleware;

namespace Roovia.Extensions
{
    public static class ApiKeyMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>();
        }
    }
}