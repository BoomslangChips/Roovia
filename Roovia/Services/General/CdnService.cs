// CdnService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.ProjectCdnConfigModels;
using System.Security.Cryptography;

namespace Roovia.Services.General
{
    public class CdnService : ICdnService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CdnService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly string _storagePath;
        private readonly string _baseUrl;

        // Cache settings
        private const string CATEGORIES_CACHE_KEY = "CdnCategories";

        private const string CONFIG_CACHE_KEY = "CdnConfiguration";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

        // Buffer size for file operations
        private const int BufferSize = 81920; // 80KB

        public CdnService(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<CdnService> logger,
            IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _memoryCache = memoryCache;

            // Configure storage path and base URL from config
            _storagePath = configuration["CDN:StoragePath"] ?? "/var/www/cdn";
            _baseUrl = configuration["CDN:BaseUrl"] ?? "https://portal.roovia.co.za/cdn";

            // Ensure root storage path exists
            EnsureDirectoryExists(_storagePath);

            // Initialize configuration if needed
            Task.Run(async () => await InitializeConfigurationAsync());

            _logger.LogInformation("CDN Service initialized with storage path: {Path}, base URL: {BaseUrl}", _storagePath, _baseUrl);
        }

        private async Task InitializeConfigurationAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check if configuration exists
                if (!await dbContext.Set<CdnConfiguration>().AnyAsync())
                {
                    var config = new CdnConfiguration
                    {
                        BaseUrl = _baseUrl,
                        StoragePath = _storagePath,
                        MaxFileSizeMB = 200,
                        AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.mp4,.mp3,.zip",
                        EnableCaching = true,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        ModifiedBy = "System",
                        IsActive = true
                    };

                    dbContext.Add(config);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Created default CDN configuration");
                }

