using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roovia.Interfaces;
using System.Linq;
using System.Collections.Generic;

namespace Roovia.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CdnController : ControllerBase
    {
        private readonly ICdnService _cdnService;
        private const string API_KEY = "RooviaCDNKey"; // Hardcoded API key
        private const long MAX_FILE_SIZE = 200 * 1024 * 1024; // 200MB limit

        public CdnController(ICdnService cdnService)
        {
            _cdnService = cdnService;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(209715200)] // 200MB in bytes
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200MB in bytes
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string category = "documents")
        {
            // Check API key for external requests
            if (!HttpContext.Request.Host.Host.Contains("roovia.co.za"))
            {
                if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
                    string.IsNullOrWhiteSpace(apiKey) ||
                    apiKey != API_KEY)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file was uploaded" });

            try
            {
                // Check file size (limit to 200MB)
                if (file.Length > MAX_FILE_SIZE)
                    return BadRequest(new { success = false, message = $"File size exceeds the {MAX_FILE_SIZE / (1024 * 1024)}MB limit" });

                // Validate file type (basic validation)
                var allowedTypes = new[] { "image/", "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument", "text/plain", "text/csv", "video/", "audio/" };
                bool isValidType = false;
                foreach (var type in allowedTypes)
                {
                    if (file.ContentType.StartsWith(type))
                    {
                        isValidType = true;
                        break;
                    }
                }

                if (!isValidType)
                    return BadRequest(new { success = false, message = "File type not allowed" });

                // Upload the file using the CDN service
                var fileUrl = await _cdnService.UploadFileAsync(file, category);

                // Return success with the file URL
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
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile([FromQuery] string path)
        {
            // Check API key for external requests
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                apiKey != API_KEY)
            {
                // Check if query parameter has the API key
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != API_KEY)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (string.IsNullOrEmpty(path))
                return BadRequest(new { success = false, message = "No file path provided" });

            try
            {
                var result = await _cdnService.DeleteFileAsync(path);
                if (result)
                    return Ok(new { success = true, message = "File deleted successfully" });
                else
                    return NotFound(new { success = false, message = "File not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        // Add a new endpoint for file renaming
        [HttpPost("rename")]
        public async Task<IActionResult> RenameFile([FromBody] RenameFileRequest request)
        {
            // Check API key
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                apiKey != API_KEY)
            {
                return Unauthorized(new { success = false, message = "Invalid API key" });
            }

            if (string.IsNullOrEmpty(request.Path) || string.IsNullOrEmpty(request.NewName))
                return BadRequest(new { success = false, message = "Path and new name are required" });

            try
            {
                // Validate new filename (remove potentially dangerous characters)
                var safeNewName = Path.GetFileNameWithoutExtension(request.NewName)
                    .Replace(" ", "-")
                    .Replace("_", "-");

                // Keep the original extension
                var originalExtension = Path.GetExtension(request.Path);

                // Get the CDN base URL and extract category and filename
                var cdnBaseUrl = _cdnService.GetCdnUrl("").TrimEnd('/');
                var relativePath = request.Path.Replace(cdnBaseUrl, "").TrimStart('/');
                var pathParts = relativePath.Split('/');

                if (pathParts.Length < 2)
                    return BadRequest(new { success = false, message = "Invalid file path" });

                var category = pathParts[0];

                // Generate the new path
                var newFilename = $"{safeNewName}{originalExtension}";

                // Check if we have direct file access
                if (_cdnService.IsDirectAccessAvailable())
                {
                    // Get physical paths
                    var oldPhysicalPath = _cdnService.GetPhysicalPath(request.Path);
                    var directoryPath = Path.GetDirectoryName(oldPhysicalPath);
                    var newPhysicalPath = Path.Combine(directoryPath, newFilename);

                    // Check if old file exists
                    if (!System.IO.File.Exists(oldPhysicalPath))
                        return NotFound(new { success = false, message = "File not found" });

                    // Check if new filename already exists
                    if (System.IO.File.Exists(newPhysicalPath))
                        return BadRequest(new { success = false, message = "A file with that name already exists" });

                    // Rename the file
                    System.IO.File.Move(oldPhysicalPath, newPhysicalPath);

                    // Return the new URL
                    var newUrl = $"{cdnBaseUrl}/{category}/{newFilename}";
                    return Ok(new { success = true, url = newUrl, message = "File renamed successfully" });
                }
                else
                {
                    // Remote CDN - we'd need to implement API endpoint on the remote server
                    // For now, return not supported
                    return StatusCode(501, new { success = false, message = "Renaming not supported on remote CDN" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        // Add a controller method to get file details
        [HttpGet("details")]
        public IActionResult GetFileDetails([FromQuery] string path)
        {
            // Check API key
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                apiKey != API_KEY)
            {
                // Check query parameter
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != API_KEY)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (string.IsNullOrEmpty(path))
                return BadRequest(new { success = false, message = "No file path provided" });

            try
            {
                // Check if we have direct file access
                if (_cdnService.IsDirectAccessAvailable())
                {
                    var physicalPath = _cdnService.GetPhysicalPath(path);
                    if (string.IsNullOrEmpty(physicalPath) || !System.IO.File.Exists(physicalPath))
                        return NotFound(new { success = false, message = "File not found" });

                    var fileInfo = new System.IO.FileInfo(physicalPath);
                    var fileName = Path.GetFileName(physicalPath);
                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    var contentType = GetContentTypeFromPath(fileName);

                    return Ok(new
                    {
                        success = true,
                        fileName = fileName,
                        extension = extension,
                        contentType = contentType,
                        size = fileInfo.Length,
                        createdDate = fileInfo.CreationTime,
                        modifiedDate = fileInfo.LastWriteTime,
                        physicalPath = physicalPath
                    });
                }
                else
                {
                    // Remote CDN - limited information
                    var fileName = Path.GetFileName(path);
                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    var contentType = GetContentTypeFromPath(fileName);

                    return Ok(new
                    {
                        success = true,
                        fileName = fileName,
                        extension = extension,
                        contentType = contentType,
                        size = -1, // Unknown
                        createdDate = DateTime.MinValue, // Unknown
                        modifiedDate = DateTime.MinValue // Unknown
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        // Add a controller method to serve files directly with authentication
        [HttpGet("view")]
        public async Task<IActionResult> ViewFile([FromQuery] string path)
        {
            // Check for API key in header first
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                apiKey != API_KEY)
            {
                // Check if query parameter has the API key
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != API_KEY)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (string.IsNullOrEmpty(path))
                return BadRequest(new { success = false, message = "No file path provided" });

            try
            {
                // Extract the physical path from the CDN URL
                var cdnBaseUrl = _cdnService.GetCdnUrl("").TrimEnd('/');
                var relativePath = path.Replace(cdnBaseUrl, "").TrimStart('/');

                // Get the CDN storage path from configuration
                var configPath = _cdnService.GetPhysicalPath(path);
                if (string.IsNullOrEmpty(configPath))
                {
                    // Fallback to constructing the path
                    var parts = relativePath.Split('/');
                    var category = parts.Length > 0 ? parts[0] : "documents";
                    var fileName = parts.Length > 1 ? parts[1] : relativePath;

                    // Get CDN storage path from the service
                    configPath = Path.Combine(_cdnService.GetPhysicalPath(cdnBaseUrl), category, fileName);
                }

                // Check if the file exists
                if (!System.IO.File.Exists(configPath))
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                // Try to determine content type from extension
                var contentType = GetContentTypeFromPath(path);

                // Set cache headers for better performance
                Response.Headers.Add("Cache-Control", "public, max-age=604800"); // 7 days
                Response.Headers.Add("Expires", DateTime.UtcNow.AddDays(7).ToString("R"));

                // Return the file with streaming enabled for large files
                return PhysicalFile(configPath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        // Updated method to get files in a category
        [HttpGet("files")]
        public async Task<IActionResult> GetFiles([FromQuery] string category = "documents", [FromQuery] string pattern = "*")
        {
            // Check for API key in header
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                apiKey != API_KEY)
            {
                // Check if query parameter has the API key
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != API_KEY)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            try
            {
                // Validate category
                var validatedCategory = ValidateCategory(category);

                // Get files from CDN service - using GetFilesAsync instead of GetFilesInCategory
                var files = await _cdnService.GetFilesAsync(validatedCategory, null, pattern);

                if (files == null || !files.Any())
                {
                    return Ok(new { success = true, files = new List<object>(), message = "No files found" });
                }

                // Map to file info objects
                var fileInfoList = files.Select(file => {
                    return new
                    {
                        FileName = file.FileName,
                        Url = file.Url,
                        ContentType = file.ContentType,
                        Size = file.FileSize,
                        Category = validatedCategory,
                        UploadDate = file.UploadDate
                    };
                }).ToList();

                return Ok(new { success = true, files = fileInfoList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        private string ValidateCategory(string category)
        {
            var allowedCategories = new[] { "documents", "images", "hr", "weighbridge", "lab" };
            return allowedCategories.Contains(category.ToLower()) ? category.ToLower() : "documents";
        }

        private string GetContentTypeFromPath(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                _ => "application/octet-stream",
            };
        }

        public class RenameFileRequest
        {
            public string Path { get; set; }
            public string NewName { get; set; }
        }
    }
}