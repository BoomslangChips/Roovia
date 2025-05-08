using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Models.CDN;

namespace Roovia.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;

        public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip non-CDN API requests
            if (!IsCdnApiRequest(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // For non-OPTIONS requests that require authentication
            if (context.Request.Method != "OPTIONS" && RequiresAuthentication(context.Request.Path))
            {
                try
                {
                    var apiKey = GetApiKey(context.Request);
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        _logger.LogWarning("API request missing API key: {Path}", context.Request.Path);
                        context.Response.StatusCode = 401; // Unauthorized
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"success\":false,\"message\":\"API key is required\"}");
                        return;
                    }

                    // Log the API key for debugging (partial)
                    var maskedKey = apiKey.Length > 8
                        ? $"{apiKey.Substring(0, 4)}...{apiKey.Substring(apiKey.Length - 4)}"
                        : "****";
                    _logger.LogDebug("API request with key {Key} to {Path}", maskedKey, context.Request.Path);

                    // Validate the API key
                    if (!await ValidateApiKeyAsync(context, apiKey))
                    {
                        _logger.LogWarning("Invalid API key: {Key} for {Path}", maskedKey, context.Request.Path);
                        context.Response.StatusCode = 401; // Unauthorized
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"success\":false,\"message\":\"Invalid API key\"}");
                        return;
                    }

                    // Valid API key - continue
                    _logger.LogDebug("Valid API key for {Path}", context.Request.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating API key for {Path}", context.Request.Path);
                    context.Response.StatusCode = 500; // Internal Server Error
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"success\":false,\"message\":\"Internal server error during API key validation\"}");
                    return;
                }
            }

            // Continue processing the request
            await _next(context);
        }

        private bool IsCdnApiRequest(PathString path)
        {
            var pathStr = path.ToString().ToLower();
            return pathStr.StartsWith("/api/cdn") ||
                   pathStr.StartsWith("/api/cdncompat") ||
                   pathStr.StartsWith("/api/cdn-debug");
        }

        private bool RequiresAuthentication(PathString path)
        {
            var pathStr = path.ToString().ToLower();

            // Allow ping without authentication for health checks
            if (pathStr.EndsWith("/ping"))
                return false;

            // Certain diagnostic endpoints might be exempted
            if (pathStr.StartsWith("/api/cdn-debug/") &&
                (pathStr.Contains("/test-connection") || pathStr.Contains("/diagnostics")))
                return false;

            return true;
        }

        private string GetApiKey(HttpRequest request)
        {
            // Try to get from header first (preferred method)
            if (request.Headers.TryGetValue("X-Api-Key", out var headerApiKey) &&
                !string.IsNullOrWhiteSpace(headerApiKey))
            {
                return headerApiKey;
            }

            // Then try from query string
            if (request.Query.TryGetValue("key", out var queryApiKey) &&
                !string.IsNullOrWhiteSpace(queryApiKey))
            {
                return queryApiKey;
            }

            // Finally try from form data for multipart uploads
            if (request.HasFormContentType &&
                request.Form.TryGetValue("apiKey", out var formApiKey) &&
                !string.IsNullOrWhiteSpace(formApiKey))
            {
                return formApiKey;
            }

            return null;
        }

        private async Task<bool> ValidateApiKeyAsync(HttpContext context, string apiKey)
        {
            try
            {
                // First, fast check against bootstrap config (from appsettings.json)
                var configuration = context.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                var bootstrapApiKey = configuration["CDN:ApiKey"];

                if (!string.IsNullOrEmpty(bootstrapApiKey) && bootstrapApiKey == apiKey)
                {
                    _logger.LogDebug("API key validated against bootstrap configuration");
                    return true;
                }

                // Then check against database
                using var scope = context.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check active configuration
                var configApiKey = await dbContext.Set<CdnConfiguration>()
                    .Where(c => c.IsActive)
                    .Select(c => c.ApiKey)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(configApiKey) && configApiKey == apiKey)
                {
                    _logger.LogDebug("API key validated against database configuration");
                    return true;
                }

                // Check against specific API keys
                var isValid = await dbContext.Set<CdnApiKey>()
                    .AnyAsync(k => k.Key == apiKey && k.IsActive);

                if (isValid)
                {
                    // Update usage statistics
                    try
                    {
                        var keyToUpdate = await dbContext.Set<CdnApiKey>()
                            .FirstOrDefaultAsync(k => k.Key == apiKey && k.IsActive);

                        if (keyToUpdate != null)
                        {
                            keyToUpdate.LastUsedDate = DateTime.Now;
                            keyToUpdate.AccessCount++;
                            keyToUpdate.LastUsedBy = context.User?.Identity?.Name ?? "Anonymous";

                            await dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail if stats update fails
                        _logger.LogWarning(ex, "Failed to update API key usage statistics");
                    }

                    _logger.LogDebug("API key validated against database API keys");
                }
                else
                {
                    _logger.LogWarning("API key not found in database: {KeyPrefix}...",
                        apiKey.Length > 4 ? apiKey.Substring(0, 4) : apiKey);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return false;
            }
        }
    }
}