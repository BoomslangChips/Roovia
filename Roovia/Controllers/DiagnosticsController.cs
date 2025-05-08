using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roovia.Interfaces;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;

namespace Roovia.Controllers
{
    [ApiController]
    [Route("api/diagnostics")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ICdnService _cdnService;
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public DiagnosticsController(
            ICdnService cdnService,
            ILogger<DiagnosticsController> logger,
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
                timestamp = DateTime.Now,
                message = "Diagnostics controller is running"
            });
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    cdn = new
                    {
                        baseUrl = _cdnService.GetCdnUrl(""),
                        apiKey = _cdnService.GetApiKey().Substring(0, 5) + "..." + _cdnService.GetApiKey().Substring(_cdnService.GetApiKey().Length - 5),
                        isDirectAccessAvailable = _cdnService.IsDirectAccessAvailable(),
                        isDevEnvironment = _cdnService.IsDevEnvironment()
                    },
                    server = new
                    {
                        timestamp = DateTime.Now,
                        timeZone = TimeZoneInfo.Local.DisplayName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration info");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var apiKey = _cdnService.GetApiKey();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                var results = new List<object>();

                // Test the production API
                try
                {
                    var response = await client.GetAsync("https://portal.roovia.co.za/api/cdn/categories");
                    results.Add(new
                    {
                        endpoint = "Production API (categories)",
                        success = response.IsSuccessStatusCode,
                        statusCode = (int)response.StatusCode,
                        statusMessage = response.ReasonPhrase
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        endpoint = "Production API (categories)",
                        success = false,
                        error = ex.Message
                    });
                }

                // Test local controllers
                try
                {
                    var baseUrl = new Uri($"{Request.Scheme}://{Request.Host}");

                    // Test CdnController
                    try
                    {
                        var cdnResponse = await client.GetAsync(new Uri(baseUrl, "api/cdncompat/ping"));
                        results.Add(new
                        {
                            endpoint = "Local CdnController (ping)",
                            success = cdnResponse.IsSuccessStatusCode,
                            statusCode = (int)cdnResponse.StatusCode,
                            statusMessage = cdnResponse.ReasonPhrase
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            endpoint = "Local CdnController (ping)",
                            success = false,
                            error = ex.Message
                        });
                    }

                    // Test ExtendedCdnController
                    try
                    {
                        var extResponse = await client.GetAsync(new Uri(baseUrl, "api/cdn/ping"));
                        results.Add(new
                        {
                            endpoint = "Local ExtendedCdnController (ping)",
                            success = extResponse.IsSuccessStatusCode,
                            statusCode = (int)extResponse.StatusCode,
                            statusMessage = extResponse.ReasonPhrase
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            endpoint = "Local ExtendedCdnController (ping)",
                            success = false,
                            error = ex.Message
                        });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        endpoint = "Local API Tests",
                        success = false,
                        error = ex.Message
                    });
                }

                return Ok(new
                {
                    success = true,
                    timestamp = DateTime.Now,
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connections");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("headers")]
        public IActionResult GetHeaders()
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in Request.Headers)
            {
                headers[header.Key] = header.Value;
            }

            return Ok(new
            {
                success = true,
                requestHeaders = headers,
                remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                requestMethod = Request.Method,
                requestPath = Request.Path,
                requestQueryString = Request.QueryString.ToString()
            });
        }
    }
}