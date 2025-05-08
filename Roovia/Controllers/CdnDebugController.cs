using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roovia.Interfaces;

namespace Roovia.Controllers
{
    [ApiController]
    [Route("api/cdn-debug")]
    public class CdnDebugController : ControllerBase
    {
        private readonly ICdnService _cdnService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CdnDebugController> _logger;
        private const string PRODUCTION_API_URL = "https://portal.roovia.co.za/api/cdn";

        public CdnDebugController(
            ICdnService cdnService,
            IHttpClientFactory httpClientFactory,
            ILogger<CdnDebugController> logger)
        {
            _cdnService = cdnService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                success = true,
                controller = GetType().Name,
                timestamp = DateTime.Now,
                message = "CDN Debug controller is operational"
            });
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection([FromQuery] string url = null)
        {
            try
            {
                // Use provided URL or default to ping endpoint
                var endpoint = !string.IsNullOrEmpty(url) ? url : $"{PRODUCTION_API_URL}/ping";
                var apiKey = _cdnService.GetApiKey();

                // Create client with appropriate headers
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Testing connection to: {Endpoint} with API key {ApiKey}",
                    endpoint, apiKey);

                // Make request and get raw response
                var response = await client.GetAsync(endpoint);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Received response: Status={Status}, Content-Type={ContentType}, Length={Length}",
                    response.StatusCode,
                    response.Content.Headers.ContentType,
                    responseContent.Length);

                return Ok(new
                {
                    success = true,
                    request = new
                    {
                        url = endpoint,
                        method = "GET",
                        headers = client.DefaultRequestHeaders.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
                    },
                    response = new
                    {
                        statusCode = (int)response.StatusCode,
                        statusPhrase = response.ReasonPhrase,
                        headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                        contentHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                        contentPreview = responseContent.Length > 1000
                            ? responseContent.Substring(0, 1000) + "..."
                            : responseContent,
                        contentLength = responseContent.Length
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("test-upload")]
        public async Task<IActionResult> TestUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file was uploaded" });

            try
            {
                _logger.LogInformation("Testing file upload: {FileName}, {ContentType}, {Size} bytes",
                    file.FileName, file.ContentType, file.Length);

                // Create HTTP client with detailed logging
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(5);

                var apiKey = _cdnService.GetApiKey();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                _logger.LogInformation("Using API key: {ApiKey}", apiKey);

                // Create multipart content and add detailed logging
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(file.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                // Add file content and metadata
                content.Add(streamContent, "file", file.FileName);
                content.Add(new StringContent("test-uploads"), "category");

                // Prepare URL
                var apiUrl = $"{PRODUCTION_API_URL}/upload";
                _logger.LogInformation("Sending request to: {Url}", apiUrl);

                // Send the request and get raw response
                var response = await client.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Received response: Status={Status}, Content-Type={ContentType}, Length={Length}",
                    response.StatusCode,
                    response.Content.Headers.ContentType,
                    responseContent.Length);

                // Return detailed response information for debugging
                return Ok(new
                {
                    success = response.IsSuccessStatusCode,
                    request = new
                    {
                        url = apiUrl,
                        method = "POST",
                        fileName = file.FileName,
                        contentType = file.ContentType,
                        fileSize = file.Length,
                        headers = client.DefaultRequestHeaders.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
                    },
                    response = new
                    {
                        statusCode = (int)response.StatusCode,
                        statusPhrase = response.ReasonPhrase,
                        headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                        contentHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                        contentPreview = responseContent.Length > 1000
                            ? responseContent.Substring(0, 1000) + "..."
                            : responseContent,
                        contentLength = responseContent.Length
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test upload");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}