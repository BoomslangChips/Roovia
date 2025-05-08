// DiagnosticsController.cs - Add this temporarily to your project
using Microsoft.AspNetCore.Mvc;
using Roovia.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly ICdnService _cdnService;
    private readonly ILogger<DiagnosticsController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DiagnosticsController(
        ICdnService cdnService,
        ILogger<DiagnosticsController> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _cdnService = cdnService;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet("test-permissions")]
    public IActionResult TestPermissions()
    {
        var results = new Dictionary<string, string>();

        try
        {
            // Get the storage path from config
            var storagePath = _configuration["CDN:StoragePath"];
            results["ConfiguredPath"] = storagePath;

            // Check if the path exists
            bool pathExists = Directory.Exists(storagePath);
            results["PathExists"] = pathExists.ToString();

            // Try to create a test directory
            var testDir = Path.Combine(storagePath, "test-permissions");
            try
            {
                Directory.CreateDirectory(testDir);
                results["CreateDirectoryResult"] = "Success";
            }
            catch (Exception ex)
            {
                results["CreateDirectoryResult"] = $"Failed: {ex.Message}";
            }

            // Try to create a test file
            var testFilePath = Path.Combine(testDir, "test.txt");
            try
            {
                System.IO.File.WriteAllText(testFilePath, "Test content");
                results["CreateFileResult"] = "Success";

                // Try to read the file back
                var content = System.IO.File.ReadAllText(testFilePath);
                results["ReadFileResult"] = content == "Test content" ? "Success" : "Content mismatch";

                // Clean up test file
                System.IO.File.Delete(testFilePath);
                results["DeleteFileResult"] = "Success";
            }
            catch (Exception ex)
            {
                results["CreateFileResult"] = $"Failed: {ex.Message}";
            }

            // Check if direct access is available according to the service
            results["IsDirectAccessAvailable"] = _cdnService.IsDirectAccessAvailable().ToString();
            results["IsDevEnvironment"] = _environment.IsDevelopment().ToString();

            return Ok(results);
        }
        catch (Exception ex)
        {
            results["Error"] = ex.ToString();
            return StatusCode(500, results);
        }
    }
}