                // Create default category if none exist
                if (!await dbContext.Set<CdnCategory>().AnyAsync())
                {
                    var defaultCategory = new CdnCategory
                    {
                        Name = "documents",
                        DisplayName = "Documents",
                        AllowedFileTypes = "*",
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System"
                    };

                    dbContext.Add(defaultCategory);
                    await dbContext.SaveChangesAsync();

                    // Create physical directory
                    var categoryPath = Path.Combine(_storagePath, "documents");
                    EnsureDirectoryExists(categoryPath);

                    _logger.LogInformation("Created default CDN category");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing CDN configuration");
            }
        }

        // File Operations
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string category = "documents", string folderPath = "")
        {
            try
            {
                // Ensure category exists or create it
                category = await EnsureAndValidateCategoryAsync(category);
                folderPath = CleanPath(folderPath);

                // Validate file
                if (!await ValidateFileTypeAsync(fileName, category))
                {
                    throw new InvalidOperationException($"File type not allowed for category {category}");
                }

                if (!await ValidateFileSizeAsync(fileStream.Length))
                {
                    throw new InvalidOperationException("File size exceeds maximum allowed size");
                }

                // Generate unique filename
                string uniqueFileName = GenerateUniqueFileName(fileName);

                // Create full directory path
                string fullDirectoryPath = Path.Combine(_storagePath, category);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    fullDirectoryPath = Path.Combine(fullDirectoryPath, folderPath);
                }

                // Ensure directory exists
                EnsureDirectoryExists(fullDirectoryPath);

                // Full file path
                string fullFilePath = Path.Combine(fullDirectoryPath, uniqueFileName);

                // Save file to disk
                using (var fileStreamDisk = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
                {
                    await fileStream.CopyToAsync(fileStreamDisk, BufferSize);
                }

                // Calculate checksum
                string checksum = await CalculateChecksumAsync(fullFilePath);

                // Save metadata to database
                var metadata = await SaveFileMetadataAsync(category, uniqueFileName, fullFilePath, contentType,
                    new FileInfo(fullFilePath).Length, checksum, folderPath);

                // Update usage statistics
                await UpdateUsageStatisticsAsync(metadata.CategoryId, 1, metadata.FileSize, 1);

                // Log access
                await LogAccessAsync("Upload", fullFilePath, "System", true, null, metadata.FileSize);

                // Generate URL - This is what the browser will use to request the file
                string relativeUrl = string.IsNullOrEmpty(folderPath)
                    ? $"{category}/{uniqueFileName}"
                    : $"{category}/{folderPath}/{uniqueFileName}";

                string url = $"{_baseUrl}/{relativeUrl}";

                _logger.LogInformation("File uploaded successfully: {Url}", url);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
                await LogAccessAsync("Upload", fileName, "System", false, ex.Message);
                throw;
            }
        }

        private async Task<bool> OnlyBase64ExistsAsync(string cdnUrl)
        {
            var physicalPath = GetPhysicalPath(cdnUrl);
            bool physicalExists = !string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath);

            if (physicalExists)
                return false;

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.Set<CdnFileMetadata>()
                .AnyAsync(f => f.Url == cdnUrl && f.HasBase64Backup && !f.IsDeleted);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string category = "documents", string folderPath = "")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            using var stream = file.OpenReadStream();
            return await UploadFileAsync(stream, file.FileName, file.ContentType, category, folderPath);
        }

        public async Task<string> UploadFileWithBase64BackupAsync(Stream fileStream, string fileName, string contentType, string category = "documents", string folderPath = "")
        {
            try
            {
                // Create a memory stream to hold the file data for reuse
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);

                // Reset position for first upload
                memoryStream.Position = 0;

                // First upload the file normally
                string url = await UploadFileAsync(memoryStream, fileName, contentType, category, folderPath);

                // Reset stream position for base64 encoding
                memoryStream.Position = 0;

                // Convert to base64
                byte[] bytes = memoryStream.ToArray();
                string base64Data = Convert.ToBase64String(bytes);

                // Save base64 backup
                await SaveBase64BackupAsync(url, base64Data, contentType);

                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file with base64 backup: {FileName}", fileName);
                throw;
            }
        }

        // Folder Operations
        public async Task<List<CdnFolder>> GetFoldersAsync(string category, string parentPath = null)
        {
            try
            {
                category = await EnsureAndValidateCategoryAsync(category);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var categoryEntity = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category);

                if (categoryEntity == null)
                {
                    return new List<CdnFolder>();
                }

                var query = dbContext.Set<CdnFolder>()
                    .Where(f => f.CategoryId == categoryEntity.Id && f.IsActive);

                if (string.IsNullOrEmpty(parentPath))
                {
                    query = query.Where(f => f.ParentId == null);
                }
                else
                {
                    var parentFolder = await dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == parentPath);

                    if (parentFolder != null)
                    {
                        query = query.Where(f => f.ParentId == parentFolder.Id);
                    }
                    else
                    {
                        return new List<CdnFolder>();
                    }
                }

                return await query.OrderBy(f => f.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders: {Category}/{ParentPath}", category, parentPath);
                return new List<CdnFolder>();
            }
        }

        public async Task<bool> DeleteFolderAsync(string category, string folderPath)
        {
            try
            {
                category = await EnsureAndValidateCategoryAsync(category);
                folderPath = CleanPath(folderPath);

                // Get full path
                string fullPath = Path.Combine(_storagePath, category, folderPath);

                // Check if folder exists
                if (!Directory.Exists(fullPath))
                {
                    return false;
                }

                // Check if folder is empty
                if (Directory.GetFiles(fullPath).Any() || Directory.GetDirectories(fullPath).Any())
                {
                    throw new InvalidOperationException("Cannot delete non-empty folder");
                }

                // Delete physical directory
                Directory.Delete(fullPath);

                // Mark as inactive in database
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var categoryEntity = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category);

                if (categoryEntity != null)
                {
                    var folder = await dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == folderPath);

                    if (folder != null)
                    {
                        folder.IsActive = false;
                        await dbContext.SaveChangesAsync();
                    }
                }

                await LogAccessAsync("DeleteFolder", fullPath, "System", true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder: {Category}/{Path}", category, folderPath);
                throw;
            }
        }

        public async Task<bool> RenameFolderAsync(string category, string oldPath, string newName)
        {
            try
            {
                category = await EnsureAndValidateCategoryAsync(category);
                oldPath = CleanPath(oldPath);

                string parentPath = Path.GetDirectoryName(oldPath)?.Replace('\\', '/') ?? "";
                string oldFullPath = Path.Combine(_storagePath, category, oldPath);
                string newFullPath = Path.Combine(_storagePath, category, parentPath, newName);

                if (!Directory.Exists(oldFullPath))
                {
                    return false;
                }

                if (Directory.Exists(newFullPath))
                {
                    throw new InvalidOperationException("A folder with this name already exists");
                }

                // Rename physical directory
                Directory.Move(oldFullPath, newFullPath);

                // Update database
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var categoryEntity = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category);

                if (categoryEntity != null)
                {
                    var folder = await dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == oldPath);

                    if (folder != null)
                    {
                        string newPath = string.IsNullOrEmpty(parentPath) ? newName : $"{parentPath}/{newName}";
                        folder.Name = newName;
                        folder.Path = newPath;

                        // Update all child folders' paths
                        var childFolders = await dbContext.Set<CdnFolder>()
                            .Where(f => f.Path.StartsWith(oldPath + "/"))
                            .ToListAsync();

                        foreach (var child in childFolders)
                        {
                            child.Path = child.Path.Replace(oldPath, newPath);
                        }

                        await dbContext.SaveChangesAsync();
                    }
                }

                await LogAccessAsync("RenameFolder", newFullPath, "System", true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming folder: {Category}/{OldPath} to {NewName}", category, oldPath, newName);
                await LogAccessAsync("RenameFolder", $"{category}/{oldPath}", "System", false, ex.Message);
                return false;
            }
        }

        public async Task<bool> MoveFolderAsync(string category, string sourcePath, string destinationPath)
        {
            try
            {
                category = await EnsureAndValidateCategoryAsync(category);
                sourcePath = CleanPath(sourcePath);
                destinationPath = CleanPath(destinationPath);

                string sourceFullPath = Path.Combine(_storagePath, category, sourcePath);
                string folderName = Path.GetFileName(sourcePath);
                string destinationFullPath = Path.Combine(_storagePath, category, destinationPath, folderName);

                if (!Directory.Exists(sourceFullPath))
                {
                    return false;
                }

                if (Directory.Exists(destinationFullPath))
                {
                    throw new InvalidOperationException("A folder with this name already exists in the destination");
                }

                // Move physical directory
                Directory.Move(sourceFullPath, destinationFullPath);

                // Update database
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var categoryEntity = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category);

                if (categoryEntity != null)
                {
                    var folder = await dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == sourcePath);

                    if (folder != null)
                    {
                        string newPath = string.IsNullOrEmpty(destinationPath) ? folderName : $"{destinationPath}/{folderName}";
                        folder.Path = newPath;

                        // Find new parent folder
                        var newParent = await dbContext.Set<CdnFolder>()
                            .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == destinationPath);

                        folder.ParentId = newParent?.Id;

                        // Update all child folders' paths
                        var childFolders = await dbContext.Set<CdnFolder>()
                            .Where(f => f.Path.StartsWith(sourcePath + "/"))
                            .ToListAsync();

                        foreach (var child in childFolders)
                        {
                            child.Path = child.Path.Replace(sourcePath, newPath);
                        }

                        await dbContext.SaveChangesAsync();
                    }
                }

                await LogAccessAsync("MoveFolder", destinationFullPath, "System", true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving folder: {Category}/{SourcePath} to {DestinationPath}", category, sourcePath, destinationPath);
                await LogAccessAsync("MoveFolder", $"{category}/{sourcePath}", "System", false, ex.Message);
                return false;
            }
        }

        // Files and folders listing
        public async Task<(List<CdnFolder> folders, List<CdnFileMetadata> files)> GetContentAsync(string category, string folderPath = null)
        {
            var folders = await GetFoldersAsync(category, folderPath);
            var files = await GetFilesAsync(category, folderPath);

            return (folders, files);
        }

        public async Task<long> GetFolderSizeAsync(string category, string folderPath)
        {
            try
            {
                category = await EnsureAndValidateCategoryAsync(category);
                folderPath = CleanPath(folderPath);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var categoryEntity = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category);

                if (categoryEntity == null)
                {
                    return 0;
                }

                var folder = await dbContext.Set<CdnFolder>()
                    .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == folderPath);

                if (folder == null)
                {
                    return 0;
                }

                // Get all files in this folder and subfolders
                var totalSize = await dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.CategoryId == categoryEntity.Id && !f.IsDeleted)
                    .Where(f => f.FolderId == folder.Id || dbContext.Set<CdnFolder>()
                        .Where(sub => sub.Path.StartsWith(folderPath + "/"))
                        .Select(sub => sub.Id)
                        .Contains(f.FolderId.Value))
                    .SumAsync(f => f.FileSize);

                return totalSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder size: {Category}/{FolderPath}", category, folderPath);
                return 0;
            }
        }

        public async Task<int> GetFileCountAsync(string category, string folderPath = null)
        {
            try
            {
                category = await EnsureAndValidateCategoryAsync(category);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var categoryEntity = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category);

                if (categoryEntity == null)
                {
                    return 0;
                }

                var query = dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.CategoryId == categoryEntity.Id && !f.IsDeleted);

                if (!string.IsNullOrEmpty(folderPath))
                {
                    folderPath = CleanPath(folderPath);
                    var folder = await dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == folderPath);

                    if (folder != null)
                    {
                        query = query.Where(f => f.FolderId == folder.Id);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    query = query.Where(f => f.FolderId == null);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file count: {Category}/{FolderPath}", category, folderPath);
                return 0;
            }
        }

        // Configuration management
        public async Task<CdnConfiguration> UpdateConfigurationAsync(CdnConfiguration config)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var existingConfig = await dbContext.Set<CdnConfiguration>()
                    .FirstOrDefaultAsync(c => c.Id == config.Id);

                if (existingConfig == null)
                {
                    throw new InvalidOperationException("Configuration not found");
                }

                existingConfig.BaseUrl = config.BaseUrl;
                existingConfig.StoragePath = config.StoragePath;
                existingConfig.MaxFileSizeMB = config.MaxFileSizeMB;
                existingConfig.AllowedFileTypes = config.AllowedFileTypes;
                existingConfig.EnableCaching = config.EnableCaching;
                existingConfig.ModifiedDate = DateTime.Now;
                existingConfig.ModifiedBy = "System"; // Should come from current user

                await dbContext.SaveChangesAsync();

                // Clear cache
                _memoryCache.Remove(CONFIG_CACHE_KEY);

                return existingConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration");
                throw;
            }
        }

        // Usage statistics
        public async Task<CdnUsageStatistic> GetUsageStatisticsAsync(DateTime date, int? categoryId = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var query = dbContext.Set<CdnUsageStatistic>()
                .Where(s => s.Date.Date == date.Date);

            if (categoryId.HasValue)
            {
                query = query.Where(s => s.CategoryId == categoryId);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<CdnUsageStatistic>> GetUsageStatisticsRangeAsync(DateTime startDate, DateTime endDate, int? categoryId = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var query = dbContext.Set<CdnUsageStatistic>()
                .Where(s => s.Date >= startDate.Date && s.Date <= endDate.Date);

            if (categoryId.HasValue)
            {
                query = query.Where(s => s.CategoryId == categoryId);
            }

            return await query.OrderBy(s => s.Date).ToListAsync();
        }

        // Utility methods
        public async Task MigrateFileToBase64Async(string cdnUrl)
        {
            try
            {
                var metadata = await GetFileMetadataAsync(cdnUrl);
                if (metadata == null)
                {
                    _logger.LogWarning("Metadata not found for migration: {Url}", cdnUrl);
                    throw new InvalidOperationException($"File metadata not found for {cdnUrl}");
                }

                // Check if base64 backup already exists
                if (metadata.HasBase64Backup)
                {
                    _logger.LogInformation("Base64 backup already exists for: {Url}", cdnUrl);
                    return;
                }

                string physicalPath = GetPhysicalPath(cdnUrl);
                if (string.IsNullOrEmpty(physicalPath) || !File.Exists(physicalPath))
                {
                    _logger.LogError("Physical file not found for migration: {Url}, Path: {Path}", cdnUrl, physicalPath);
                    throw new FileNotFoundException($"Physical file not found for {cdnUrl}");
                }

                // Read file and convert to base64
                byte[] bytes = await File.ReadAllBytesAsync(physicalPath);
                string base64Data = Convert.ToBase64String(bytes);

                // Save base64 backup
                await SaveBase64BackupAsync(cdnUrl, base64Data, metadata.ContentType);

                _logger.LogInformation("Successfully migrated file to base64: {Url}, Original size: {Size} bytes, Base64 length: {Base64Length}",
                    cdnUrl, bytes.Length, base64Data.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating file to base64: {Url}", cdnUrl);
                throw;
            }
        }

        public async Task<bool> RestoreFromBase64Async(string cdnUrl)
        {
            try
            {
                var base64Data = await GetBase64FallbackAsync(cdnUrl);
                if (base64Data == null)
                {
                    return false;
                }

                var metadata = await GetFileMetadataAsync(cdnUrl);
                if (metadata == null)
                {
                    return false;
                }

                string physicalPath = metadata.FilePath;
                string directory = Path.GetDirectoryName(physicalPath);

                EnsureDirectoryExists(directory);

                byte[] bytes = Convert.FromBase64String(base64Data.Base64Data);
                await File.WriteAllBytesAsync(physicalPath, bytes);

                await LogAccessAsync("RestoreFromBase64", physicalPath, "System", true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring from base64: {Url}", cdnUrl);
                await LogAccessAsync("RestoreFromBase64", cdnUrl, "System", false, ex.Message);
                return false;
            }
        }

        public async Task CleanupDeletedFilesAsync(int daysOld = 30)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var cutoffDate = DateTime.Now.AddDays(-daysOld);

                var deletedFiles = await dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.IsDeleted && f.DeletedDate < cutoffDate)
                    .ToListAsync();

                foreach (var file in deletedFiles)
                {
                    // Remove base64 backup if exists
                    var base64Storage = await dbContext.Set<CdnBase64Storage>()
                        .FirstOrDefaultAsync(b => b.FileMetadataId == file.Id);

                    if (base64Storage != null)
                    {
                        dbContext.Remove(base64Storage);
                    }

                    // Remove file metadata
                    dbContext.Remove(file);
                }

                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} deleted files older than {Days} days", deletedFiles.Count, daysOld);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up deleted files");
            }
        }

        // Helper methods (these are private implementation details)
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogDebug("Created directory: {Path}", path);
            }
        }

        private string CleanPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // Remove dangerous path elements
            path = path.Replace("..", "");
            path = path.Replace("\\", "/");
            path = path.Trim('/');

            // Remove double slashes
            while (path.Contains("//"))
            {
                path = path.Replace("//", "/");
            }

            return path;
        }

        private string GetRelativePath(string cdnUrl)
        {
            if (string.IsNullOrEmpty(cdnUrl))
                return null;

            // Remove base URL to get relative path
            if (cdnUrl.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return cdnUrl.Substring(_baseUrl.Length).TrimStart('/');
            }

            // Handle relative paths
            if (cdnUrl.StartsWith("/cdn/"))
            {
                return cdnUrl.Substring(5);
            }

            return cdnUrl.TrimStart('/');
        }

        private string GenerateUniqueFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            // Clean the filename
            nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9_-]", "-");

            // Generate unique identifier
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);

            return $"{nameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
        }

        private async Task<CdnFileMetadata> SaveFileMetadataAsync(string category, string fileName, string filePath,
            string contentType, long fileSize, string checksum, string folderPath)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Get category
            var categoryEntity = await dbContext.Set<CdnCategory>()
                .FirstOrDefaultAsync(c => c.Name == category);

            if (categoryEntity == null)
            {
                throw new InvalidOperationException($"Category '{category}' not found");
            }

            // Get folder ID if specified
            int? folderId = null;
            if (!string.IsNullOrEmpty(folderPath))
            {
                var folder = await dbContext.Set<CdnFolder>()
                    .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == folderPath);

                folderId = folder?.Id;
            }

            // Create URL
            string relativeUrl = string.IsNullOrEmpty(folderPath)
                ? $"{category}/{fileName}"
                : $"{category}/{folderPath}/{fileName}";
            string url = $"{_baseUrl}/{relativeUrl}";

            // Create metadata
            var metadata = new CdnFileMetadata
            {
                FileName = fileName,
                FilePath = filePath,
                Url = url,
                ContentType = contentType,
                FileSize = fileSize,
                CategoryId = categoryEntity.Id,
                FolderId = folderId,
                Checksum = checksum,
                UploadDate = DateTime.Now,
                UploadedBy = "System",
                IsDeleted = false,
                HasBase64Backup = false
            };

            dbContext.Add(metadata);
            await dbContext.SaveChangesAsync();

            return metadata;
        }

        private async Task<string> EnsureAndValidateCategoryAsync(string category)
        {
            if (string.IsNullOrEmpty(category))
                category = "documents";

            // Sanitize category name
            category = System.Text.RegularExpressions.Regex.Replace(category.ToLower(), @"[^a-z0-9_-]", "-");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existingCategory = await dbContext.Set<CdnCategory>()
                .FirstOrDefaultAsync(c => c.Name == category && c.IsActive);

            if (existingCategory == null)
            {
                // Create category if it doesn't exist
                existingCategory = new CdnCategory
                {
                    Name = category,
                    DisplayName = char.ToUpper(category[0]) + category.Substring(1),
                    AllowedFileTypes = "*",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                };

                dbContext.Add(existingCategory);
                await dbContext.SaveChangesAsync();

                // Clear cache
                _memoryCache.Remove(CATEGORIES_CACHE_KEY);

                // Create physical directory
                var categoryPath = Path.Combine(_storagePath, category);
                EnsureDirectoryExists(categoryPath);

                _logger.LogInformation("Auto-created category: {Category}", category);
            }

            return category;
        }

        private async Task SaveBase64BackupAsync(string fileUrl, string base64Data, string contentType)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var metadata = await dbContext.Set<CdnFileMetadata>()
                    .FirstOrDefaultAsync(f => f.Url == fileUrl);

                if (metadata == null)
                {
                    _logger.LogError("Metadata not found for URL: {Url}", fileUrl);
                    throw new InvalidOperationException($"File metadata not found for {fileUrl}");
                }

                // Check if base64 storage already exists
                var existingBase64 = await dbContext.Set<CdnBase64Storage>()
                    .FirstOrDefaultAsync(b => b.FileMetadataId == metadata.Id);

                if (existingBase64 != null)
                {
                    // Update existing base64 data
                    existingBase64.Base64Data = base64Data;
                    existingBase64.MimeType = contentType;
                    existingBase64.CreatedDate = DateTime.Now;
                    dbContext.Update(existingBase64);
                }
                else
                {
                    // Create new base64 storage
                    var base64Storage = new CdnBase64Storage
                    {
                        FileMetadataId = metadata.Id,
                        Base64Data = base64Data,
                        MimeType = contentType,
                        CreatedDate = DateTime.Now
                    };

                    dbContext.Add(base64Storage);
                }

                // Update metadata to indicate it has a backup
                metadata.HasBase64Backup = true;
                dbContext.Update(metadata);

                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Base64 backup saved for file: {Url}, Data length: {Length} characters",
                    fileUrl, base64Data.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving base64 backup for URL: {Url}", fileUrl);
                throw;
            }
        }

        private async Task<CdnBase64Storage> GetBase64FallbackAsync(string cdnUrl)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var metadata = await dbContext.Set<CdnFileMetadata>()
                    .Include(f => f.Base64Storage)
                    .FirstOrDefaultAsync(f => f.Url == cdnUrl && !f.IsDeleted);

                if (metadata?.Base64Storage != null)
                {
                    _logger.LogInformation("Base64 fallback found for: {Url}, Data length: {Length}",
                        cdnUrl, metadata.Base64Storage.Base64Data?.Length ?? 0);
                    return metadata.Base64Storage;
                }

                _logger.LogWarning("No base64 fallback found for: {Url}", cdnUrl);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting base64 fallback for: {Url}", cdnUrl);
                return null;
            }
        }

        // Implement all remaining public interface methods...
        public async Task<(Stream stream, bool isFromBase64)> GetFileStreamAsync(string cdnUrl)
        {
            try
            {
                // Try to get physical file first
                string physicalPath = GetPhysicalPath(cdnUrl);

                if (!string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath))
                {
                    var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
                    return (stream, false);
                }

                // If file doesn't exist physically, try base64 fallback
                var base64Data = await GetBase64FallbackAsync(cdnUrl);
                if (base64Data != null)
                {
                    byte[] bytes = Convert.FromBase64String(base64Data.Base64Data);
                    _logger.LogInformation("Serving file from base64 backup: {Url}", cdnUrl);
                    return (new MemoryStream(bytes), true);
                }

                return (null, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file stream for: {Url}", cdnUrl);
                return (null, false);
            }
        }

        public async Task<bool> DeleteFileAsync(string cdnUrl)
        {
            try
            {
                string physicalPath = GetPhysicalPath(cdnUrl);

                // Delete physical file if exists
                if (!string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }

                // Update database
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var metadata = await dbContext.Set<CdnFileMetadata>()
                    .FirstOrDefaultAsync(f => f.Url == cdnUrl);

                if (metadata != null)
                {
                    metadata.IsDeleted = true;
                    metadata.DeletedDate = DateTime.Now;
                    await dbContext.SaveChangesAsync();

                    // Update usage statistics
                    await UpdateUsageStatisticsAsync(metadata.CategoryId, -1, -metadata.FileSize, 0, 0, 1);

                    // Log access
                    await LogAccessAsync("Delete", physicalPath ?? cdnUrl, "System", true, null, metadata.FileSize);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Url}", cdnUrl);
                await LogAccessAsync("Delete", cdnUrl, "System", false, ex.Message);
                return false;
            }
        }

        public async Task<string> RenameFileAsync(string cdnUrl, string newFileName)
        {
            try
            {
                string physicalPath = GetPhysicalPath(cdnUrl);

                if (string.IsNullOrEmpty(physicalPath) || !File.Exists(physicalPath))
                {
                    throw new FileNotFoundException("File not found");
                }

                string directory = Path.GetDirectoryName(physicalPath);
                string extension = Path.GetExtension(physicalPath);

                if (!newFileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    newFileName += extension;
                }

                string newPath = Path.Combine(directory, newFileName);

                if (File.Exists(newPath))
                {
                    throw new InvalidOperationException("A file with this name already exists");
                }

                // Rename file
                File.Move(physicalPath, newPath);

                // Update database
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var metadata = await dbContext.Set<CdnFileMetadata>()
                    .FirstOrDefaultAsync(f => f.Url == cdnUrl);

                if (metadata != null)
                {
                    string relativePath = GetRelativePath(cdnUrl);
                    string newRelativePath = Path.GetDirectoryName(relativePath).Replace('\\', '/') + "/" + newFileName;
                    string newUrl = $"{_baseUrl}/{newRelativePath}";

                    metadata.FileName = newFileName;
                    metadata.FilePath = newPath;
                    metadata.Url = newUrl;

                    await dbContext.SaveChangesAsync();

                    await LogAccessAsync("Rename", newPath, "System", true);

                    return newUrl;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming file: {Url}", cdnUrl);
                await LogAccessAsync("Rename", cdnUrl, "System", false, ex.Message);
                throw;
            }
        }

        // New method to get only base64 stream
        public async Task<Stream> GetBase64StreamAsync(string cdnUrl)
        {
            try
            {
                var base64Data = await GetBase64FallbackAsync(cdnUrl);
                if (base64Data != null && !string.IsNullOrEmpty(base64Data.Base64Data))
                {
                    byte[] bytes = Convert.FromBase64String(base64Data.Base64Data);
                    _logger.LogInformation("Serving base64 stream for: {Url}, Size: {Size} bytes", cdnUrl, bytes.Length);
                    return new MemoryStream(bytes);
                }

                _logger.LogWarning("No base64 data found for: {Url}", cdnUrl);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting base64 stream for: {Url}", cdnUrl);
                return null;
            }
        }

        public async Task<bool> FileExistsAsync(string cdnUrl)
        {
            // Check physical file first
            var physicalPath = GetPhysicalPath(cdnUrl);
            if (!string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath))
                return true;

            // Check if exists in database with base64 backup
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var metadata = await dbContext.Set<CdnFileMetadata>()
                .FirstOrDefaultAsync(f => f.Url == cdnUrl && !f.IsDeleted);

            return metadata != null;
        }

        public async Task<CdnFileMetadata> GetFileMetadataAsync(string cdnUrl)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.Set<CdnFileMetadata>()
                .Include(f => f.Category)
                .Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.Url == cdnUrl && !f.IsDeleted);
        }

        public async Task<CdnFolder> CreateFolderAsync(string category, string folderPath, string folderName)
        {
            try
            {
                category = await EnsureAndValidateCategoryAsync(category);
                folderPath = CleanPath(folderPath);

                // Create physical directory
                string fullPath = Path.Combine(_storagePath, category);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    fullPath = Path.Combine(fullPath, folderPath);
                }
                fullPath = Path.Combine(fullPath, folderName);

                EnsureDirectoryExists(fullPath);

                // Save to database
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var categoryEntity = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category);

                if (categoryEntity == null)
                {
                    throw new InvalidOperationException($"Category '{category}' not found");
                }

                // Find parent folder if specified
                int? parentId = null;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    var parentFolder = await dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == folderPath);

                    parentId = parentFolder?.Id;
                }

                // Create folder entity
                string fullFolderPath = string.IsNullOrEmpty(folderPath)
                    ? folderName
                    : $"{folderPath}/{folderName}";

                var folder = new CdnFolder
                {
                    Name = folderName,
                    Path = fullFolderPath,
                    CategoryId = categoryEntity.Id,
                    ParentId = parentId,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System",
                    IsActive = true
                };

                dbContext.Add(folder);
                await dbContext.SaveChangesAsync();

                await LogAccessAsync("CreateFolder", fullPath, "System", true);

                return folder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder: {Category}/{Path}/{Name}", category, folderPath, folderName);
                await LogAccessAsync("CreateFolder", $"{category}/{folderPath}/{folderName}", "System", false, ex.Message);
                throw;
            }
        }

        public async Task<List<CdnCategory>> GetCategoriesAsync()
        {
            if (_memoryCache.TryGetValue(CATEGORIES_CACHE_KEY, out List<CdnCategory> categories))
            {
                return categories;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            categories = await dbContext.Set<CdnCategory>()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayName)
                .ToListAsync();

            _memoryCache.Set(CATEGORIES_CACHE_KEY, categories, CacheDuration);

            return categories;
        }

        public async Task<CdnCategory> GetCategoryAsync(string name)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.Set<CdnCategory>()
                .FirstOrDefaultAsync(c => c.Name == name && c.IsActive);
        }

        public async Task<CdnCategory> CreateCategoryAsync(string name, string displayName, string allowedFileTypes = "*", string description = null)
        {
            try
            {
                // Sanitize name for filesystem
                name = System.Text.RegularExpressions.Regex.Replace(name.ToLower(), @"[^a-z0-9_-]", "-");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check if category already exists
                if (await dbContext.Set<CdnCategory>().AnyAsync(c => c.Name == name))
                {
                    throw new InvalidOperationException($"Category '{name}' already exists");
                }

                var category = new CdnCategory
                {
                    Name = name,
                    DisplayName = displayName,
                    Description = description,
                    AllowedFileTypes = allowedFileTypes,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                };

                dbContext.Add(category);
                await dbContext.SaveChangesAsync();

                // Clear cache
                _memoryCache.Remove(CATEGORIES_CACHE_KEY);

                // Create physical directory
                var categoryPath = Path.Combine(_storagePath, name);
                EnsureDirectoryExists(categoryPath);

                _logger.LogInformation("Created CDN category: {Name}", name);

                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category: {Name}", name);
                throw;
            }
        }

        public async Task<CdnCategory> UpdateCategoryAsync(int categoryId, string displayName, string allowedFileTypes, string description)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var category = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Id == categoryId);

                if (category == null)
                {
                    throw new InvalidOperationException("Category not found");
                }

                category.DisplayName = displayName;
                category.AllowedFileTypes = allowedFileTypes;
                category.Description = description;

                await dbContext.SaveChangesAsync();

                // Clear cache
                _memoryCache.Remove(CATEGORIES_CACHE_KEY);

                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category: {Id}", categoryId);
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var category = await dbContext.Set<CdnCategory>()
                    .Include(c => c.Files)
                    .FirstOrDefaultAsync(c => c.Id == categoryId);

                if (category == null)
                    return false;

                // Check if category has active files
                if (category.Files.Any(f => !f.IsDeleted))
                {
                    throw new InvalidOperationException("Cannot delete category with active files");
                }

                // Soft delete
                category.IsActive = false;
                await dbContext.SaveChangesAsync();

                // Clear cache
                _memoryCache.Remove(CATEGORIES_CACHE_KEY);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {Id}", categoryId);
                throw;
            }
        }

        public async Task<bool> CategoryExistsAsync(string name)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.Set<CdnCategory>()
                .AnyAsync(c => c.Name == name && c.IsActive);
        }

        public async Task<CdnConfiguration> GetConfigurationAsync()
        {
            if (_memoryCache.TryGetValue(CONFIG_CACHE_KEY, out CdnConfiguration config))
            {
                return config;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            config = await dbContext.Set<CdnConfiguration>()
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (config != null)
            {
                _memoryCache.Set(CONFIG_CACHE_KEY, config, CacheDuration);
            }

            return config;
        }

        public async Task<bool> ValidateFileTypeAsync(string fileName, string category)
        {
            var categoryEntity = await GetCategoryAsync(category);
            if (categoryEntity == null || categoryEntity.AllowedFileTypes == "*")
                return true;

            var extension = Path.GetExtension(fileName).ToLower();
            var allowedTypes = categoryEntity.AllowedFileTypes.Split(',').Select(t => t.Trim().ToLower());

            return allowedTypes.Contains(extension);
        }

        public async Task<bool> ValidateFileSizeAsync(long fileSize)
        {
            var config = await GetConfigurationAsync();
            if (config == null)
                return true;

            var maxSizeBytes = config.MaxFileSizeMB * 1024 * 1024;
            return fileSize <= maxSizeBytes;
        }

        public async Task UpdateUsageStatisticsAsync(int categoryId, int fileCountChange, long storageBytesChange,
            int uploadCount = 0, int downloadCount = 0, int deleteCount = 0)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var today = DateTime.Today;
                var stat = await dbContext.Set<CdnUsageStatistic>()
                    .Where(s => s.Date.Date == today && s.CategoryId == categoryId)
                    .FirstOrDefaultAsync();

                if (stat == null)
                {
                    stat = new CdnUsageStatistic
                    {
                        Date = today,
                        CategoryId = categoryId,
                        FileCount = fileCountChange,
                        StorageUsedBytes = storageBytesChange,
                        UploadCount = uploadCount,
                        DownloadCount = downloadCount,
                        DeleteCount = deleteCount,
                        RecordedBy = "System"
                    };

                    dbContext.Add(stat);
                }
                else
                {
                    stat.FileCount += fileCountChange;
                    stat.StorageUsedBytes += storageBytesChange;
                    stat.UploadCount += uploadCount;
                    stat.DownloadCount += downloadCount;
                    stat.DeleteCount += deleteCount;
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating usage statistics");
            }
        }

        public async Task LogAccessAsync(string actionType, string filePath, string username, bool isSuccess,
            string errorMessage = null, long? fileSizeBytes = null)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var log = new CdnAccessLog
                {
                    Timestamp = DateTime.Now,
                    ActionType = actionType,
                    FilePath = filePath,
                    Username = username,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    FileSizeBytes = fileSizeBytes
                };

                dbContext.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging access");
            }
        }

        public async Task<List<CdnAccessLog>> GetAccessLogsAsync(DateTime startDate, DateTime endDate, string actionType = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var query = dbContext.Set<CdnAccessLog>()
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate);

            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(l => l.ActionType == actionType);
            }

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }

        public string GetCdnUrl(string path)
        {
            if (string.IsNullOrEmpty(path))
                return _baseUrl;

            if (path.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase))
                return path;

            return $"{_baseUrl}/{path.TrimStart('/')}";
        }

        public bool IsDirectAccessAvailable()
        {
            return Directory.Exists(_storagePath);
        }

        public string GetPhysicalPath(string cdnUrl)
        {
            if (string.IsNullOrEmpty(cdnUrl))
                return null;

            string relativePath = GetRelativePath(cdnUrl);
            if (string.IsNullOrEmpty(relativePath))
                return null;

            return Path.Combine(_storagePath, relativePath);
        }

        public async Task<string> CalculateChecksumAsync(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true))
                {
                    byte[] hash = await Task.Run(() => sha256.ComputeHash(stream));
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public async Task<List<CdnFileMetadata>> GetFilesAsync(string category = "documents", string folderPath = null, string searchTerm = null)
        {
            try
            {
                category = await EnsureAndValidateCategoryAsync(category);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var categoryEntity = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == category);

                if (categoryEntity == null)
                {
                    return new List<CdnFileMetadata>();
                }

                var query = dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.CategoryId == categoryEntity.Id && !f.IsDeleted);

                if (!string.IsNullOrEmpty(folderPath))
                {
                    folderPath = CleanPath(folderPath);
                    var folder = await dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryEntity.Id && f.Path == folderPath);

                    if (folder != null)
                    {
                        query = query.Where(f => f.FolderId == folder.Id);
                    }
                    else
                    {
                        return new List<CdnFileMetadata>();
                    }
                }
                else
                {
                    query = query.Where(f => f.FolderId == null);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(f => f.FileName.Contains(searchTerm));
                }

                return await query.OrderByDescending(f => f.UploadDate).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files");
                return new List<CdnFileMetadata>();
            }
        }
    }
}