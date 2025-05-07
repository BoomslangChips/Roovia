// Controllers/ExtendedCdnController.cs
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roovia.Interfaces;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Roovia.Models.CDN;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Roovia.Controllers
{
    [ApiController]
    [Route("api/cdn")]
    public class ExtendedCdnController : ControllerBase
    {
        private readonly ICdnService _cdnService;
        private readonly Data.ApplicationDbContext _dbContext;
        private readonly ILogger<ExtendedCdnController> _logger;

        public ExtendedCdnController(
            ICdnService cdnService,
            Data.ApplicationDbContext dbContext,
            ILogger<ExtendedCdnController> logger)
        {
            _cdnService = cdnService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
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
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("folders")]
        public async Task<IActionResult> GetFolders([FromQuery] string category = "documents")
        {
            try
            {
                var folders = await _cdnService.GetFoldersAsync(category);

                return Ok(new
                {
                    success = true,
                    folders = folders.Select(f => new
                    {
                        id = f.Id,
                        name = f.Name,
                        path = f.Path,
                        parentId = f.ParentId,
                        createdDate = f.CreatedDate
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("folder")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Category) || string.IsNullOrEmpty(request.Path))
                {
                    return BadRequest(new { success = false, message = "Category and path are required" });
                }

                // Get category ID
                var category = await _dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == request.Category && c.IsActive);

                if (category == null)
                {
                    // Create default category if not found
                    category = new CdnCategory
                    {
                        Name = request.Category,
                        DisplayName = char.ToUpper(request.Category[0]) + request.Category.Substring(1),
                        AllowedFileTypes = "*",
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System"
                    };

                    _dbContext.Add(category);
                    await _dbContext.SaveChangesAsync();
                }

                // Clean path to prevent directory traversal
                var cleanPath = CleanPath(request.Path);

                // Split path into segments
                var segments = cleanPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                int? parentId = null;
                string currentPath = "";

                // Process each segment, creating folders as needed
                foreach (var segment in segments)
                {
                    currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

                    // Look for existing folder
                    var folder = await _dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == category.Id && f.Path == currentPath);

                    if (folder == null)
                    {
                        // Create new folder
                        folder = new CdnFolder
                        {
                            Name = segment,
                            Path = currentPath,
                            ParentId = parentId,
                            CategoryId = category.Id,
                            CreatedDate = DateTime.Now,
                            CreatedBy = User.Identity?.Name ?? "System",
                            IsActive = true
                        };

                        _dbContext.Add(folder);
                        await _dbContext.SaveChangesAsync();
                    }

                    parentId = folder.Id;
                }

                // Create physical directory if direct access is available
                if (_cdnService.IsDirectAccessAvailable())
                {
                    // Get storage path from the CDN service
                    var fullPath = Path.Combine(_cdnService.GetPhysicalPath(_cdnService.GetCdnUrl("")), request.Category, cleanPath);
                    Directory.CreateDirectory(fullPath);
                }

                return Ok(new
                {
                    success = true,
                    path = cleanPath,
                    message = "Folder created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("folder/rename")]
        public async Task<IActionResult> RenameFolder([FromBody] RenameFolderRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Category) || string.IsNullOrEmpty(request.Path) || string.IsNullOrEmpty(request.NewName))
                {
                    return BadRequest(new { success = false, message = "Category, path, and new name are required" });
                }

                // Get category
                var category = await _dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == request.Category && c.IsActive);

                if (category == null)
                {
                    return BadRequest(new { success = false, message = "Invalid category" });
                }

                // Clean paths
                var cleanPath = CleanPath(request.Path);
                var cleanNewName = SanitizeFileName(request.NewName);

                // Get folder
                var folder = await _dbContext.Set<CdnFolder>()
                    .Include(f => f.Parent)
                    .FirstOrDefaultAsync(f => f.CategoryId == category.Id && f.Path == cleanPath);

                if (folder == null)
                {
                    return NotFound(new { success = false, message = "Folder not found" });
                }

                // Determine new path
                string newPath;
                if (folder.Parent == null)
                {
                    newPath = cleanNewName;
                }
                else
                {
                    var parentPath = folder.Parent.Path;
                    newPath = $"{parentPath}/{cleanNewName}";
                }

                // Check if new path already exists
                var existingFolder = await _dbContext.Set<CdnFolder>()
                    .FirstOrDefaultAsync(f => f.CategoryId == category.Id && f.Path == newPath && f.Id != folder.Id);

                if (existingFolder != null)
                {
                    return BadRequest(new { success = false, message = "A folder with that name already exists" });
                }

                // Old path for updating child paths
                var oldPath = folder.Path;

                // Update folder
                folder.Name = cleanNewName;
                folder.Path = newPath;

                // Update all child folder paths
                var childFolders = await _dbContext.Set<CdnFolder>()
                    .Where(f => f.CategoryId == category.Id && f.Path.StartsWith(oldPath + "/"))
                    .ToListAsync();

                foreach (var childFolder in childFolders)
                {
                    childFolder.Path = childFolder.Path.Replace(oldPath, newPath);
                }

                await _dbContext.SaveChangesAsync();

                // If direct access is available, rename the physical folder
                if (_cdnService.IsDirectAccessAvailable())
                {
                    var basePath = _cdnService.GetPhysicalPath(_cdnService.GetCdnUrl(""));
                    var oldFolderPath = Path.Combine(basePath, request.Category, oldPath);
                    var newFolderPath = Path.Combine(basePath, request.Category, newPath);

                    if (Directory.Exists(oldFolderPath) && !Directory.Exists(newFolderPath))
                    {
                        Directory.Move(oldFolderPath, newFolderPath);
                    }
                }

                // Update file records that reference this folder
                await UpdateFileUrlsForFolder(category.Id, oldPath, newPath);

                return Ok(new
                {
                    success = true,
                    path = newPath,
                    message = "Folder renamed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming folder: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        private async Task UpdateFileUrlsForFolder(int categoryId, string oldPath, string newPath)
        {
            try
            {
                // Get the CDN base URL
                var cdnBaseUrl = _cdnService.GetCdnUrl("").TrimEnd('/');
                var category = await _dbContext.Set<CdnCategory>().FindAsync(categoryId);

                if (category == null)
                    return;

                var categoryName = category.Name;

                // Get all files in the folder
                var files = await _dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.CategoryId == categoryId && f.Url.Contains($"/{categoryName}/{oldPath}/"))
                    .ToListAsync();

                foreach (var file in files)
                {
                    // Update URL
                    file.Url = file.Url.Replace($"/{categoryName}/{oldPath}/", $"/{categoryName}/{newPath}/");

                    // Update file path if we have direct access
                    if (_cdnService.IsDirectAccessAvailable())
                    {
                        var basePath = _cdnService.GetPhysicalPath(_cdnService.GetCdnUrl(""));
                        file.FilePath = file.FilePath.Replace(
                            Path.Combine(basePath, categoryName, oldPath),
                            Path.Combine(basePath, categoryName, newPath));
                    }
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file URLs for folder: {Message}", ex.Message);
            }
        }

        [HttpDelete("folder")]
        public async Task<IActionResult> DeleteFolder([FromQuery] string category, [FromQuery] string path)
        {
            try
            {
                if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(path))
                {
                    return BadRequest(new { success = false, message = "Category and path are required" });
                }

                // Get category
                var categoryObj = await _dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category && c.IsActive);

                if (categoryObj == null)
                {
                    return BadRequest(new { success = false, message = "Invalid category" });
                }

                // Clean path
                var cleanPath = CleanPath(path);

                // Get folder
                var folder = await _dbContext.Set<CdnFolder>()
                    .FirstOrDefaultAsync(f => f.CategoryId == categoryObj.Id && f.Path == cleanPath);

                if (folder == null)
                {
                    return NotFound(new { success = false, message = "Folder not found" });
                }

                // Get all child folders
                var childFolders = await _dbContext.Set<CdnFolder>()
                    .Where(f => f.CategoryId == categoryObj.Id && (f.Path == cleanPath || f.Path.StartsWith(cleanPath + "/")))
                    .ToListAsync();

                // Get all files in these folders
                var folderIds = childFolders.Select(f => f.Id).ToList();
                var files = await _dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.CategoryId == categoryObj.Id && folderIds.Contains(f.FolderId.Value))
                    .ToListAsync();

                // Mark all files as deleted
                foreach (var file in files)
                {
                    file.IsDeleted = true;
                }

                // Mark folders as inactive
                foreach (var childFolder in childFolders)
                {
                    childFolder.IsActive = false;
                }

                await _dbContext.SaveChangesAsync();

                // Delete physical folder if direct access is available
                if (_cdnService.IsDirectAccessAvailable())
                {
                    var basePath = _cdnService.GetPhysicalPath(_cdnService.GetCdnUrl(""));
                    var folderPath = Path.Combine(basePath, category, cleanPath);

                    if (Directory.Exists(folderPath))
                    {
                        Directory.Delete(folderPath, true);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"Folder deleted successfully with {files.Count} files"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder: {Message}", ex.Message);
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
                // Get category
                var categoryObj = await _dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category && c.IsActive);

                if (categoryObj == null)
                {
                    // Try to create default category
                    categoryObj = new CdnCategory
                    {
                        Name = category,
                        DisplayName = char.ToUpper(category[0]) + category.Substring(1),
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System"
                    };

                    _dbContext.Add(categoryObj);
                    await _dbContext.SaveChangesAsync();
                }

                // Get folder if specified
                CdnFolder folderObj = null;
                if (!string.IsNullOrEmpty(folder))
                {
                    folderObj = await _dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryObj.Id && f.Path == folder && f.IsActive);

                    if (folderObj == null)
                    {
                        // Folder doesn't exist, return empty result
                        return Ok(new
                        {
                            success = true,
                            files = new List<object>(),
                            folders = new List<object>(),
                            message = "Folder not found"
                        });
                    }
                }

                // Get child folders of current folder
                var childFolders = await _dbContext.Set<CdnFolder>()
                    .Where(f => f.CategoryId == categoryObj.Id &&
                           f.IsActive &&
                           (folderObj == null ? f.ParentId == null : f.ParentId == folderObj.Id))
                    .ToListAsync();

                // Get files in current folder
                var query = _dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.CategoryId == categoryObj.Id && !f.IsDeleted);

                if (folderObj == null)
                {
                    // Root folder
                    query = query.Where(f => f.FolderId == null);
                }
                else
                {
                    // Specific folder
                    query = query.Where(f => f.FolderId == folderObj.Id);
                }

                // Apply search if specified
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f => f.FileName.Contains(search));
                }

                var files = await query.ToListAsync();

                // Map to file info objects
                var fileInfoList = files.Select(f => new
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    Url = f.Url,
                    ContentType = f.ContentType,
                    Size = f.FileSize,
                    CategoryId = f.CategoryId,
                    FolderId = f.FolderId,
                    UploadDate = f.UploadDate
                }).ToList();

                // Map folders
                var folderList = childFolders.Select(f => new
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    ModifiedDate = f.CreatedDate
                }).ToList();

                return Ok(new
                {
                    success = true,
                    files = fileInfoList,
                    folders = folderList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("upload")]
        [RequestSizeLimit(209715200)] // 200MB in bytes
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200MB in bytes
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string category = "documents", [FromForm] string folder = "")
        {
            // Check API key for external requests
            if (!HttpContext.Request.Host.Host.Contains("roovia.co.za"))
            {
                if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
                    string.IsNullOrWhiteSpace(apiKey) ||
                    apiKey != _cdnService.GetApiKey())
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file was uploaded" });

            try
            {
                // Check file size (get actual limit from database)
                var config = await _dbContext.Set<CdnConfiguration>()
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                long maxFileSize = (config?.MaxFileSizeMB ?? 200) * 1024 * 1024; // Convert to bytes

                if (file.Length > maxFileSize)
                    return BadRequest(new { success = false, message = $"File size exceeds the {maxFileSize / (1024 * 1024)}MB limit" });

                // Get category
                var categoryObj = await _dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category && c.IsActive);

                if (categoryObj == null)
                {
                    // Create category if it doesn't exist
                    categoryObj = new CdnCategory
                    {
                        Name = category,
                        DisplayName = char.ToUpper(category[0]) + category.Substring(1),
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System"
                    };

                    _dbContext.Add(categoryObj);
                    await _dbContext.SaveChangesAsync();
                }

                // Validate file type
                var allowedTypes = string.IsNullOrEmpty(categoryObj.AllowedFileTypes)
                    ? (config?.AllowedFileTypes ?? "*").Split(',')
                    : categoryObj.AllowedFileTypes.Split(',');

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (allowedTypes[0] != "*" && !allowedTypes.Contains(extension))
                    return BadRequest(new { success = false, message = $"File type {extension} not allowed for category {category}" });

                // Upload file
                var fileUrl = await _cdnService.UploadFileAsync(file, category, folder);

                // Return success with the file URL
                return Ok(new
                {
                    success = true,
                    url = fileUrl,
                    fileName = file.FileName,
                    contentType = file.ContentType,
                    size = file.Length,
                    category = category,
                    folder = folder
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during upload: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("move")]
        public async Task<IActionResult> MoveFiles([FromBody] MoveFilesRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Category) || request.Files == null || !request.Files.Any())
                {
                    return BadRequest(new { success = false, message = "Category and files are required" });
                }

                // Get category
                var category = await _dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == request.Category && c.IsActive);

                if (category == null)
                {
                    return BadRequest(new { success = false, message = "Invalid category" });
                }

                // Get target folder
                CdnFolder targetFolder = null;
                if (!string.IsNullOrEmpty(request.TargetFolder))
                {
                    targetFolder = await _dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == category.Id && f.Path == request.TargetFolder && f.IsActive);

                    if (targetFolder == null)
                    {
                        // Create target folder if it doesn't exist
                        var segments = request.TargetFolder.Split('/', StringSplitOptions.RemoveEmptyEntries);

                        int? parentId = null;
                        string currentPath = "";

                        foreach (var segment in segments)
                        {
                            currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

                            var folder = await _dbContext.Set<CdnFolder>()
                                .FirstOrDefaultAsync(f => f.CategoryId == category.Id && f.Path == currentPath);

                            if (folder == null)
                            {
                                folder = new CdnFolder
                                {
                                    Name = segment,
                                    Path = currentPath,
                                    ParentId = parentId,
                                    CategoryId = category.Id,
                                    CreatedDate = DateTime.Now,
                                    CreatedBy = User.Identity?.Name ?? "System",
                                    IsActive = true
                                };

                                _dbContext.Add(folder);
                                await _dbContext.SaveChangesAsync();
                            }

                            parentId = folder.Id;
                            targetFolder = folder;
                        }

                        // Create physical directory if direct access available
                        if (_cdnService.IsDirectAccessAvailable())
                        {
                            var basePath = _cdnService.GetPhysicalPath(_cdnService.GetCdnUrl(""));
                            var dirPath = Path.Combine(basePath, request.Category, request.TargetFolder);
                            Directory.CreateDirectory(dirPath);
                        }
                    }
                }

                int movedCount = 0;
                var errors = new List<string>();

                // Process each file
                foreach (var fileUrl in request.Files)
                {
                    try
                    {
                        // Get file metadata
                        var file = await _dbContext.Set<CdnFileMetadata>()
                            .FirstOrDefaultAsync(f => f.Url == fileUrl && !f.IsDeleted);

                        if (file == null)
                        {
                            errors.Add($"File not found: {fileUrl}");
                            continue;
                        }

                        // Extract original filename
                        var fileName = file.FileName;

                        // Move physical file if direct access available
                        if (_cdnService.IsDirectAccessAvailable())
                        {
                            var sourcePath = file.FilePath;
                            
                            if (System.IO.File.Exists(sourcePath))
                            {
                                var basePath = _cdnService.GetPhysicalPath(_cdnService.GetCdnUrl(""));
                                var targetPath = targetFolder == null
                                    ? Path.Combine(basePath, request.Category, fileName)
                                    : Path.Combine(basePath, request.Category, targetFolder.Path, fileName);

                                // Create directory if it doesn't exist
                                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                                // Check if target file already exists
                                if (System.IO.File.Exists(targetPath))
                                {
                                    // Rename to avoid conflict
                                    var newFileName = GenerateUniqueFileName(fileName);
                                    targetPath = Path.Combine(Path.GetDirectoryName(targetPath), newFileName);
                                    fileName = newFileName;
                                }

                                // Move the file
                                System.IO.File.Move(sourcePath, targetPath);

                                // Update file metadata
                                file.FilePath = targetPath;
                            }
                        }

                        // Update file metadata
                        file.FolderId = targetFolder?.Id;

                        // Update URL
                        var cdnBaseUrl = _cdnService.GetCdnUrl("").TrimEnd('/');
                        file.Url = targetFolder == null
                            ? $"{cdnBaseUrl}/{request.Category}/{fileName}"
                            : $"{cdnBaseUrl}/{request.Category}/{targetFolder.Path}/{fileName}";

                        movedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error moving {fileUrl}: {ex.Message}");
                    }
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    count = movedCount,
                    errors = errors,
                    message = $"Moved {movedCount} files to {request.TargetFolder ?? "root folder"}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving files: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("rename")]
        public async Task<IActionResult> RenameFile([FromBody] RenameFileRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Path) || string.IsNullOrEmpty(request.NewName))
                    return BadRequest(new { success = false, message = "Path and new name are required" });

                // Get file metadata
                var file = await _dbContext.Set<CdnFileMetadata>()
                    .Include(f => f.Category)
                    .FirstOrDefaultAsync(f => f.Url == request.Path && !f.IsDeleted);

                if (file == null)
                    return NotFound(new { success = false, message = "File not found" });

                // Clean new name
                var safeNewName = SanitizeFileName(request.NewName);

                // Keep original extension
                var originalExtension = Path.GetExtension(file.FileName);
                var newFileName = $"{safeNewName}{originalExtension}";

                // Check if direct access is available
                if (_cdnService.IsDirectAccessAvailable())
                {
                    // Get physical paths
                    var oldPhysicalPath = file.FilePath;
                    var directoryPath = Path.GetDirectoryName(oldPhysicalPath);
                    var newPhysicalPath = Path.Combine(directoryPath, newFileName);

                    // Check if file exists
                    if (!System.IO.File.Exists(oldPhysicalPath))
                        return NotFound(new { success = false, message = "File not found" });

                    // Check if new name already exists
                    if (System.IO.File.Exists(newPhysicalPath))
                        return BadRequest(new { success = false, message = "A file with that name already exists" });

                    // Rename the file
                    System.IO.File.Move(oldPhysicalPath, newPhysicalPath);

                    // Update file path
                    file.FilePath = newPhysicalPath;
                }

                // Update filename
                file.FileName = newFileName;

                // Update URL
                var cdnBaseUrl = _cdnService.GetCdnUrl("").TrimEnd('/');
                var categoryName = file.Category.Name;

                string newUrl;
                if (file.FolderId == null)
                {
                    // Root folder
                    newUrl = $"{cdnBaseUrl}/{categoryName}/{newFileName}";
                }
                else
                {
                    // In a folder - get folder path
                    var folder = await _dbContext.Set<CdnFolder>().FindAsync(file.FolderId);
                    if (folder != null)
                    {
                        newUrl = $"{cdnBaseUrl}/{categoryName}/{folder.Path}/{newFileName}";
                    }
                    else
                    {
                        // Fallback if folder not found
                        newUrl = $"{cdnBaseUrl}/{categoryName}/{newFileName}";
                    }
                }

                file.Url = newUrl;

                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, url = newUrl, message = "File renamed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming file: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("maintenance/clean")]
        public async Task<IActionResult> CleanOrphanedFiles()
        {
            try
            {
                // Get deleted files from database
                var deletedFiles = await _dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.IsDeleted)
                    .ToListAsync();

                int cleanedCount = 0;
                long bytesFreed = 0;

                foreach (var file in deletedFiles)
                {
                    try
                    {
                        // Check if file exists
                        if (_cdnService.IsDirectAccessAvailable() && !string.IsNullOrEmpty(file.FilePath) && System.IO.File.Exists(file.FilePath))
                        {
                            // Get file size
                            var fileInfo = new System.IO.FileInfo(file.FilePath);
                            bytesFreed += fileInfo.Length;

                            // Delete file
                            System.IO.File.Delete(file.FilePath);
                            cleanedCount++;
                        }

                        // Remove the metadata
                        _dbContext.Remove(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning file {FilePath}: {Message}", file.FilePath, ex.Message);
                    }
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    count = cleanedCount,
                    bytesFreed = bytesFreed,
                    message = $"Cleaned {cleanedCount} orphaned files, freeing {FormatFileSize(bytesFreed)}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning orphaned files: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("maintenance/clearcache")]
        public IActionResult ClearCache()
        {
            try
            {
                // Clear file path cache
                var cacheType = typeof(Roovia.Services.CdnService);
                var cacheField = cacheType.GetField("_filePathCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var expiryField = cacheType.GetField("_cacheExpiry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (cacheField != null && cacheField.GetValue(null) is ConcurrentDictionary<string, string> cache)
                {
                    cache.Clear();
                }

                if (expiryField != null && expiryField.GetValue(null) is ConcurrentDictionary<string, DateTime> expiry)
                {
                    expiry.Clear();
                }

                return Ok(new
                {
                    success = true,
                    message = "CDN cache cleared successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("view")]
        public async Task<IActionResult> ViewFile([FromQuery] string path)
        {
            // Check for API key in header first
            if (!HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                apiKey != _cdnService.GetApiKey())
            {
                // Check if query parameter has the API key
                if (!Request.Query.TryGetValue("key", out var queryApiKey) ||
                    string.IsNullOrWhiteSpace(queryApiKey) ||
                    queryApiKey != _cdnService.GetApiKey())
                {
                    return Unauthorized(new { success = false, message = "Invalid API key" });
                }
            }

            if (string.IsNullOrEmpty(path))
                return BadRequest(new { success = false, message = "No file path provided" });

            try
            {
                // Get file metadata
                var file = await _dbContext.Set<CdnFileMetadata>()
                    .FirstOrDefaultAsync(f => f.Url == path && !f.IsDeleted);

                if (file == null)
                {
                    // Try direct file path if metadata not found
                    if (_cdnService.IsDirectAccessAvailable())
                    {
                        var physicalPath = _cdnService.GetPhysicalPath(path);
                        if (string.IsNullOrEmpty(physicalPath) || !System.IO.File.Exists(physicalPath))
                        {
                            return NotFound(new { success = false, message = "File not found" });
                        }

                        // Update access count
                        file = new CdnFileMetadata
                        {
                            FilePath = physicalPath,
                            FileName = Path.GetFileName(physicalPath),
                            ContentType = GetContentTypeFromPath(physicalPath),
                            FileSize = new FileInfo(physicalPath).Length,
                            Url = path,
                            UploadDate = DateTime.Now,
                            AccessCount = 1,
                            LastAccessDate = DateTime.Now
                        };

                        _dbContext.Add(file);
                        await _dbContext.SaveChangesAsync();

                        // Set cache headers for better performance
                        Response.Headers.Add("Cache-Control", "public, max-age=604800"); // 7 days
                        Response.Headers.Add("Expires", DateTime.UtcNow.AddDays(7).ToString("R"));

                        // Return the file with streaming enabled for large files
                        return PhysicalFile(physicalPath, file.ContentType, enableRangeProcessing: true);
                    }

                    return NotFound(new { success = false, message = "File not found" });
                }

                // Update access statistics
                file.AccessCount++;
                file.LastAccessDate = DateTime.Now;
                await _dbContext.SaveChangesAsync();

                if (_cdnService.IsDirectAccessAvailable())
                {
                    // Check if file exists
                    if (string.IsNullOrEmpty(file.FilePath) || !System.IO.File.Exists(file.FilePath))
                    {
                        return NotFound(new { success = false, message = "File not found" });
                    }

                    // Set cache headers for better performance
                    Response.Headers.Add("Cache-Control", "public, max-age=604800"); // 7 days
                    Response.Headers.Add("Expires", DateTime.UtcNow.AddDays(7).ToString("R"));

                    // Return the file with streaming enabled for large files
                    return PhysicalFile(file.FilePath, file.ContentType, enableRangeProcessing: true);
                }
                else
                {
                    // Redirect to the CDN URL
                    return Redirect(file.Url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing file: {Path}", path);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        #region Helper Methods

        private string CleanPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";

            // Remove any potentially dangerous characters or patterns
            path = path.Replace("..", "").Replace("\\", "/");
            path = path.TrimStart('/').TrimEnd('/');

            // Remove any redundant separators
            while (path.Contains("//"))
                path = path.Replace("//", "/");

            return path;
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '-');
            }

            return fileName;
        }

        private string GenerateUniqueFileName(string fileName)
        {
            // Remove any potentially dangerous characters from the filename
            var safeName = Path.GetFileNameWithoutExtension(fileName)
                .Replace(" ", "-")
                .Replace("_", "-");

            var extension = Path.GetExtension(fileName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = Guid.NewGuid().ToString().Substring(0, 8);

            return $"{safeName}_{timestamp}_{random}{extension}";
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
                ".zip" => "application/zip",
                _ => "application/octet-stream",
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        #endregion

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