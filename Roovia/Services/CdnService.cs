// Services/CdnService.cs
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

            // Get bootstrap configuration from appsettings.json (only used first time)
            _bootstrapCdnBaseUrl = configuration["CDN:BaseUrl"];
            _bootstrapCdnStoragePath = configuration["CDN:StoragePath"];
            _bootstrapApiKey = configuration["CDN:ApiKey"];

            // Create an optimized HttpClient for CDN operations with longer timeout
            _optimizedHttpClient = _httpClientFactory.CreateClient("CdnClient");
            _optimizedHttpClient.Timeout = TimeSpan.FromMinutes(10); // 10 minute timeout for large file uploads

            // Load the database configuration asynchronously
            Task.Run(async () => await LoadConfigFromDatabaseAsync());
        }

        private async Task LoadConfigFromDatabaseAsync()
        {
            try
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
                }

                // Load categories
                var categories = await dbContext.Set<CdnCategory>()
                    .Where(c => c.IsActive)
                    .ToListAsync();

                if (categories.Any())
                {
                    _memoryCache.Set(CATEGORIES_CACHE_KEY, categories, TimeSpan.FromMinutes(15));
                    _categories = categories;
                }
                else
                {
                    // Create default categories if none exist
                    _categories = new List<CdnCategory>
                    {
                        new CdnCategory { Name = "documents", DisplayName = "Documents", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.txt" },
                        new CdnCategory { Name = "images", DisplayName = "Images", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.webp,.svg" },
                    };
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
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string category = "documents", string folderPath = "")
        {
            // Check if we need to refresh the configuration
            await RefreshConfigIfNeededAsync();

            try
            {
                // Ensure category is valid
                category = await ValidateCategoryAsync(category);

                // Generate a unique filename to prevent collisions
                var uniqueFileName = GenerateUniqueFileName(fileName);

                // Create relative path with optional folder
                var relativePath = string.IsNullOrEmpty(folderPath)
                    ? Path.Combine(category, uniqueFileName)
                    : Path.Combine(category, folderPath, uniqueFileName);

                // Modified decision logic - direct access is ONLY for production environment
                // and only when the storage path is directly accessible
                bool useDirectAccess = IsDirectAccessAvailable() && !_environment.IsDevelopment();

                if (useDirectAccess)
                {
                    // Create the physical directory if it doesn't exist
                    string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;
                    var directoryPath = string.IsNullOrEmpty(folderPath)
                        ? Path.Combine(cdnStoragePath, category)
                        : Path.Combine(cdnStoragePath, category, folderPath);

                    // Create all directories in the path
                    Directory.CreateDirectory(directoryPath);

                    // Save file to disk with optimized large buffer size
                    var filePath = Path.Combine(directoryPath, uniqueFileName);

                    // Use FileOptions.Asynchronous and SequentialScan for optimal performance
                    using (var fileStream2 = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        LargeBufferSize,
                        FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        await fileStream.CopyToAsync(fileStream2, LargeBufferSize);
                    }

                    // Get folder ID from database or create if needed
                    var folderId = await GetOrCreateFolderAsync(category, folderPath);

                    // Add metadata to database
                    await SaveFileMetadataAsync(category, uniqueFileName, filePath, contentType, new FileInfo(filePath).Length, folderId, folderPath);
                }
                else
                {
                    // We're on a different server or in development mode, so use the API
                    using var streamContent = new StreamContent(fileStream, LargeBufferSize);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                    // Prepare multipart form content
                    using var content = new MultipartFormDataContent();
                    content.Add(streamContent, "file", fileName);
                    content.Add(new StringContent(category), "category");

                    // Add folder path if provided
                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        content.Add(new StringContent(folderPath), "folder");
                    }

                    // Use the configured production URL instead of hardcoded one
                    // Get the base URL from configuration, defaulting to production if not specified
                    var productionApiUrl = _configuration["CDN:ProductionApiUrl"] ?? "https://portal.roovia.co.za/api/cdn/upload";

                    // Ensure API key is set in headers
                    if (!_optimizedHttpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
                    {
                        _optimizedHttpClient.DefaultRequestHeaders.Add("X-Api-Key", GetApiKey());
                    }

                    var response = await _optimizedHttpClient.PostAsync(productionApiUrl, content);
                    response.EnsureSuccessStatusCode();

                    // Get the URL from the response
                    var result = await response.Content.ReadFromJsonAsync<UploadResult>();

                    // Return the URL from the API response
                    return result.url;
                }

                // Return the public URL with the CDN domain
                string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
                return string.IsNullOrEmpty(folderPath)
                    ? $"{cdnBaseUrl.TrimEnd('/')}/{category}/{uniqueFileName}"
                    : $"{cdnBaseUrl.TrimEnd('/')}/{category}/{folderPath.TrimStart('/').TrimEnd('/')}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
                throw new Exception($"Failed to upload file: {ex.Message}", ex);
            }
        }

        // Modified detection method
        public bool IsDirectAccessAvailable()
        {
            string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;
            return !string.IsNullOrEmpty(cdnStoragePath) && Directory.Exists(cdnStoragePath);
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
                // Extract the relative path from the URL
                string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
                string relativePath = path.Replace(cdnBaseUrl, "").TrimStart('/');

                if (IsDirectAccessAvailable())
                {
                    // Direct file system access
                    string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;
                    string fullPath = Path.Combine(cdnStoragePath, relativePath);

                    // Update database record first to mark as deleted
                    await MarkFileAsDeletedAsync(path);

                    if (File.Exists(fullPath))
                    {
                        // Get file info before deletion
                        var fileInfo = new FileInfo(fullPath);

                        // Try to determine category from path
                        string categoryName = relativePath.Split('/')[0];
                        var categoryId = await GetCategoryIdAsync(categoryName);

                        // Delete the file
                        File.Delete(fullPath);

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

                    return false;
                }
                else
                {
                    // Remote API access
                    var apiUrl = $"https://portal.roovia.co.za/api/cdn/delete?path={Uri.EscapeDataString(path)}";
                    var response = await _optimizedHttpClient.DeleteAsync(apiUrl);

                    // Update file metadata in database if successful
                    if (response.IsSuccessStatusCode)
                    {
                        await MarkFileAsDeletedAsync(path);
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

                // Extract the relative path from the URL
                string cdnBaseUrl = _cdnConfig?.BaseUrl ?? _bootstrapCdnBaseUrl;
                string relativePath = cdnUrl.Replace(cdnBaseUrl, "").TrimStart('/');
                string cdnStoragePath = _cdnConfig?.StoragePath ?? _bootstrapCdnStoragePath;
                var physicalPath = Path.Combine(cdnStoragePath, relativePath);

                // Cache the result
                _filePathCache[cdnUrl] = physicalPath;
                _cacheExpiry[cdnUrl] = DateTime.UtcNow.Add(_cacheDuration);

                return physicalPath;
            }
            return null;
        }

        public Stream GetFileStream(string cdnUrl)
        {
            if (IsDirectAccessAvailable())
            {
                var physicalPath = GetPhysicalPath(cdnUrl);
                if (File.Exists(physicalPath))
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
                    return categories;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories from database");
            }

            // If no categories found or error occurred, return default ones
            return new List<CdnCategory>
            {
                new CdnCategory { Id = 1, Name = "documents", DisplayName = "Documents", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.txt" },
                new CdnCategory { Id = 2, Name = "images", DisplayName = "Images", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.webp,.svg" },
                new CdnCategory { Id = 3, Name = "hr", DisplayName = "HR", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx" },
                new CdnCategory { Id = 4, Name = "weighbridge", DisplayName = "Weighbridge", AllowedFileTypes = ".pdf,.xls,.xlsx,.csv" },
                new CdnCategory { Id = 5, Name = "lab", DisplayName = "Laboratory", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.jpg,.jpeg,.png" }
            };
        }

        public async Task<List<CdnFolder>> GetFoldersAsync(string category)
        {
            try
            {
                var categoryId = await GetCategoryIdAsync(category);
                if (!categoryId.HasValue)
                    return new List<CdnFolder>();

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                var folders = await dbContext.Set<CdnFolder>()
                    .Where(f => f.CategoryId == categoryId && f.IsActive)
                    .ToListAsync();

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
                // Get category ID
                var categoryId = await GetCategoryIdAsync(category);
                if (!categoryId.HasValue)
                    return new List<CdnFileMetadata>();

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                // Start with base query
                var query = dbContext.Set<CdnFileMetadata>()
                    .Where(f => f.CategoryId == categoryId && !f.IsDeleted);

                // If folder path is specified, get the folder ID
                if (!string.IsNullOrEmpty(folderPath))
                {
                    var folder = await dbContext.Set<CdnFolder>()
                        .FirstOrDefaultAsync(f => f.CategoryId == categoryId && f.Path == folderPath);

                    if (folder != null)
                    {
                        query = query.Where(f => f.FolderId == folder.Id);
                    }
                    else
                    {
                        // If folder doesn't exist, return empty list
                        return new List<CdnFileMetadata>();
                    }
                }
                else if (folderPath == "") // Root folder specifically
                {
                    query = query.Where(f => f.FolderId == null);
                }

                // If search term is specified, filter by filename
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(f => f.FileName.Contains(searchTerm));
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for category {Category}", category);
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

            // Default to documents if not found
            return "documents";
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

        private async Task RefreshConfigIfNeededAsync()
        {
            // Check if we need to refresh the database configuration
            if (_configLastRefreshed.Add(_configRefreshInterval) < DateTime.Now)
            {
                await LoadConfigFromDatabaseAsync();
            }
        }

        private class UploadResult
        {
            public bool success { get; set; }
            public string url { get; set; }
            public string fileName { get; set; }
            public string contentType { get; set; }
            public long size { get; set; }
            public string category { get; set; }
        }
    }
}