using System;
using System.Linq;
using System.Net;
using System.Text.Json;
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
        private readonly string[] _bypassPaths;

        public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _logger = logger;

            // Paths that bypass API key check (e.g., public endpoints)
            _bypassPaths = new string[]
            {
                "/api/health",
                "/cdn-dashboard",
                "/cdn-test",
                "/admin/cdn-config"
            };
        }

        public async Task Invoke(HttpContext context)
        {
            // Skip API key check if not a CDN API endpoint
            if (!context.Request.Path.Value.StartsWith("/api/cdn") ||
                _bypassPaths.Any(bp => context.Request.Path.Value.StartsWith(bp)))
            {
                await _next(context);
                return;
            }

            // Try to get API key from headers, query string, or cookie
            if (!TryGetApiKey(context, out string apiKey))
            {
                _logger.LogWarning("API key missing for request to {Path}", context.Request.Path);
                await WriteAccessDeniedResponse(context, "API key is required");
                return;
            }

            // Check if the API key is valid
            var validationResult = await ValidateApiKey(context, apiKey);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid API key used for request to {Path}: {Reason}",
                    context.Request.Path, validationResult.Message);
                await WriteAccessDeniedResponse(context, validationResult.Message);
                return;
            }

            // Successful validation - log access
            await LogApiAccess(context, apiKey, true);

            // Continue with the request pipeline
            await _next(context);
        }

        private bool TryGetApiKey(HttpContext context, out string apiKey)
        {
            apiKey = null;

            // Try to get from headers (preferred method)
            if (context.Request.Headers.TryGetValue("X-Api-Key", out var headerApiKey) &&
                !string.IsNullOrWhiteSpace(headerApiKey))
            {
                apiKey = headerApiKey;
                return true;
            }

            // Try to get from query string
            if (context.Request.Query.TryGetValue("key", out var queryApiKey) &&
                !string.IsNullOrWhiteSpace(queryApiKey))
            {
                apiKey = queryApiKey;
                return true;
            }

            // Try to get from cookies (least preferred, but useful for browser-based tools)
            if (context.Request.Cookies.TryGetValue("CDN-API-Key", out var cookieApiKey) &&
                !string.IsNullOrWhiteSpace(cookieApiKey))
            {
                apiKey = cookieApiKey;
                return true;
            }

            return false;
        }

        private async Task<(bool IsValid, string Message)> ValidateApiKey(HttpContext context, string apiKey)
        {
            try
            {
                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // First check if the API key exists
                    var apiKeyRecord = await dbContext.Set<CdnApiKey>()
                        .FirstOrDefaultAsync(k => k.Key == apiKey);

                    if (apiKeyRecord == null)
                    {
                        // If not found in database, check if it matches the default key in the configuration
                        var config = await dbContext.Set<CdnConfiguration>()
                            .Where(c => c.IsActive)
                            .OrderByDescending(c => c.Id)
                            .FirstOrDefaultAsync();

                        if (config != null && config.ApiKey == apiKey)
                        {
                            // Create an access log for default key
                            await LogApiAccess(context, apiKey, true);
                            return (true, "Valid default API key");
                        }

                        return (false, "Invalid API key");
                    }

                    // Check if the API key is active
                    if (!apiKeyRecord.IsActive)
                    {
                        return (false, "API key is inactive");
                    }

                    // Check if the API key has expired
                    if (apiKeyRecord.ExpiryDate.HasValue && apiKeyRecord.ExpiryDate.Value < DateTime.Now)
                    {
                        return (false, "API key has expired");
                    }

                    // Check IP restrictions if configured
                    if (!string.IsNullOrEmpty(apiKeyRecord.AllowedIps))
                    {
                        var clientIp = context.Connection.RemoteIpAddress.ToString();
                        var allowedIps = apiKeyRecord.AllowedIps.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        if (!IsIpAllowed(clientIp, allowedIps))
                        {
                            return (false, "IP address not authorized for this API key");
                        }
                    }

                    // Check domain restrictions if configured
                    if (!string.IsNullOrEmpty(apiKeyRecord.AllowedDomains) &&
                        context.Request.Headers.TryGetValue("Referer", out var referrer))
                    {
                        var refererHost = new Uri(referrer).Host;
                        var allowedDomains = apiKeyRecord.AllowedDomains.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        if (!allowedDomains.Any(d => refererHost.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
                        {
                            return (false, "Domain not authorized for this API key");
                        }
                    }

                    // Update usage stats
                    apiKeyRecord.AccessCount++;
                    apiKeyRecord.LastUsedDate = DateTime.Now;
                    apiKeyRecord.LastUsedBy = context.User?.Identity?.Name ?? "Anonymous";

                    await dbContext.SaveChangesAsync();

                    return (true, "Valid API key");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key: {Message}", ex.Message);

                // If validation fails due to technical issues, allow the request to continue
                // This prevents downtime if the database is temporarily unavailable
                return (true, "Validation bypassed due to technical issue");
            }
        }

        private bool IsIpAllowed(string clientIp, string[] allowedIps)
        {
            // Convert IP strings to IPAddress objects for proper comparison
            if (IPAddress.TryParse(clientIp, out var ipAddress))
            {
                foreach (var allowedIp in allowedIps)
                {
                    // Check for exact match
                    if (allowedIp.Equals(clientIp, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // Check for subnet match (CIDR notation)
                    if (allowedIp.Contains("/"))
                    {
                        if (IpAddressInSubnet(ipAddress, allowedIp))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IpAddressInSubnet(IPAddress ipAddress, string cidrSubnet)
        {
            try
            {
                // Parse CIDR notation (e.g., "192.168.1.0/24")
                var parts = cidrSubnet.Split('/');
                if (parts.Length != 2)
                {
                    return false;
                }

                var subnetIp = IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);

                // Only handle IPv4 for simplicity
                if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return false;
                }

                // Convert IPs to integers for mask application
                var ipBytes = ipAddress.GetAddressBytes();
                var subnetBytes = subnetIp.GetAddressBytes();

                if (ipBytes.Length != 4 || subnetBytes.Length != 4)
                {
                    return false;
                }

                // Apply the subnet mask to both IPs
                uint ip = (uint)((ipBytes[0] << 24) | (ipBytes[1] << 16) | (ipBytes[2] << 8) | ipBytes[3]);
                uint subnet = (uint)((subnetBytes[0] << 24) | (subnetBytes[1] << 16) | (subnetBytes[2] << 8) | subnetBytes[3]);
                uint mask = ~(uint.MaxValue >> prefixLength);

                return (ip & mask) == (subnet & mask);
            }
            catch
            {
                // If any parsing error occurs, return false
                return false;
            }
        }

        private async Task WriteAccessDeniedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private async Task LogApiAccess(HttpContext context, string apiKey, bool isSuccess)
        {
            try
            {
                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var log = new CdnAccessLog
                    {
                        Timestamp = DateTime.Now,
                        ActionType = context.Request.Method,
                        FilePath = context.Request.Path,
                        ApiKey = apiKey,
                        Username = context.User?.Identity?.Name,
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.Request.Headers["User-Agent"].ToString(),
                        Referrer = context.Request.Headers["Referer"].ToString(),
                        IsSuccess = isSuccess
                    };

                    dbContext.Add(log);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the request if logging fails
                _logger.LogError(ex, "Error logging API access: {Message}", ex.Message);
            }
        }
    }
}