using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Roovia.Interfaces;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Roovia.Models.CDN;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Text.Json;
using System.Net.Http.Json;

namespace Roovia.Services
{
    public class CdnService : ICdnService
    {
        private readonly IConfiguration _configuration;
        private readonly string _bootstrapApiKey;
        private readonly string _bootstrapCdnBaseUrl;
        private readonly string _bootstrapCdnStoragePath;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CdnService> _logger;
        private readonly IMemoryCache _memoryCache;

        // Production API URL - no longer needed for direct access
        private readonly string _productionApiUrl;
        private readonly bool _forceRemoteApiInTestEnv;

        // Cache keys
        private const string CONFIG_CACHE_KEY = "CdnConfiguration";
        private const string CATEGORIES_CACHE_KEY = "CdnCategories";

        // Cache common API responses to avoid redundant disk operations
        private static readonly ConcurrentDictionary<string, string> _filePathCache = new();
        private static readonly ConcurrentDictionary<string, DateTime> _cacheExpiry = new();
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);

        // Large buffer size for improved I/O performance
        private const int LargeBufferSize = 262144; // 256KB buffer

        // HttpClient optimized for CDN uploads - only kept for backward compatibility
        private readonly HttpClient _optimizedHttpClient;

        // Database configuration 
        private CdnConfiguration _cdnConfig;
        private List<CdnCategory> _categories;
        private DateTime _configLastRefreshed;
        private readonly TimeSpan _configRefreshInterval = TimeSpan.FromMinutes(5);

        // File operation retry settings
        private const int MaxRetryAttempts = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

        public CdnService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment,
            IServiceProvider serviceProvider,
            ILogger<CdnService> logger,
            IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _memoryCache = memoryCache;

            // Get bootstrap configuration from appsettings.json
            _bootstrapCdnBaseUrl = configuration["CDN:BaseUrl"] ?? "https://cdn.roovia.co.za";
            _bootstrapCdnStoragePath = configuration["CDN:StoragePath"] ?? Path.Combine(environment.ContentRootPath, "wwwroot", "cdn");
            _bootstrapApiKey = configuration["CDN:ApiKey"] ?? "RooviaCDNKey";

            // These are no longer used for actual API calls but kept for backward compatibility
            _productionApiUrl = configuration["CDN:ProductionApiUrl"] ?? "https://portal.roovia.co.za/api/cdn";
            _forceRemoteApiInTestEnv = false; // Force direct access always

            _logger.LogInformation("CdnService initialized with base URL: {BaseUrl}, storage path: {StoragePath}",
                _bootstrapCdnBaseUrl, _bootstrapCdnStoragePath);

            // Create an optimized HttpClient for CDN operations with longer timeout
            _optimizedHttpClient = _httpClientFactory.CreateClient("CdnClient");
            _optimizedHttpClient.Timeout = TimeSpan.FromMinutes(10); // 10 minute timeout for large file uploads

            // Load the database configuration asynchronously
            Task.Run(async () => await LoadConfigFromDatabaseAsync());

