using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roovia.Interfaces;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Roovia.Controllers
{
    [ApiController]
    [Route("api/cdn")]
    public class ExtendedCdnController : ControllerBase
    {
        private readonly ICdnService _cdnService;
        private readonly ILogger<ExtendedCdnController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string PRODUCTION_API_URL = "https://portal.roovia.co.za/api/cdn";

        public ExtendedCdnController(
            ICdnService cdnService,
            ILogger<ExtendedCdnController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _cdnService = cdnService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                success = true,
                controller = GetType().Name,
                timestamp = DateTime.Now
            });
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var response = await client.GetAsync($"{PRODUCTION_API_URL}/categories");
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
                _logger.LogError(ex, "Error getting categories from production");
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("folders")]
        public async Task<IActionResult> GetFolders([FromQuery] string category = "documents")
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var response = await client.GetAsync($"{PRODUCTION_API_URL}/folders?category={category}");
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
                _logger.LogError(ex, "Error getting folders from production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("folder")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var response = await client.PostAsJsonAsync($"{PRODUCTION_API_URL}/folder", request);
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
                _logger.LogError(ex, "Error creating folder on production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("folder/rename")]
        public async Task<IActionResult> RenameFolder([FromBody] RenameFolderRequest request)
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var response = await client.PostAsJsonAsync($"{PRODUCTION_API_URL}/folder/rename", request);
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
                _logger.LogError(ex, "Error renaming folder on production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete("folder")]
        public async Task<IActionResult> DeleteFolder([FromQuery] string category, [FromQuery] string path)
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var response = await client.DeleteAsync($"{PRODUCTION_API_URL}/folder?category={Uri.EscapeDataString(category)}&path={Uri.EscapeDataString(path)}");
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
                _logger.LogError(ex, "Error deleting folder on production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetFiles(
            [FromQuery] string category = "documents",
            [FromQuery] string folder = "",
            [FromQuery] string search = "")
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var url = $"{PRODUCTION_API_URL}/files?category={Uri.EscapeDataString(category)}";
                if (!string.IsNullOrEmpty(folder))
                {
                    url += $"&folder={Uri.EscapeDataString(folder)}";
                }
                if (!string.IsNullOrEmpty(search))
                {
                    url += $"&search={Uri.EscapeDataString(search)}";
                }

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
                _logger.LogError(ex, "Error getting files from production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("upload")]
        [RequestSizeLimit(209715200)] // 200MB in bytes
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200MB in bytes
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string category = "documents", [FromForm] string folder = "")
        {
            // Check API key for external requests
            var apiKey = _cdnService.GetApiKey();
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var requestApiKey) ||
                string.IsNullOrWhiteSpace(requestApiKey) ||
                requestApiKey != apiKey)
            {
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
                // Use the CDN service directly to upload the file
                string fileUrl = await _cdnService.UploadFileAsync(file, category, folder);

                // Return success response with the URL
                return Ok(new
                {
                    success = true,
                    url = fileUrl,
                    fileName = file.FileName,
                    contentType = file.ContentType,
                    size = file.Length,
                    category = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file upload: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("move")]
        public async Task<IActionResult> MoveFiles([FromBody] MoveFilesRequest request)
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var response = await client.PostAsJsonAsync($"{PRODUCTION_API_URL}/move", request);
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
                _logger.LogError(ex, "Error moving files on production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("rename")]
        public async Task<IActionResult> RenameFile([FromBody] RenameFileRequest request)
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

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
                _logger.LogError(ex, "Error renaming file on production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("maintenance/clean")]
        public async Task<IActionResult> CleanOrphanedFiles()
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var response = await client.PostAsync($"{PRODUCTION_API_URL}/maintenance/clean", null);
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
                _logger.LogError(ex, "Error cleaning orphaned files on production: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("maintenance/clearcache")]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var response = await client.PostAsync($"{PRODUCTION_API_URL}/maintenance/clearcache", null);
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
                _logger.LogError(ex, "Error clearing cache on production: {Message}", ex.Message);
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
                // For viewing files, redirect to CDN with API key
                var separator = path.Contains("?") ? "&" : "?";
                return Redirect($"{path}{separator}key={apiKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redirecting to file: {Path}", path);
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

        #region Request Models

        public class CreateFolderRequest
        {
            public string Category { get; set; }
            public string Path { get; set; }
        }

        public class RenameFolderRequest
        {
            public string Category { get; set; }
            public string Path { get; set; }
            public string NewName { get; set; }
        }

        public class MoveFilesRequest
        {
            public string Category { get; set; }
            public List<string> Files { get; set; }
            public string TargetFolder { get; set; }
        }

        public class RenameFileRequest
        {
            public string Path { get; set; }
            public string NewName { get; set; }
        }

        #endregion
    }
}