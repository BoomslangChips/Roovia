using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roovia.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using Roovia.Models.CDN;
using Microsoft.Extensions.Configuration;

namespace Roovia.Controllers
{
    [ApiController]
    [Route("api/cdn")]
    public class CDNController : ControllerBase
    {
        private readonly ICdnService _cdnService;
        private readonly ILogger<CDNController> _logger;
        private readonly IConfiguration _configuration;

        public CDNController(
            ICdnService cdnService,
            ILogger<CDNController> logger,
            IConfiguration configuration)
        {
            _cdnService = cdnService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            // Simple ping endpoint that requires no authorization
            _logger.LogInformation("CDN Ping request received at {Time}", DateTime.Now);

            return Ok(new
            {
                success = true,
                controller = "CDNController",
                timestamp = DateTime.Now,
                apiVersion = "1.0"
            });
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            // Test endpoint to verify CDN service is working
            try
            {
                var path = _cdnService.GetPhysicalPath("");
                var exists = !string.IsNullOrEmpty(path) && Directory.Exists(path);

                // Try to create a test file
                var canWrite = false;
                string errorMessage = null;

                if (exists)
                {
                    try
                    {
                        var testFile = Path.Combine(path, $"test_{Guid.NewGuid()}.txt");
                        System.IO.File.WriteAllText(testFile, "Test write access");
                        canWrite = System.IO.File.Exists(testFile);
                        if (canWrite)
                        {
                            System.IO.File.Delete(testFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.Message;
                    }
                }

                return Ok(new
                {
                    success = true,
                    path,
                    exists,
                    canWrite,
                    error = errorMessage,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CDN test endpoint");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Set very high size limits for Linux
        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadFile()
        {
            _logger.LogInformation("Upload request received at {Time}", DateTime.Now);

            try
            {
                // Check API key
                var apiKey = _cdnService.GetApiKey();
                bool hasValidKey = false;

                // Check header first
                if (HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var headerApiKey))
                {
                    hasValidKey = headerApiKey == apiKey;
                    _logger.LogInformation("API key from header present: {HasKey}", !string.IsNullOrEmpty(headerApiKey));
                }

                // Check query string next
                if (!hasValidKey && Request.Query.TryGetValue("key", out var queryApiKey))
                {
                    hasValidKey = queryApiKey == apiKey;
                    _logger.LogInformation("API key from query present: {HasKey}", !string.IsNullOrEmpty(queryApiKey));
                }

                // Finally check form
                if (!hasValidKey && Request.HasFormContentType && Request.Form.TryGetValue("key", out var formApiKey))
                {
                    hasValidKey = formApiKey == apiKey;
                    _logger.LogInformation("API key from form present: {HasKey}", !string.IsNullOrEmpty(formApiKey));
                }

                if (!hasValidKey)
                {
                    _logger.LogWarning("Invalid or missing API key in request");
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                // Log form data details
                _logger.LogInformation("Content type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Form count: {Count}", Request.Form.Files.Count);

                // Get the file directly from request
                IFormFile file = Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;

                // Get other form parameters
                string category = Request.Form.TryGetValue("category", out var categoryValue) ?
                    categoryValue.ToString() : "documents";

                string folder = Request.Form.TryGetValue("folder", out var folderValue) ?
                    folderValue.ToString() : "";

                // Validate file
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file was uploaded or file is empty");
                    return BadRequest(new { success = false, message = "No file was uploaded or file is empty" });
                }

                _logger.LogInformation("File received: {FileName}, Size: {Size} bytes, ContentType: {ContentType}",
                    file.FileName, file.Length, file.ContentType);

                // Get storage directory path and check if it exists
                string cdnStoragePath = _cdnService.GetPhysicalPath("");
                if (string.IsNullOrEmpty(cdnStoragePath) || !Directory.Exists(cdnStoragePath))
                {
                    _logger.LogError("CDN storage directory does not exist: {Path}", cdnStoragePath);
                    return StatusCode(500, new { success = false, message = "CDN storage directory not configured or does not exist" });
                }

                _logger.LogInformation("Using CDN storage directory: {Path}", cdnStoragePath);

                // Upload file using a memory stream to avoid file system issues
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Use CDN service to upload
                    string fileUrl = await _cdnService.UploadFileAsync(
                        memoryStream,
                        file.FileName,
                        file.ContentType,
                        category,
                        folder);

                    _logger.LogInformation("File uploaded successfully: {Url}", fileUrl);

                    // Return success response
                    return Ok(new
                    {
                        success = true,
                        url = fileUrl,
                        fileName = file.FileName,
                        contentType = file.ContentType,
                        size = file.Length,
                        category
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {Error}", ex.Message);

                // Detailed error response for debugging
                return StatusCode(500, new
                {
                    success = false,
                    message = "Upload failed: " + ex.Message,
                    exceptionType = ex.GetType().Name,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        // Alternative upload endpoint using direct content
        [HttpPost("upload-direct")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFileDirect()
        {
            try
            {
                // Check API key
                var apiKey = _cdnService.GetApiKey();
                bool hasValidKey = false;

                // Check header first
                if (HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var headerApiKey))
                {
                    hasValidKey = headerApiKey == apiKey;
                }

                // Check query string next
                if (!hasValidKey && Request.Query.TryGetValue("key", out var queryApiKey))
                {
                    hasValidKey = queryApiKey == apiKey;
                }

                if (!hasValidKey)
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }

                // Try to get filename from content disposition header
                string fileName = "uploaded_file";
                string contentType = "application/octet-stream";

                if (Request.Headers.TryGetValue("Content-Disposition", out var contentDisposition))
                {
                    var disposition = ContentDispositionHeaderValue.Parse(contentDisposition);
                    fileName = disposition.FileNameStar ?? disposition.FileName ?? "uploaded_file";
                    fileName = fileName.Trim('"');
                }

                if (Request.Headers.TryGetValue("Content-Type", out var contentTypeHeader))
                {
                    contentType = contentTypeHeader;
                }

                // Get category and folder from query
                string category = Request.Query.TryGetValue("category", out var categoryValue) ?
                    categoryValue.ToString() : "documents";

                string folder = Request.Query.TryGetValue("folder", out var folderValue) ?
                    folderValue.ToString() : "";

                // Read request body directly
                using (var memoryStream = new MemoryStream())
                {
                    await Request.Body.CopyToAsync(memoryStream);

                    if (memoryStream.Length == 0)
                    {
                        return BadRequest(new { success = false, message = "No file content provided" });
                    }

                    memoryStream.Position = 0;

                    // Use CDN service to upload
                    string fileUrl = await _cdnService.UploadFileAsync(
                        memoryStream,
                        fileName,
                        contentType,
                        category,
                        folder);

                    // Return success response
                    return Ok(new
                    {
                        success = true,
                        url = fileUrl,
                        fileName = fileName,
                        contentType = contentType,
                        size = memoryStream.Length,
                        category
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct upload: {Error}", ex.Message);
                return StatusCode(500, new { success = false, message = "Upload failed: " + ex.Message });
            }
        }

        [HttpGet("test-upload")]
        public ContentResult TestUploadForm()
        {
            // A test form that posts directly to the server without any JavaScript
            string html = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>CDN Upload Test</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 20px; }
                    .container { max-width: 600px; margin: 0 auto; }
                    .form-group { margin-bottom: 15px; }
                    label { display: block; margin-bottom: 5px; }
                    input, button { padding: 8px; }
                    button { background-color: #4CAF50; color: white; border: none; cursor: pointer; }
                    .result { margin-top: 20px; padding: 10px; border: 1px solid #ddd; display: none; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <h1>CDN Upload Test</h1>
                    
                    <form action='/api/cdn/upload' method='post' enctype='multipart/form-data'>
                        <div class='form-group'>
                            <label for='file'>Select File:</label>
                            <input type='file' id='file' name='file' required>
                        </div>
                        
                        <div class='form-group'>
                            <label for='category'>Category:</label>
                            <input type='text' id='category' name='category' value='test-uploads'>
                        </div>
                        
                        <div class='form-group'>
                            <label for='folder'>Folder (optional):</label>
                            <input type='text' id='folder' name='folder' value=''>
                        </div>
                        
                        <div class='form-group'>
                            <input type='hidden' name='key' value='" + _cdnService.GetApiKey() + @"'>
                            <button type='submit'>Upload</button>
                        </div>
                    </form>
                    
                    <div class='result' id='result'></div>
                </div>
            </body>
            </html>";

            return Content(html, "text/html");
        }

        // NEW ENDPOINTS BELOW

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                // Validate API key
                if (!IsApiKeyValid())
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                var categories = await _cdnService.GetCategoriesAsync();

                return Ok(new
                {
                    success = true,
                    categories = categories.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        displayName = c.DisplayName,
                        allowedFileTypes = c.AllowedFileTypes
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("folders")]
        public async Task<IActionResult> GetFolders(string category = "documents")
        {
            try
            {
                // Validate API key
                if (!IsApiKeyValid())
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                var folders = await _cdnService.GetFoldersAsync(category);

                return Ok(new
                {
                    success = true,
                    category,
                    folders = folders.Select(f => new
                    {
                        id = f.Id,
                        name = f.Name,
                        path = f.Path,
                        parentId = f.ParentId
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders for category {Category}: {Error}", category, ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetFiles(string category = "documents", string folder = "", string search = "")
        {
            try
            {
                // Validate API key
                if (!IsApiKeyValid())
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                var files = await _cdnService.GetFilesAsync(category, folder, search);

                return Ok(new
                {
                    success = true,
                    category,
                    folder,
                    search = !string.IsNullOrEmpty(search) ? search : null,
                    files = files.Select(f => new
                    {
                        id = f.Id,
                        fileName = f.FileName,
                        url = f.Url,
                        contentType = f.ContentType,
                        fileSize = f.FileSize,
                        uploadDate = f.UploadDate,
                        uploadedBy = f.UploadedBy
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for category {Category}, folder {Folder}: {Error}",
                    category, folder, ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("file-details")]
        public async Task<IActionResult> GetFileDetails(string path)
        {
            try
            {
                // Validate API key
                if (!IsApiKeyValid())
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                if (string.IsNullOrEmpty(path))
                {
                    return BadRequest(new { success = false, message = "File path is required" });
                }

                // Get physical path
                string physicalPath = _cdnService.GetPhysicalPath(path);

                if (string.IsNullOrEmpty(physicalPath) || !System.IO.File.Exists(physicalPath))
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                // Get file info
                var fileInfo = new FileInfo(physicalPath);

                // Try to find file metadata in database
                var metadata = (await _cdnService.GetFilesAsync())
                    .FirstOrDefault(f => f.Url == path || f.FilePath == physicalPath);

                return Ok(new
                {
                    success = true,
                    fileName = fileInfo.Name,
                    path = path,
                    physicalPath = physicalPath,
                    size = fileInfo.Length,
                    contentType = metadata?.ContentType ?? GetContentTypeFromExtension(fileInfo.Extension),
                    created = fileInfo.CreationTime,
                    modified = fileInfo.LastWriteTime,
                    category = metadata?.CategoryId.HasValue == true ?
                        (await _cdnService.GetCategoriesAsync())
                            .FirstOrDefault(c => c.Id == metadata.CategoryId)?.Name : null,
                    uploadDate = metadata?.UploadDate,
                    uploadedBy = metadata?.UploadedBy
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file details for {Path}: {Error}", path, ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("rename")]
        public async Task<IActionResult> RenameFile([FromBody] RenameFileRequest request)
        {
            try
            {
                // Validate API key
                if (!IsApiKeyValid())
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                if (request == null || string.IsNullOrEmpty(request.Path) || string.IsNullOrEmpty(request.NewName))
                {
                    return BadRequest(new { success = false, message = "Path and new name are required" });
                }

                // Get physical path
                string physicalPath = _cdnService.GetPhysicalPath(request.Path);

                if (string.IsNullOrEmpty(physicalPath) || !System.IO.File.Exists(physicalPath))
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                // Get file info
                var fileInfo = new FileInfo(physicalPath);

                // Create new file name with the same extension
                string newFileName = $"{request.NewName}{fileInfo.Extension}";
                string newPath = Path.Combine(fileInfo.DirectoryName, newFileName);

                // Check if the new file already exists
                if (System.IO.File.Exists(newPath))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"A file with the name '{newFileName}' already exists in this location"
                    });
                }

                // Rename the file
                System.IO.File.Move(physicalPath, newPath);

                // Get the full URL for the new file
                string cdnBaseUrl = _configuration["CDN:BaseUrl"] ?? "https://cdn.roovia.co.za";
                string relativePath = request.Path.Replace(cdnBaseUrl, "").TrimStart('/');
                string directory = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
                string newRelativePath = string.IsNullOrEmpty(directory) ?
                    newFileName : $"{directory}/{newFileName}";
                string newUrl = $"{cdnBaseUrl.TrimEnd('/')}/{newRelativePath}";

                // Try to update metadata in database
                try
                {
                    var metadata = (await _cdnService.GetFilesAsync())
                        .FirstOrDefault(f => f.Url == request.Path || f.FilePath == physicalPath);

                    if (metadata != null)
                    {
                        // We need to update this in the database, but since the CdnService doesn't have a direct
                        // method for this, we'll note it in the response
                        _logger.LogInformation("File renamed from {OldPath} to {NewPath}. Metadata should be updated.",
                            physicalPath, newPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error updating metadata after rename");
                    // Continue with the rename even if metadata update fails
                }

                return Ok(new
                {
                    success = true,
                    oldPath = request.Path,
                    newPath = newUrl,
                    fileName = newFileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming file {Path} to {NewName}: {Error}",
                    request?.Path, request?.NewName, ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("create-folder")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
        {
            try
            {
                // Validate API key
                if (!IsApiKeyValid())
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                if (request == null || string.IsNullOrEmpty(request.Category) || string.IsNullOrEmpty(request.FolderName))
                {
                    return BadRequest(new { success = false, message = "Category and folder name are required" });
                }

                string category = request.Category;
                string parentFolder = request.ParentFolder ?? "";
                string folderName = request.FolderName;

                // Clean folder names for security
                folderName = folderName.Replace("..", "").Replace("/", "").Replace("\\", "");

                // Create the folder path
                string relativeFolderPath = string.IsNullOrEmpty(parentFolder) ?
                    folderName : $"{parentFolder}/{folderName}";

                // Get physical path
                string cdnStoragePath = _cdnService.GetPhysicalPath("");
                string categoryPath = Path.Combine(cdnStoragePath, category);
                string folderPath = Path.Combine(categoryPath, relativeFolderPath.Replace('/', Path.DirectorySeparatorChar));

                // Check if folder already exists
                if (Directory.Exists(folderPath))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"A folder named '{folderName}' already exists in this location"
                    });
                }

                // Create the folder
                Directory.CreateDirectory(folderPath);

                // Update database with new folder if needed (future enhancement)

                return Ok(new
                {
                    success = true,
                    category = category,
                    path = relativeFolderPath,
                    folderName = folderName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder {Folder} in {Category}: {Error}",
                    request?.FolderName, request?.Category, ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile(string path)
        {
            try
            {
                // Validate API key
                if (!IsApiKeyValid())
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                if (string.IsNullOrEmpty(path))
                {
                    return BadRequest(new { success = false, message = "File path is required" });
                }

                // Use the service to delete the file
                bool result = await _cdnService.DeleteFileAsync(path);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "File deleted successfully",
                        path = path
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "File not found or could not be deleted",
                        path = path
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {Path}: {Error}", path, ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("delete-folder")]
        public async Task<IActionResult> DeleteFolder(string category, string path)
        {
            try
            {
                // Validate API key
                if (!IsApiKeyValid())
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(path))
                {
                    return BadRequest(new { success = false, message = "Category and folder path are required" });
                }

                // Get physical path
                string cdnStoragePath = _cdnService.GetPhysicalPath("");
                string fullPath = Path.Combine(cdnStoragePath, category, path.Replace('/', Path.DirectorySeparatorChar));

                if (!Directory.Exists(fullPath))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Folder not found",
                        category = category,
                        path = path
                    });
                }

                // Check if directory is empty
                bool isEmpty = !Directory.EnumerateFileSystemEntries(fullPath).Any();

                if (!isEmpty && !Request.Query.ContainsKey("force"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Folder is not empty. Use force=true to delete non-empty folders",
                        category = category,
                        path = path
                    });
                }

                // Delete the folder and all its contents
                Directory.Delete(fullPath, recursive: true);

                // Update database if needed (future enhancement)

                return Ok(new
                {
                    success = true,
                    message = "Folder deleted successfully",
                    category = category,
                    path = path
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder {Path} in {Category}: {Error}",
                    path, category, ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("serve/{category}/{**path}")]
        public async Task<IActionResult> ServeFile(string category, string path)
        {
            try
            {
                // Allow public access if configured, otherwise check API key
                bool requireKey = true;

                try
                {
                    // Try to get configuration
                    var publicCategories = _configuration
                        .GetSection("CDN:PublicCategories")
                        ?.Get<string[]>() ?? new string[0];

                    // If category is in the public list, don't require a key
                    requireKey = !publicCategories.Contains(category, StringComparer.OrdinalIgnoreCase);
                }
                catch
                {
                    // Default to requiring a key if configuration fails
                    requireKey = true;
                }

                if (requireKey && !IsApiKeyValid())
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing API key" });
                }

                if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(path))
                {
                    return BadRequest(new { success = false, message = "Category and file path are required" });
                }

                // Construct the full URL
                string cdnBaseUrl = _configuration["CDN:BaseUrl"] ?? "https://cdn.roovia.co.za";
                string fullUrl = $"{cdnBaseUrl.TrimEnd('/')}/{category}/{path}";

                // Get physical path
                string physicalPath = _cdnService.GetPhysicalPath(fullUrl);

                if (string.IsNullOrEmpty(physicalPath) || !System.IO.File.Exists(physicalPath))
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                // Get file info
                var fileInfo = new FileInfo(physicalPath);

                // Determine content type
                string contentType = GetContentTypeFromExtension(fileInfo.Extension);

                // Try to find more accurate content type from metadata
                try
                {
                    var metadata = (await _cdnService.GetFilesAsync())
                        .FirstOrDefault(f => f.Url == fullUrl || f.FilePath == physicalPath);

                    if (metadata != null && !string.IsNullOrEmpty(metadata.ContentType))
                    {
                        contentType = metadata.ContentType;
                    }
                }
                catch
                {
                    // Use the determined content type if metadata lookup fails
                }

                // Log access
                _logger.LogInformation("Serving file: {Path}, Size: {Size}, Type: {Type}",
                    physicalPath, fileInfo.Length, contentType);

                // Set correct caching headers
                Response.Headers.Add("Cache-Control", "public, max-age=3600"); // 1 hour cache

                // Return the file
                return PhysicalFile(physicalPath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving file {Category}/{Path}: {Error}",
                    category, path, ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #region Helper Methods and Classes

        private bool IsApiKeyValid()
        {
            var expectedApiKey = _cdnService.GetApiKey();
            bool hasValidKey = false;

            // Check header first (preferred method)
            if (HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var headerApiKey))
            {
                hasValidKey = headerApiKey == expectedApiKey;
            }

            // Check query string next
            if (!hasValidKey && Request.Query.TryGetValue("key", out var queryApiKey))
            {
                hasValidKey = queryApiKey == expectedApiKey;
            }

            return hasValidKey;
        }

        private string GetContentTypeFromExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";

            // Remove the dot if present
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            // Map common extensions to MIME types
            return extension.ToLower() switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "webp" => "image/webp",
                "svg" => "image/svg+xml",
                "pdf" => "application/pdf",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xls" => "application/vnd.ms-excel",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ppt" => "application/vnd.ms-powerpoint",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "txt" => "text/plain",
                "html" or "htm" => "text/html",
                "css" => "text/css",
                "js" => "application/javascript",
                "json" => "application/json",
                "xml" => "application/xml",
                "zip" => "application/zip",
                "rar" => "application/vnd.rar",
                "7z" => "application/x-7z-compressed",
                "mp3" => "audio/mpeg",
                "mp4" => "video/mp4",
                "avi" => "video/x-msvideo",
                "mov" => "video/quicktime",
                "csv" => "text/csv",
                _ => "application/octet-stream" // Default content type for unknown extensions
            };
        }

        public class RenameFileRequest
        {
            public string Path { get; set; }
            public string NewName { get; set; }
        }

        public class CreateFolderRequest
        {
            public string Category { get; set; }
            public string ParentFolder { get; set; }
            public string FolderName { get; set; }
        }

        #endregion
    }
}