            // Ensure bootstrap storage path exists
            if (!Directory.Exists(_bootstrapCdnStoragePath))
            {
                try
                {
                    Directory.CreateDirectory(_bootstrapCdnStoragePath);
                    _logger.LogInformation("Created bootstrap CDN storage path: {Path}", _bootstrapCdnStoragePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to create bootstrap CDN storage path: {Path}. Error: {Error}",
                        _bootstrapCdnStoragePath, ex.Message);
                }
            }
        }

        private async Task LoadConfigFromDatabaseAsync()
        {
            try
            {
                // First check if we have the config cached
                if (_memoryCache.TryGetValue(CONFIG_CACHE_KEY, out CdnConfiguration cachedConfig))
                {
                    _cdnConfig = cachedConfig;
                    _logger.LogDebug("Loaded CDN configuration from cache");
                }
                else
                {
                    // Create a scope to resolve the DbContext
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                    // Get the active configuration
                    var config = await dbContext.Set<CdnConfiguration>()
                        .Where(c => c.IsActive)
                        .OrderByDescending(c => c.Id)
                        .FirstOrDefaultAsync();

                    if (config != null)
                    {
                        // Update cache
                        _memoryCache.Set(CONFIG_CACHE_KEY, config, TimeSpan.FromMinutes(15));

                        // Update the configuration values
                        _cdnConfig = config;
                        _logger.LogInformation("Loaded CDN configuration from database: ID={Id}, BaseUrl={BaseUrl}",
                            config.Id, config.BaseUrl);
                    }
                    else
                    {
                        // If no config in DB, use the bootstrap values
                        _cdnConfig = new CdnConfiguration
                        {
                            BaseUrl = _bootstrapCdnBaseUrl,
                            StoragePath = _bootstrapCdnStoragePath,
                            ApiKey = _bootstrapApiKey,
                            MaxFileSizeMB = 200,
                            AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.mp4,.mp3,.zip",
                            EnforceAuthentication = true,
                            AllowDirectAccess = true,
                            EnableCaching = true
                        };
                        _logger.LogInformation("No CDN configuration found in database. Using bootstrap values: BaseUrl={BaseUrl}",
                            _bootstrapCdnBaseUrl);
                    }
                }

                // Check if categories are cached
                if (_memoryCache.TryGetValue(CATEGORIES_CACHE_KEY, out List<CdnCategory> cachedCategories))
                {
                    _categories = cachedCategories;
                    _logger.LogDebug("Loaded CDN categories from cache: Count={Count}", cachedCategories.Count);
                }
                else
                {
                    // Load categories from database
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                    var categories = await dbContext.Set<CdnCategory>()
                        .Where(c => c.IsActive)
                        .ToListAsync();

                    if (categories.Any())
                    {
                        _memoryCache.Set(CATEGORIES_CACHE_KEY, categories, TimeSpan.FromMinutes(15));
                        _categories = categories;
                        _logger.LogInformation("Loaded {Count} CDN categories from database", categories.Count);
                    }
                    else
                    {
                        // Create default categories if none exist
                        _categories = new List<CdnCategory>
                        {
                            new CdnCategory { Name = "documents", DisplayName = "Documents", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.txt" },
                            new CdnCategory { Name = "images", DisplayName = "Images", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.webp,.svg" },
                            new CdnCategory { Name = "test-uploads", DisplayName = "Test Uploads", AllowedFileTypes = "*" },
                        };
                        _logger.LogInformation("No CDN categories found in database. Using default categories");
                    }
                }

                // Ensure storage directory exists for each category
                EnsureCategoryDirectoriesExist();

                // No longer needed but kept for compatibility
                if (_optimizedHttpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
                {
                    _optimizedHttpClient.DefaultRequestHeaders.Remove("X-Api-Key");
                }
                _optimizedHttpClient.DefaultRequestHeaders.Add("X-Api-Key", GetApiKey());

                _configLastRefreshed = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading CDN configuration from database");

                // Use bootstrap settings as fallback
                if (_cdnConfig == null)
                {
                    _cdnConfig = new CdnConfiguration
                    {
                        BaseUrl = _bootstrapCdnBaseUrl,
                        StoragePath = _bootstrapCdnStoragePath,
                        ApiKey = _bootstrapApiKey,
                        MaxFileSizeMB = 200,
                        AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.mp4,.mp3,.zip",
                        EnforceAuthentication = true,
                        AllowDirectAccess = true,
                        EnableCaching = true
                    };
                }

                // Default categories if not set
                if (_categories == null)
                {
                    _categories = new List<CdnCategory>
                    {
                        new CdnCategory { Name = "documents", DisplayName = "Documents", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.txt" },
                        new CdnCategory { Name = "images", DisplayName = "Images", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.webp,.svg" },
                        new CdnCategory { Name = "test-uploads", DisplayName = "Test Uploads", AllowedFileTypes = "*" },
                    };
                }

                // Ensure storage directory exists for each category
                EnsureCategoryDirectoriesExist();
            }
        }

        private void EnsureCategoryDirectoriesExist()
        {
            if (_categories == null)
                return;

            string storagePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;
            if (string.IsNullOrEmpty(storagePath))
                return;

            // Try to create the storage path if it doesn't exist
            if (!Directory.Exists(storagePath))
            {
                try
                {
                    Directory.CreateDirectory(storagePath);
                    _logger.LogInformation("Created CDN storage path: {Path}", storagePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to create CDN storage path: {Error}", ex.Message);
                    return;
                }
            }

            foreach (var category in _categories)
            {
                string categoryPath = Path.Combine(storagePath, category.Name);
                if (!Directory.Exists(categoryPath))
                {
                    try
                    {
                        Directory.CreateDirectory(categoryPath);
                        _logger.LogInformation("Created directory for category {Category}: {Path}",
                            category.Name, categoryPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to create directory for category {Category}: {Error}",
                            category.Name, ex.Message);
                    }
                }
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string category = "documents", string folderPath = "")
        {
            try
            {
                _logger.LogInformation("Uploading file: {FileName}, ContentType: {ContentType}, Category: {Category}, Folder: {Folder}",
                    fileName, contentType, category, folderPath);

                // Ensure category is valid
                category = await ValidateCategoryAsync(category);

                // Clean up folder path for security
                folderPath = CleanFolderPath(folderPath);

                // Check if we need to refresh configuration
                await RefreshConfigIfNeededAsync();

                // Generate a unique file name to avoid collisions
                string uniqueFileName = GenerateUniqueFileName(fileName);

                // Get storage path from configuration
                string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;

                // Create category directory path
                string categoryPath = Path.Combine(cdnStoragePath, category);
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                    _logger.LogInformation("Created category directory: {Path}", categoryPath);
                }

                // Create folder path if specified
                string targetFolderPath = categoryPath;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    string[] folders = folderPath.Split('/', '\\');
                    string currentPath = categoryPath;

                    foreach (var folder in folders)
                    {
                        if (string.IsNullOrWhiteSpace(folder)) continue;

                        currentPath = Path.Combine(currentPath, folder);
                        if (!Directory.Exists(currentPath))
                        {
                            Directory.CreateDirectory(currentPath);
                            _logger.LogDebug("Created subfolder: {Path}", currentPath);
                        }
                    }

                    targetFolderPath = currentPath;
                }

                // Full path where the file will be saved
                string fullFilePath = Path.Combine(targetFolderPath, uniqueFileName);

                // Save the file to disk with optimized buffer
                using (var fileStream2 = new FileStream(
                    fullFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    LargeBufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await fileStream.CopyToAsync(fileStream2, LargeBufferSize);
                }

                _logger.LogInformation("File saved to: {Path}", fullFilePath);

                // Create database folder record if needed
                int? folderId = null;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    folderId = await GetOrCreateFolderAsync(category, folderPath);
                }

                // Get file size
                var fileInfo = new FileInfo(fullFilePath);
                long fileSize = fileInfo.Length;

                // Build the CDN URL
                string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
                string relativeUrlPath = string.IsNullOrEmpty(folderPath)
                    ? $"{category}/{uniqueFileName}"
                    : $"{category}/{folderPath.TrimStart('/').TrimEnd('/')}/{uniqueFileName}";

                string fileUrl = $"{cdnBaseUrl.TrimEnd('/')}/{relativeUrlPath}";

                // Save metadata to database
                await SaveFileMetadataAsync(category, uniqueFileName, fullFilePath, contentType, fileSize, folderId, folderPath);

                // Update usage statistics for the category
                var categoryId = await GetCategoryIdAsync(category);
                if (categoryId.HasValue)
                {
                    await UpdateUsageStatisticsAsync(categoryId.Value, 1, fileSize, 1, 0, 0);
                }

                _logger.LogInformation("File uploaded successfully: {Url}", fileUrl);
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
                throw new Exception($"Failed to upload file: {ex.Message}", ex);
            }
        }

        private string CleanFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return string.Empty;

            // Remove any potentially dangerous path characters
            folderPath = folderPath.Replace("..", string.Empty);

            // Replace backslashes with forward slashes
            folderPath = folderPath.Replace("\\", "/");

            // Trim leading and trailing slashes
            folderPath = folderPath.Trim('/');

            // Remove any double slashes
            while (folderPath.Contains("//"))
            {
                folderPath = folderPath.Replace("//", "/");
            }

            return folderPath;
        }

        public bool IsDirectAccessAvailable()
        {
            // Always return true since we're using direct access
            string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;

            // Check if storage path exists
            bool pathExists = !string.IsNullOrEmpty(cdnStoragePath) && Directory.Exists(cdnStoragePath);

            // If in development, we might use local storage
            if (_environment.IsDevelopment() && !pathExists)
            {
                // Check if we can use local storage
                string localStoragePath = Path.Combine(_environment.ContentRootPath, "wwwroot", "cdn");
                if (!Directory.Exists(localStoragePath))
                {
                    try
                    {
                        Directory.CreateDirectory(localStoragePath);
                        _logger.LogInformation("Created local CDN storage directory: {Path}", localStoragePath);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to create local CDN storage directory: {Error}", ex.Message);
                        return false;
                    }
                }
                return true;
            }

            return pathExists;
        }

        private async Task<int?> GetOrCreateFolderAsync(string categoryName, string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return null;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                // Find the category ID
                var category = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == categoryName && c.IsActive);

                if (category == null)
                {
                    // Create the category if it doesn't exist
                    category = new CdnCategory
                    {
                        Name = categoryName,
                        DisplayName = char.ToUpper(categoryName[0]) + categoryName.Substring(1),
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System"
                    };
                    dbContext.Add(category);
                    await dbContext.SaveChangesAsync();
                }

                // Split path into segments
                var segments = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

                int? parentId = null;
                string currentPath = "";

                // Process each segment, creating folders as needed
                foreach (var segment in segments)
                {
                    currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

                    // Look for existing folder
                    var folder = await dbContext.Set<CdnFolder>()
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
                            CreatedBy = "System",
                            IsActive = true
                        };

                        dbContext.Add(folder);
                        await dbContext.SaveChangesAsync();
                    }

                    parentId = folder.Id;
                }

                return parentId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder structure: {Path}", folderPath);
                return null;
            }
        }

        private async Task SaveFileMetadataAsync(string categoryName, string fileName, string filePath, string contentType, long fileSize, int? folderId, string folderPath)
        {
            try
            {
                // Create a scope to resolve the DbContext
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                // Get category ID
                var category = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == categoryName && c.IsActive);

                if (category == null)
                {
                    // Create default category if not found
                    category = new CdnCategory
                    {
                        Name = categoryName,
                        DisplayName = char.ToUpper(categoryName[0]) + categoryName.Substring(1),
                        AllowedFileTypes = "*",
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System"
                    };

                    dbContext.Add(category);
                    await dbContext.SaveChangesAsync();
                }

                // Create URL
                string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
                string url = string.IsNullOrEmpty(folderPath)
                    ? $"{cdnBaseUrl.TrimEnd('/')}/{categoryName}/{fileName}"
                    : $"{cdnBaseUrl.TrimEnd('/')}/{categoryName}/{folderPath.TrimStart('/').TrimEnd('/')}/{fileName}";

                // Create metadata record
                var metadata = new CdnFileMetadata
                {
                    FilePath = filePath,
                    FileName = fileName,
                    ContentType = contentType,
                    FileSize = fileSize,
                    CategoryId = category.Id,
                    FolderId = folderId,
                    UploadDate = DateTime.Now,
                    UploadedBy = "System", // Ideally this would come from the current user
                    Url = url,
                    Checksum = null // Could calculate MD5 or SHA hash if needed
                };

                dbContext.Add(metadata);
                await dbContext.SaveChangesAsync();

                // Update usage statistics
                await UpdateUsageStatisticsAsync(category.Id, 0, fileSize, 1, 0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file metadata for {FileName}", fileName);
                // Don't rethrow to avoid failing the upload process
            }
        }

        private async Task UpdateUsageStatisticsAsync(
            int categoryId,
            int fileCountChange,
            long storageBytesChange,
            int uploadCountChange,
            int downloadCountChange,
            int deleteCountChange)
        {
            try
            {
                // Create a scope to resolve the DbContext
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                // Get today's statistics record or create a new one
                var today = DateTime.Today;
                var stat = await dbContext.Set<CdnUsageStatistic>()
                    .Where(s => s.Date.Date == today && s.CategoryId == categoryId)
                    .FirstOrDefaultAsync();

                if (stat == null)
                {
                    // Create a new record
                    stat = new CdnUsageStatistic
                    {
                        Date = today,
                        CategoryId = categoryId,
                        FileCount = fileCountChange,
                        StorageUsedBytes = storageBytesChange,
                        UploadCount = uploadCountChange,
                        DownloadCount = downloadCountChange,
                        DeleteCount = deleteCountChange,
                        RecordedBy = "System" // Ideally this would come from the current user
                    };

                    dbContext.Add(stat);
                }
                else
                {
                    // Update existing record
                    stat.FileCount += fileCountChange;
                    stat.StorageUsedBytes += storageBytesChange;
                    stat.UploadCount += uploadCountChange;
                    stat.DownloadCount += downloadCountChange;
                    stat.DeleteCount += deleteCountChange;
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating usage statistics for category {CategoryId}", categoryId);
                // Don't rethrow to avoid failing the main process
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string category = "documents", string folderPath = "")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            using var stream = file.OpenReadStream();
            return await UploadFileAsync(stream, file.FileName, file.ContentType, category, folderPath);
        }

        public string GetCdnUrl(string path)
        {
            // If path is null or empty, return the base CDN URL
            string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
            if (string.IsNullOrEmpty(path))
                return cdnBaseUrl;

            // If path already starts with the CDN base URL, return it as is
            if (path.StartsWith(cdnBaseUrl, StringComparison.OrdinalIgnoreCase))
                return path;

            // Handle local development URLs
            if (_environment.IsDevelopment() && path.StartsWith("/cdn/"))
            {
                // Return the local URL as is
                return path;
            }

            // If path starts with a slash, remove it
            if (path.StartsWith("/"))
                path = path.Substring(1);

            // If path is now empty after removing the slash, return just the base URL
            if (string.IsNullOrEmpty(path))
                return cdnBaseUrl;

            return $"{cdnBaseUrl.TrimEnd('/')}/{path}";
        }

        // Improved file path resolution and file operation with retries
        private (string RelativePath, string PhysicalPath) ResolveFilePaths(string path)
        {
            _logger.LogInformation("Resolving path: {Path}", path);

            // Extract the relative path from the URL
            string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
            string relativePath = "";

            // Handle local development URLs
            if (_environment.IsDevelopment() && path.StartsWith("/cdn/"))
            {
                relativePath = path.Substring(5); // Remove "/cdn/"
                _logger.LogDebug("Development path detected, relative path: {RelativePath}", relativePath);
            }
            else if (path.StartsWith(cdnBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = path.Substring(cdnBaseUrl.Length).TrimStart('/');
                _logger.LogDebug("CDN URL detected, relative path: {RelativePath}", relativePath);
            }
            else if (path.StartsWith("http://") || path.StartsWith("https://"))
            {
                // Try to extract relative path from any URL by looking for category names
                var segments = path.Split('/');
                bool foundCategory = false;
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    if (_categories?.Any(c => c.Name.Equals(segments[i], StringComparison.OrdinalIgnoreCase)) == true)
                    {
                        foundCategory = true;
                        relativePath = string.Join("/", segments.Skip(i));
                        _logger.LogDebug("Found category in URL path, relative path: {RelativePath}", relativePath);
                        break;
                    }
                }

                if (!foundCategory)
                {
                    // If no category found, treat as relative path
                    relativePath = path.TrimStart('/');
                    _logger.LogDebug("No category found in URL, treating as relative path: {RelativePath}", relativePath);
                }
            }
            else
            {
                // If path doesn't start with the base URL, treat it as a relative path
                relativePath = path.TrimStart('/');
                _logger.LogDebug("Treating as relative path: {RelativePath}", relativePath);
            }

            // Get CDN storage path
            string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;

            // If in development and storage path doesn't exist, try local storage path
            if (_environment.IsDevelopment() &&
                (!Directory.Exists(cdnStoragePath) || !File.Exists(Path.Combine(cdnStoragePath, relativePath))))
            {
                string localPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "cdn");

                // If the file exists in the local path, use it
                if (Directory.Exists(localPath) && File.Exists(Path.Combine(localPath, relativePath)))
                {
                    cdnStoragePath = localPath;
                    _logger.LogDebug("Using local storage path: {Path}", cdnStoragePath);
                }
            }

            string physicalPath = Path.Combine(cdnStoragePath, relativePath);
            _logger.LogDebug("Resolved to physical path: {PhysicalPath}", physicalPath);

            return (relativePath, physicalPath);
        }

        // Verify file permissions before attempting operations
        private bool VerifyFileAccess(string path, FileAccess access)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                _logger.LogWarning("File does not exist: {Path}", path);
                return false;
            }

            try
            {
                // Try to open the file with the requested access to check permissions
                using (var fileStream = new FileStream(
                    path,
                    FileMode.Open,
                    access,
                    FileShare.ReadWrite,
                    4096,
                    FileOptions.None))
                {
                    _logger.LogDebug("File access verified for {Path} with {Access} access", path, access);
                    return true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Permission denied for file {Path} with {Access} access", path, access);
                return false;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "File is locked or in use: {Path}", path);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying file access for {Path}", path);
                return false;
            }
        }

        // Attempt file operation with retries
        private async Task<bool> TryFileOperationAsync(Func<Task<bool>> operation, string operationName, string path)
        {
            int attempts = 0;
            while (attempts < MaxRetryAttempts)
            {
                try
                {
                    _logger.LogDebug("Attempting {Operation} on {Path}, attempt {Attempt}/{MaxAttempts}",
                        operationName, path, attempts + 1, MaxRetryAttempts);

                    var result = await operation();

                    if (result)
                    {
                        _logger.LogInformation("{Operation} successful on {Path}", operationName, path);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("{Operation} returned false on {Path}", operationName, path);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "Permission denied for {Operation} on {Path}", operationName, path);
                    // Don't retry permission issues
                    return false;
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "IO Exception during {Operation} on {Path}, attempt {Attempt}/{MaxAttempts}",
                        operationName, path, attempts + 1, MaxRetryAttempts);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during {Operation} on {Path}, attempt {Attempt}/{MaxAttempts}",
                        operationName, path, attempts + 1, MaxRetryAttempts);
                }

                attempts++;
                if (attempts < MaxRetryAttempts)
                {
                    await Task.Delay(RetryDelay);
                }
            }

            _logger.LogError("Failed {Operation} on {Path} after {MaxAttempts} attempts",
                operationName, path, MaxRetryAttempts);
            return false;
        }

        // Improved delete file implementation
        public async Task<bool> DeleteFileAsync(string path)
        {
            // Check if we need to refresh the configuration
            await RefreshConfigIfNeededAsync();

            try
            {
                _logger.LogInformation("Deleting file: {Path}", path);

                // Resolve paths
                var (relativePath, physicalPath) = ResolveFilePaths(path);

                _logger.LogDebug("Delete file - Physical path: {Path}", physicalPath);

                // Update database record first to mark as deleted
                await MarkFileAsDeletedAsync(path);

                // Check if file exists and is accessible
                if (!File.Exists(physicalPath))
                {
                    _logger.LogWarning("File not found for deletion: {Path}", physicalPath);
                    return false;
                }

                if (!VerifyFileAccess(physicalPath, FileAccess.ReadWrite))
                {
                    _logger.LogWarning("File access denied for deletion: {Path}", physicalPath);

                    // Try alternate delete methods if permission issues
                    return await TryFileOperationAsync(
                        async () =>
                        {
                            try
                            {
                                // Try to use alternative delete method
                                if (File.Exists(physicalPath))
                                {
                                    File.SetAttributes(physicalPath, FileAttributes.Normal);
                                    File.Delete(physicalPath);
                                    await Task.CompletedTask; // Just to make it async
                                    return true;
                                }
                                return false;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Alternative delete method failed: {Path}", physicalPath);
                                return false;
                            }
                        },
                        "Alternative Delete",
                        physicalPath);
                }

                // Try to get file info before deletion (for statistics update)
                long fileSize = 0;
                try
                {
                    var fileInfo = new FileInfo(physicalPath);
                    fileSize = fileInfo.Length;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get file size for {Path}", physicalPath);
                }

                // Try to determine category from path
                string categoryName = relativePath.Split('/').FirstOrDefault() ?? "";
                var categoryId = await GetCategoryIdAsync(categoryName);

                // Delete with retry
                bool deleteSuccess = await TryFileOperationAsync(
                    async () =>
                    {
                        try
                        {
                            if (File.Exists(physicalPath))
                            {
                                // Reset any read-only attribute
                                File.SetAttributes(physicalPath, FileAttributes.Normal);
                                File.Delete(physicalPath);
                                await Task.CompletedTask; // Just to make it async
                                return true;
                            }
                            return false;
                        }
                        catch
                        {
                            return false;
                        }
                    },
                    "Delete",
                    physicalPath);

                if (deleteSuccess)
                {
                    _logger.LogInformation("Successfully deleted file: {Path}", physicalPath);

                    // Remove from cache if exists
                    _filePathCache.TryRemove(path, out _);
                    _cacheExpiry.TryRemove(path, out _);

                    // Update usage statistics
                    if (categoryId.HasValue && fileSize > 0)
                    {
                        await UpdateUsageStatisticsAsync(
                            categoryId.Value,
                            -1,
                            -fileSize,
                            0,
                            0,
                            1);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Path}", path);
                return false;
            }
        }

        // Improved rename file implementation
        public async Task<string> RenameFileAsync(string path, string newName)
        {
            try
            {
                _logger.LogInformation("Renaming file: {Path} to {NewName}", path, newName);

                // Check if we need to refresh the configuration
                await RefreshConfigIfNeededAsync();

                // Resolve paths
                var (relativePath, oldFullPath) = ResolveFilePaths(path);

                _logger.LogDebug("Rename file - Old physical path: {Path}", oldFullPath);

                // Get directory, old filename, and extension
                string directory = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
                string oldFileName = Path.GetFileName(relativePath);
                string extension = Path.GetExtension(oldFileName);

                // If newName already has the extension, don't add it again
                if (!newName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    newName = $"{newName}{extension}";
                }

                string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;

                // If in development and storage path doesn't exist, try local storage path
                if (_environment.IsDevelopment() &&
                    (!Directory.Exists(cdnStoragePath) || !File.Exists(oldFullPath)))
                {
                    cdnStoragePath = Path.Combine(_environment.ContentRootPath, "wwwroot", "cdn");
                }

                string newRelativePath = string.IsNullOrEmpty(directory) ?
                    newName :
                    Path.Combine(directory, newName).Replace('\\', '/');
                string newFullPath = Path.Combine(cdnStoragePath, newRelativePath);

                _logger.LogDebug("Rename file - New physical path: {Path}", newFullPath);

                // Check if the old file exists and is accessible
                if (!File.Exists(oldFullPath))
                {
                    _logger.LogWarning("File not found for rename: {Path}", oldFullPath);
                    return null;
                }

                if (!VerifyFileAccess(oldFullPath, FileAccess.ReadWrite))
                {
                    _logger.LogWarning("File access denied for rename: {Path}", oldFullPath);
                    return null;
                }

                // Check if the new filename already exists
                if (File.Exists(newFullPath))
                {
                    _logger.LogWarning("Destination file already exists: {Path}", newFullPath);
                    throw new Exception($"A file with the name '{newName}' already exists in this location.");
                }

                // Ensure the directory for the new path exists
                string newDir = Path.GetDirectoryName(newFullPath);
                if (!string.IsNullOrEmpty(newDir) && !Directory.Exists(newDir))
                {
                    try
                    {
                        Directory.CreateDirectory(newDir);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create directory for rename: {Path}", newDir);
                        throw new Exception($"Failed to create directory for the renamed file: {ex.Message}", ex);
                    }
                }

                // Rename with retry
                bool renameSuccess = await TryFileOperationAsync(
                    async () =>
                    {
                        try
                        {
                            if (File.Exists(oldFullPath))
                            {
                                // Reset any read-only attribute
                                File.SetAttributes(oldFullPath, FileAttributes.Normal);

                                // Use copy + delete as a more reliable alternative to Move
                                File.Copy(oldFullPath, newFullPath, false);
                                File.Delete(oldFullPath);
                                await Task.CompletedTask; // Just to make it async
                                return true;
                            }
                            return false;
                        }
                        catch
                        {
                            return false;
                        }
                    },
                    "Rename",
                    oldFullPath);

                if (!renameSuccess)
                {
                    _logger.LogError("Failed to rename file after multiple attempts: {Path}", oldFullPath);
                    return null;
                }

                _logger.LogInformation("Successfully renamed file from {OldPath} to {NewPath}", oldFullPath, newFullPath);

                // Create new URL
                string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
                string newUrl = $"{cdnBaseUrl.TrimEnd('/')}/{newRelativePath}";

                // Update database record
                await UpdateFileMetadataAsync(path, newUrl, newName);

                // Remove from cache if exists
                _filePathCache.TryRemove(path, out _);
                _cacheExpiry.TryRemove(path, out _);

                return newUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming file: {Path} to {NewName}", path, newName);
                throw new Exception($"Failed to rename file: {ex.Message}", ex);
            }
        }

        private async Task UpdateFileMetadataAsync(string oldUrl, string newUrl, string newFileName)
        {
            try
            {
                // Create a scope to resolve the DbContext
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                // Find the file metadata
                var metadata = await dbContext.Set<CdnFileMetadata>()
                    .FirstOrDefaultAsync(f => f.Url == oldUrl && !f.IsDeleted);

                if (metadata != null)
                {
                    // Update with new information
                    metadata.Url = newUrl;
                    metadata.FileName = newFileName;
                    // metadata.UpdatedDate = DateTime.Now;

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Updated file metadata for rename: {OldUrl} -> {NewUrl}", oldUrl, newUrl);
                }
                else
                {
                    // Try finding by matching the ending portion of the URL
                    var allFiles = await dbContext.Set<CdnFileMetadata>()
                        .Where(f => !f.IsDeleted)
                        .ToListAsync();

                    var match = allFiles.FirstOrDefault(f => oldUrl.EndsWith(f.Url.TrimStart('/')) || f.Url.EndsWith(oldUrl.TrimStart('/')));

                    if (match != null)
                    {
                        match.Url = newUrl;
                        match.FileName = newFileName;
                        //  match.UpdatedDate = DateTime.Now;

                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Updated file metadata for rename (partial URL match): {OldUrl} -> {NewUrl}", match.Url, newUrl);
                    }
                    else
                    {
                        _logger.LogWarning("File metadata not found in database for rename: {Url}", oldUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file metadata for rename: {Url}", oldUrl);
                // Don't rethrow to avoid failing the rename process
            }
        }

        private async Task<int?> GetCategoryIdAsync(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return null;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                var category = await dbContext.Set<CdnCategory>()
                    .FirstOrDefaultAsync(c => c.Name == categoryName && c.IsActive);

                return category?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category ID for {CategoryName}", categoryName);
                return null;
            }
        }

        private async Task MarkFileAsDeletedAsync(string url)
        {
            try
            {
                // Create a scope to resolve the DbContext
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                // Find the file metadata
                var metadata = await dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.Url == url && !f.IsDeleted)
                    .FirstOrDefaultAsync();

                if (metadata != null)
                {
                    // Mark as deleted
                    metadata.IsDeleted = true;
                    // metadata.DeletedDate = DateTime.Now;
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Marked file as deleted in database: {Url}", url);
                }
                else
                {
                    // Try finding by matching the ending portion of the URL
                    var allFiles = await dbContext.Set<CdnFileMetadata>()
                        .Where(f => !f.IsDeleted)
                        .ToListAsync();

                    var match = allFiles.FirstOrDefault(f => url.EndsWith(f.Url.TrimStart('/')) || f.Url.EndsWith(url.TrimStart('/')));

                    if (match != null)
                    {
                        match.IsDeleted = true;
                        // match.DeletedDate = DateTime.Now;
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Marked file as deleted in database (partial URL match): {Url}", match.Url);
                    }
                    else
                    {
                        _logger.LogWarning("File metadata not found in database: {Url}", url);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking file as deleted: {Url}", url);
                // Don't rethrow to avoid failing the deletion process
            }
        }

        public string GetApiKey()
        {
            // If _cdnConfig is null or ApiKey is empty, use the fallback
            if (_cdnConfig == null || string.IsNullOrEmpty(_cdnConfig.ApiKey))
            {
                return _configuration["CDN:ApiKey"] ?? "RooviaCDNKey";
            }
            return _cdnConfig.ApiKey;
        }

        public string GetPhysicalPath(string cdnUrl)
        {
            if (IsDirectAccessAvailable())
            {
                // Check cache first for better performance
                if (_filePathCache.TryGetValue(cdnUrl, out var cachedPath))
                {
                    // Check if cache is expired
                    if (_cacheExpiry.TryGetValue(cdnUrl, out var expiry) && expiry > DateTime.UtcNow)
                    {
                        return cachedPath;
                    }
                }

                // Get storage path
                string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;

                // Handle development environment URLs
                if (_environment.IsDevelopment())
                {
                    if (cdnUrl.StartsWith("/cdn/"))
                    {
                        // Local development URL
                        string relativePath2 = cdnUrl.Substring(5); // Remove "/cdn/"
                        string localPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "cdn", relativePath2);

                        // Cache the result
                        _filePathCache[cdnUrl] = localPath;
                        _cacheExpiry[cdnUrl] = DateTime.UtcNow.Add(_cacheDuration);

                        return localPath;
                    }
                }

                // Use the more robust path resolution
                var (_, physicalPath) = ResolveFilePaths(cdnUrl);

                // Cache the result
                _filePathCache[cdnUrl] = physicalPath;
                _cacheExpiry[cdnUrl] = DateTime.UtcNow.Add(_cacheDuration);

                return physicalPath;
            }

            // In development, try to use local storage
            if (_environment.IsDevelopment())
            {
                if (cdnUrl.StartsWith("/cdn/"))
                {
                    // Local development URL
                    string relativePath2 = cdnUrl.Substring(5); // Remove "/cdn/"
                    return Path.Combine(_environment.ContentRootPath, "wwwroot", "cdn", relativePath2);
                }

                // Extract the relative path from the URL
                string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
                string relativePath = cdnUrl.Replace(cdnBaseUrl, "").TrimStart('/');

                return Path.Combine(_environment.ContentRootPath, "wwwroot", "cdn", relativePath);
            }

            return null;
        }

        public Stream GetFileStream(string cdnUrl)
        {
            if (IsDirectAccessAvailable())
            {
                var physicalPath = GetPhysicalPath(cdnUrl);
                if (!string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath))
                {
                    // Use optimized file stream settings for better performance
                    return new FileStream(
                        physicalPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        LargeBufferSize,
                        FileOptions.Asynchronous | FileOptions.SequentialScan);
                }
            }

            // In development, try to use local storage
            if (_environment.IsDevelopment())
            {
                if (cdnUrl.StartsWith("/cdn/"))
                {
                    // Local development URL
                    string relativePath = cdnUrl.Substring(5); // Remove "/cdn/"
                    string localPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "cdn", relativePath);

                    if (File.Exists(localPath))
                    {
                        return new FileStream(
                            localPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            LargeBufferSize,
                            FileOptions.Asynchronous | FileOptions.SequentialScan);
                    }
                }
            }

            return null;
        }

        public bool IsDevEnvironment()
        {
            return _environment.IsDevelopment();
        }

        public async Task<List<CdnCategory>> GetCategoriesAsync()
        {
            // Check cache first
            if (_memoryCache.TryGetValue(CATEGORIES_CACHE_KEY, out List<CdnCategory> cachedCategories))
            {
                return cachedCategories;
            }

            // Check if we need to refresh the configuration
            await RefreshConfigIfNeededAsync();

            // If we have categories loaded, return them
            if (_categories != null && _categories.Any())
            {
                return _categories;
            }

            // Otherwise, load from database
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                var categories = await dbContext.Set<CdnCategory>()
                    .Where(c => c.IsActive)
                    .ToListAsync();

                if (categories.Any())
                {
                    // Update cache
                    _memoryCache.Set(CATEGORIES_CACHE_KEY, categories, TimeSpan.FromMinutes(15));
                    _categories = categories;
                    return categories;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories from database");
            }

            // If no categories found or error occurred, return default ones
            var defaultCategories = new List<CdnCategory>
            {
                new CdnCategory { Id = 1, Name = "documents", DisplayName = "Documents", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.txt" },
                new CdnCategory { Id = 2, Name = "images", DisplayName = "Images", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.webp,.svg" },
                new CdnCategory { Id = 3, Name = "test-uploads", DisplayName = "Test Uploads", AllowedFileTypes = "*" },
            };

            // Try to save default categories to database
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                foreach (var category in defaultCategories)
                {
                    if (!await dbContext.Set<CdnCategory>().AnyAsync(c => c.Name == category.Name))
                    {
                        var newCategory = new CdnCategory
                        {
                            Name = category.Name,
                            DisplayName = category.DisplayName,
                            AllowedFileTypes = category.AllowedFileTypes,
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            CreatedBy = "System"
                        };

                        dbContext.Add(newCategory);
                    }
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Saved default categories to database");

                // Reload categories
                var savedCategories = await dbContext.Set<CdnCategory>()
                    .Where(c => c.IsActive)
                    .ToListAsync();

                if (savedCategories.Any())
                {
                    _memoryCache.Set(CATEGORIES_CACHE_KEY, savedCategories, TimeSpan.FromMinutes(15));
                    _categories = savedCategories;
                    return savedCategories;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving default categories to database");
            }

            return defaultCategories;
        }

        public async Task<List<CdnFolder>> GetFoldersAsync(string category)
        {
            try
            {
                // Ensure the category is valid
                category = await ValidateCategoryAsync(category);

                // Get category ID
                var categoryId = await GetCategoryIdAsync(category);
                if (!categoryId.HasValue)
                {
                    _logger.LogWarning("Category not found for folder lookup: {Category}", category);
                    return new List<CdnFolder>();
                }

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                var folders = await dbContext.Set<CdnFolder>()
                    .Where(f => f.CategoryId == categoryId && f.IsActive)
                    .OrderBy(f => f.Path)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} folders for category {Category}", folders.Count, category);
                return folders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving folders for category {Category}", category);
                return new List<CdnFolder>();
            }
        }

        public async Task<List<CdnFileMetadata>> GetFilesAsync(string category = "documents", string folderPath = null, string searchTerm = null)
        {
            try
            {
                // Ensure the category is valid
                category = await ValidateCategoryAsync(category);

                // Get category ID
                var categoryId = await GetCategoryIdAsync(category);
                if (!categoryId.HasValue)
                {
                    _logger.LogWarning("Category not found for file lookup: {Category}", category);
                    return new List<CdnFileMetadata>();
                }

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                // Start with base query
                var query = dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.CategoryId == categoryId && !f.IsDeleted);

                // If folder path is specified, get the folder ID
                if (!string.IsNullOrEmpty(folderPath))
                {
                    folderPath = CleanFolderPath(folderPath);

                    var folder = await dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryId && f.Path == folderPath && f.IsActive);

                    if (folder != null)
                    {
                        query = query.Where(f => f.FolderId == folder.Id);
                        _logger.LogDebug("Found folder {FolderPath}, ID: {FolderId}", folderPath, folder.Id);
                    }
                    else
                    {
                        // If folder doesn't exist, return empty list
                        _logger.LogWarning("Folder not found: {Category}/{FolderPath}", category, folderPath);
                        return new List<CdnFileMetadata>();
                    }
                }
                else if (folderPath == "") // Root folder specifically
                {
                    query = query.Where(f => f.FolderId == null);
                    _logger.LogDebug("Looking up files in root folder of category {Category}", category);
                }

                // If search term is specified, filter by filename
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(f => f.FileName.Contains(searchTerm));
                    _logger.LogDebug("Applied search filter: {SearchTerm}", searchTerm);
                }

                var files = await query.OrderByDescending(f => f.UploadDate).ToListAsync();
                _logger.LogInformation("Found {Count} files for category {Category}, folder {Folder}",
                    files.Count, category, folderPath ?? "root");

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for category {Category}, folder {Folder}",
                    category, folderPath ?? "root");
                return new List<CdnFileMetadata>();
            }
        }

        private async Task<string> ValidateCategoryAsync(string category)
        {
            if (string.IsNullOrEmpty(category))
                return "documents";

            // Check if the category exists in the database
            var categories = await GetCategoriesAsync();

            if (categories.Any(c => c.Name.Equals(category, StringComparison.OrdinalIgnoreCase)))
                return category.ToLowerInvariant();

            // Check if it's a standard category
            string[] standardCategories = { "documents", "images", "hr", "weighbridge", "lab", "test-uploads" };
            if (standardCategories.Contains(category.ToLowerInvariant()))
                return category.ToLowerInvariant();

            // Default to documents if not found
            return "documents";
        }

        private string GenerateUniqueFileName(string fileName)
        {
            // Remove any potentially dangerous characters from the filename
            string extension = Path.GetExtension(fileName);

            // Process the file name to make it safe
            var safeName = Path.GetFileNameWithoutExtension(fileName)
                .Replace(" ", "-")
                .Replace("_", "-");

            // Remove any invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                safeName = safeName.Replace(c.ToString(), "");
            }

            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = "file";
            }

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = Guid.NewGuid().ToString().Substring(0, 8);

            return $"{safeName}_{timestamp}_{random}{extension}";
        }

        private async Task RefreshConfigIfNeededAsync()
        {
            // Check if we need to refresh the database configuration
            if (_configLastRefreshed.Add(_configRefreshInterval) < DateTime.Now || _cdnConfig == null)
            {
                await LoadConfigFromDatabaseAsync();
            }
        }

        // These classes are used for JSON deserialization - kept for backward compatibility
        private class UploadResult
        {
            public bool Success { get; set; }
            public string? Url { get; set; }
            public string? FileName { get; set; }
            public string? ContentType { get; set; }
            public long? Size { get; set; }
            public string? Category { get; set; }
            public string? Message { get; set; }
        }

        private class ApiResponse<T>
        {
            public bool? success { get; set; }
            public T? data { get; set; }
            public string? message { get; set; }
        }
    }
}