using System;
using System.Text;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Roovia.Interfaces;

namespace Roovia.Controllers
{
    [ApiController]
    [Route("api/cdn-test")]
    public class CdnTestController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICdnService _cdnService;
        private readonly ILogger<CdnTestController> _logger;

        public CdnTestController(
            IHttpClientFactory httpClientFactory,
            ICdnService cdnService,
            ILogger<CdnTestController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cdnService = cdnService;
            _logger = logger;
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            var apiKey = _cdnService.GetApiKey();
            var client = _httpClientFactory.CreateClient();

            // Add all possible headers that might be required
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var productionUrl = "https://portal.roovia.co.za/api/cdn/ping";

            try
            {
                // Get response as string first to diagnose what's coming back
                var response = await client.GetAsync(productionUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log everything for debugging
                _logger.LogInformation("API response: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                return Ok(new
                {
                    success = response.IsSuccessStatusCode,
                    statusCode = (int)response.StatusCode,
                    statusPhrase = response.ReasonPhrase,
                    contentType = response.Content.Headers.ContentType?.ToString(),
                    content = responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent,
                    requestUrl = productionUrl,
                    headers = response.Headers.ToDictionary(h => h.Key, h => h.Value)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
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