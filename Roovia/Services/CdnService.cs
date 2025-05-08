using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

        // Production API URL - always use this in test/dev environments
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

        // HttpClient optimized for CDN uploads
        private readonly HttpClient _optimizedHttpClient;

        // Database configuration 
        private CdnConfiguration _cdnConfig;
        private List<CdnCategory> _categories;
        private DateTime _configLastRefreshed;
        private readonly TimeSpan _configRefreshInterval = TimeSpan.FromMinutes(5);

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

            // Set production API URL - always use this in test/dev environments
            _productionApiUrl = configuration["CDN:ProductionApiUrl"] ?? "https://portal.roovia.co.za/api/cdn";

            // Force remote API usage in test/dev environments (default to true)
            _forceRemoteApiInTestEnv = configuration.GetValue<bool>("CDN:ForceRemoteApiInTestEnv", true);

            _logger.LogInformation("CdnService initialized with production API URL: {ProductionApiUrl}, ForceRemoteApi: {ForceRemote}",
                _productionApiUrl, _forceRemoteApiInTestEnv);

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
                if (!_forceRemoteApiInTestEnv || !IsDevEnvironment())
                {
                    EnsureCategoryDirectoriesExist();
                }

                // Update the HttpClient API key
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

                // Ensure storage directory exists for each category if not using remote API
                if (!_forceRemoteApiInTestEnv || !IsDevEnvironment())
                {
                    EnsureCategoryDirectoriesExist();
                }
            }
        }

        private void EnsureCategoryDirectoriesExist()
        {
            if (_categories == null || _cdnConfig == null)
                return;

            string storagePath = _cdnConfig.StoragePath;
            if (string.IsNullOrEmpty(storagePath) || !Directory.Exists(storagePath))
            {
                storagePath = _bootstrapCdnStoragePath;
                if (string.IsNullOrEmpty(storagePath))
                    return;

                // Try to create the bootstrap storage path if it doesn't exist
                if (!Directory.Exists(storagePath))
                {
                    try
                    {
                        Directory.CreateDirectory(storagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to create bootstrap storage path: {Error}", ex.Message);
                        return;
                    }
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

        // Modified detection method
        public bool IsDirectAccessAvailable()
        {
            // When forcing remote API in a test/dev environment, always return false
            if (IsDevEnvironment() && _forceRemoteApiInTestEnv)
            {
                return false;
            }

            // Get the storage path from config
            string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;

            // Check if storage path exists
            bool pathExists = !string.IsNullOrEmpty(cdnStoragePath) && Directory.Exists(cdnStoragePath);

            // If in development, we might not have direct access to the production storage
            // but we can still use local storage
            if (IsDevEnvironment() && !pathExists && !_forceRemoteApiInTestEnv)
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

        public async Task<bool> DeleteFileAsync(string path)
        {
            // Check if we need to refresh the configuration
            await RefreshConfigIfNeededAsync();

            try
            {
                _logger.LogInformation("Deleting file: {Path}", path);

                // Check if we're in a test environment and should force remote API
                bool forceRemoteApi = IsDevEnvironment() && _forceRemoteApiInTestEnv;

                string relativePathForDb = path;

                // Extract the relative path from the URL
                string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
                string relativePath;

                // Handle local development URLs
                if (_environment.IsDevelopment() && path.StartsWith("/cdn/"))
                {
                    relativePath = path.Substring(5); // Remove "/cdn/"
                }
                else
                {
                    relativePath = path.Replace(cdnBaseUrl, "").TrimStart('/');
                }

                _logger.LogDebug("Extracted relative path: {RelativePath}, ForceRemote: {ForceRemote}",
                    relativePath, forceRemoteApi);

                if (IsDirectAccessAvailable() && !forceRemoteApi)
                {
                    // Direct file system access
                    string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;

                    // If in development and storage path doesn't exist, try local storage path
                    if (_environment.IsDevelopment() && (!Directory.Exists(cdnStoragePath) || !File.Exists(Path.Combine(cdnStoragePath, relativePath))))
                    {
                        cdnStoragePath = Path.Combine(_environment.ContentRootPath, "wwwroot", "cdn");
                    }

                    string fullPath = Path.Combine(cdnStoragePath, relativePath);

                    _logger.LogDebug("Physical path for delete: {Path}", fullPath);

                    // Update database record first to mark as deleted
                    await MarkFileAsDeletedAsync(relativePathForDb);

                    if (File.Exists(fullPath))
                    {
                        // Get file info before deletion
                        var fileInfo = new FileInfo(fullPath);

                        // Try to determine category from path
                        string categoryName = relativePath.Split('/')[0];
                        var categoryId = await GetCategoryIdAsync(categoryName);

                        // Delete the file
                        File.Delete(fullPath);

                        _logger.LogInformation("Successfully deleted file: {Path}", fullPath);

                        // Remove from cache if exists
                        _filePathCache.TryRemove(path, out _);
                        _cacheExpiry.TryRemove(path, out _);

                        // Update usage statistics
                        if (categoryId.HasValue)
                        {
                            await UpdateUsageStatisticsAsync(
                                categoryId.Value,
                                -1,
                                -fileInfo.Length,
                                0,
                                0,
                                1);
                        }

                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("File not found for deletion: {Path}", fullPath);
                    }

                    return false;
                }
                else
                {
                    _logger.LogInformation("Using remote API for file deletion");

                    // Remote API access
                    string apiUrl = $"{_productionApiUrl.TrimEnd('/')}/delete?path={Uri.EscapeDataString(path)}";

                    // Ensure API key is set
                    if (!_optimizedHttpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
                    {
                        _optimizedHttpClient.DefaultRequestHeaders.Add("X-Api-Key", GetApiKey());
                    }

                    var response = await _optimizedHttpClient.DeleteAsync(apiUrl);

                    // Update file metadata in database if successful
                    if (response.IsSuccessStatusCode)
                    {
                        await MarkFileAsDeletedAsync(relativePathForDb);
                        _logger.LogInformation("File deleted successfully via remote API");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete file via remote API: {StatusCode}", response.StatusCode);
                    }

                    // Remove from cache if exists
                    _filePathCache.TryRemove(path, out _);
                    _cacheExpiry.TryRemove(path, out _);

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Path}", path);
                return false;
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
                    .Where(f => f.Url == url)
                    .FirstOrDefaultAsync();

                if (metadata != null)
                {
                    // Mark as deleted
                    metadata.IsDeleted = true;
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Marked file as deleted in database: {Url}", url);
                }
                else
                {
                    _logger.LogWarning("File metadata not found in database: {Url}", url);
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
            // When forcing API in test/dev, return null
            if (IsDevEnvironment() && _forceRemoteApiInTestEnv)
            {
                return null;
            }

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

                // Extract the relative path from the URL
                string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
                string relativePath = cdnUrl.Replace(cdnBaseUrl, "").TrimStart('/');

                var physicalPath = Path.Combine(cdnStoragePath, relativePath);

                // Cache the result
                _filePathCache[cdnUrl] = physicalPath;
                _cacheExpiry[cdnUrl] = DateTime.UtcNow.Add(_cacheDuration);

                return physicalPath;
            }

            // In development, try to use local storage
            if (_environment.IsDevelopment() && !_forceRemoteApiInTestEnv)
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
            // When forcing API in test/dev, return null
            if (IsDevEnvironment() && _forceRemoteApiInTestEnv)
            {
                return null;
            }

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
            if (_environment.IsDevelopment() && !_forceRemoteApiInTestEnv)
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

                // Check if we're in a test environment and should force remote API
                bool forceRemoteApi = IsDevEnvironment() && _forceRemoteApiInTestEnv;

                if (forceRemoteApi)
                {
                    // Use the production API to get folders
                    string apiUrl = $"{_productionApiUrl.TrimEnd('/')}/folders?category={category}";

                    // Ensure API key is set in headers
                    if (!_optimizedHttpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
                    {
                        _optimizedHttpClient.DefaultRequestHeaders.Add("X-Api-Key", GetApiKey());
                    }

                    var response = await _optimizedHttpClient.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CdnFolder>>>();
                        if (result?.success == true && result.data != null)
                        {
                            return result.data;
                        }
                    }

                    _logger.LogWarning("Failed to get folders from production API: {StatusCode}", response.StatusCode);
                    return new List<CdnFolder>();
                }

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

                // Check if we're in a test environment and should force remote API
                bool forceRemoteApi = IsDevEnvironment() && _forceRemoteApiInTestEnv;

                if (forceRemoteApi)
                {
                    // Use the production API to get files
                    string apiUrl = $"{_productionApiUrl.TrimEnd('/')}/files?category={category}";

                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        apiUrl += $"&folder={Uri.EscapeDataString(folderPath)}";
                    }

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        apiUrl += $"&search={Uri.EscapeDataString(searchTerm)}";
                    }

                    // Ensure API key is set in headers
                    if (!_optimizedHttpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
                    {
                        _optimizedHttpClient.DefaultRequestHeaders.Add("X-Api-Key", GetApiKey());
                    }

                    var response = await _optimizedHttpClient.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CdnFileMetadata>>>();
                        if (result?.success == true && result.data != null)
                        {
                            return result.data;
                        }
                    }

                    _logger.LogWarning("Failed to get files from production API: {StatusCode}", response.StatusCode);
                    return new List<CdnFileMetadata>();
                }

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