using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roovia.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Roovia.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CdnController : ControllerBase
    {
        private readonly ICdnService _cdnService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CdnController> _logger;
        private const string PRODUCTION_API_URL = "https://portal.roovia.co.za/api/cdn";

        public CdnController(
            ICdnService cdnService,
            IHttpClientFactory httpClientFactory,
            ILogger<CdnController> logger)
        {
            _cdnService = cdnService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(209715200)] // 200MB in bytes
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200MB in bytes
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string category = "documents", [FromForm] string folder = "")
        {
            var apiKey = _cdnService.GetApiKey();
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var requestApiKey) ||
                string.IsNullOrWhiteSpace(requestApiKey) ||
                requestApiKey != apiKey)
            {
                // Check if query parameter has the API key as fallback
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != apiKey)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file was uploaded" });

            try
            {
                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout for large uploads
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                // Prepare multipart form
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(file.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "file", file.FileName);
                content.Add(new StringContent(category), "category");

                if (!string.IsNullOrEmpty(folder))
                {
                    content.Add(new StringContent(folder), "folder");
                }

                // Forward request to production API
                var response = await client.PostAsync($"{PRODUCTION_API_URL}/upload", content);
                var result = await response.Content.ReadFromJsonAsync<dynamic>();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding upload to production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile([FromQuery] string path)
        {
            var apiKey = _cdnService.GetApiKey();
            // Check API key for external requests
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var requestApiKey) ||
                string.IsNullOrWhiteSpace(requestApiKey) ||
                requestApiKey != apiKey)
            {
                // Check if query parameter has the API key
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != apiKey)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (string.IsNullOrEmpty(path))
                return BadRequest(new { success = false, message = "No file path provided" });

            try
            {
                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                // Forward request to production API
                var response = await client.DeleteAsync($"{PRODUCTION_API_URL}/delete?path={Uri.EscapeDataString(path)}");
                var result = await response.Content.ReadFromJsonAsync<dynamic>();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding delete to production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("rename")]
        public async Task<IActionResult> RenameFile([FromBody] RenameFileRequest request)
        {
            var apiKey = _cdnService.GetApiKey();
            // Check API key
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var requestApiKey) ||
                string.IsNullOrWhiteSpace(requestApiKey) ||
                requestApiKey != apiKey)
            {
                return Unauthorized(new { success = false, message = "Invalid API key" });
            }

            if (string.IsNullOrEmpty(request.Path) || string.IsNullOrEmpty(request.NewName))
                return BadRequest(new { success = false, message = "Path and new name are required" });

            try
            {
                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                // Forward request to production API
                var response = await client.PostAsJsonAsync($"{PRODUCTION_API_URL}/rename", request);
                var result = await response.Content.ReadFromJsonAsync<dynamic>();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding rename to production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("details")]
        public async Task<IActionResult> GetFileDetails([FromQuery] string path)
        {
            var apiKey = _cdnService.GetApiKey();
            // Check API key
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var requestApiKey) ||
                string.IsNullOrWhiteSpace(requestApiKey) ||
                requestApiKey != apiKey)
            {
                // Check query parameter
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != apiKey)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (string.IsNullOrEmpty(path))
                return BadRequest(new { success = false, message = "No file path provided" });

            try
            {
                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                // Forward request to production API
                var response = await client.GetAsync($"{PRODUCTION_API_URL}/details?path={Uri.EscapeDataString(path)}");
                var result = await response.Content.ReadFromJsonAsync<dynamic>();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding details request to production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("view")]
        public async Task<IActionResult> ViewFile([FromQuery] string path)
        {
            var apiKey = _cdnService.GetApiKey();
            // Check for API key in header first
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var requestApiKey) ||
                string.IsNullOrWhiteSpace(requestApiKey) ||
                requestApiKey != apiKey)
            {
                // Check if query parameter has the API key
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != apiKey)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (string.IsNullOrEmpty(path))
                return BadRequest(new { success = false, message = "No file path provided" });

            try
            {
                // For view requests, simply redirect to the CDN URL with API key
                var separator = path.Contains("?") ? "&" : "?";
                return Redirect($"{path}{separator}key={apiKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redirecting to CDN URL: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetFiles([FromQuery] string category = "documents", [FromQuery] string pattern = "*", [FromQuery] string folder = "")
        {
            var apiKey = _cdnService.GetApiKey();
            // Check for API key in header
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var requestApiKey) ||
                string.IsNullOrWhiteSpace(requestApiKey) ||
                requestApiKey != apiKey)
            {
                // Check if query parameter has the API key
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != apiKey)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            try
            {
                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                // Build the URL with query parameters
                var url = $"{PRODUCTION_API_URL}/files?category={category}";
                if (!string.IsNullOrEmpty(pattern) && pattern != "*")
                {
                    url += $"&pattern={Uri.EscapeDataString(pattern)}";
                }
                if (!string.IsNullOrEmpty(folder))
                {
                    url += $"&folder={Uri.EscapeDataString(folder)}";
                }

                // Forward request to production API
                var response = await client.GetAsync(url);
                var result = await response.Content.ReadFromJsonAsync<dynamic>();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding files request to production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        public class RenameFileRequest
        {
            public string Path { get; set; }
            public string NewName { get; set; }
        }
    }
}