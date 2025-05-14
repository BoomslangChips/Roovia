//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Roovia.Data;
//using Roovia.Interfaces;
//using Roovia.Models.CDN;

//namespace Roovia.Controllers
//{
//    [ApiController]
//    [Route("api/diag")]
//    public class DiagnosticsController : ControllerBase
//    {
//        private readonly ILogger<DiagnosticsController> _logger;
//        private readonly ICdnService _cdnService;
//        private readonly IConfiguration _configuration;
//        private readonly ApplicationDbContext _dbContext;

//        public DiagnosticsController(
//            ILogger<DiagnosticsController> logger,
//            ICdnService cdnService,
//            IConfiguration configuration,
//            ApplicationDbContext dbContext)
//        {
//            _logger = logger;
//            _cdnService = cdnService;
//            _configuration = configuration;
//            _dbContext = dbContext;
//        }

//        [HttpGet("ping")]
//        public IActionResult Ping()
//        {
//            return Ok(new
//            {
//                success = true,
//                message = "Diagnostics controller ping successful",
//                timestamp = DateTime.Now,
//                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
//            });
//        }

//        [HttpGet("environment")]
//        public IActionResult GetEnvironmentInfo()
//        {
//            return Ok(new
//            {
//                success = true,
//                osVersion = Environment.OSVersion.ToString(),
//                machineName = Environment.MachineName,
//                userName = Environment.UserName,
//                processPath = Environment.ProcessPath,
//                currentDirectory = Environment.CurrentDirectory,
//                aspnetEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
//                processId = Environment.ProcessId,
//                is64BitProcess = Environment.Is64BitProcess,
//                is64BitOperatingSystem = Environment.Is64BitOperatingSystem
//            });
//        }

//        [HttpGet("cdn")]
//        public IActionResult GetCdnInfo()
//        {
//            var results = new
//            {
//                configuredStoragePath = _configuration["CDN:StoragePath"],
//                configuredBaseUrl = _configuration["CDN:BaseUrl"],
//                configuredApiKey = MaskKey(_configuration["CDN:ApiKey"]),
//                serviceCdnPath = _cdnService.GetPhysicalPath(""),
//                apiKeyFromService = MaskKey(_cdnService.GetApiKey()),
//                directAccessAvailable = _cdnService.IsDirectAccessAvailable(),
//                isDevEnvironment = _cdnService.IsDevEnvironment()
//            };

//            return Ok(results);
//        }

//        [HttpGet("storage")]
//        public IActionResult CheckStorage()
//        {
//            var configPath = _configuration["CDN:StoragePath"];
//            var servicePath = _cdnService.GetPhysicalPath("");

//            var configExists = !string.IsNullOrEmpty(configPath) && Directory.Exists(configPath);
//            var serviceExists = !string.IsNullOrEmpty(servicePath) && Directory.Exists(servicePath);

//            var configCanWrite = TestWriteAccess(configPath);
//            var serviceCanWrite = TestWriteAccess(servicePath);

//            var results = new
//            {
//                configPath,
//                configExists,
//                configCanWrite,
//                servicePath,
//                serviceExists,
//                serviceCanWrite,

//                // Test standard category paths
//                categories = new[] { "documents", "images", "test-uploads" }
//                    .Select(cat => new
//                    {
//                        name = cat,
//                        path = !string.IsNullOrEmpty(servicePath) ? Path.Combine(servicePath, cat) : null,
//                        exists = !string.IsNullOrEmpty(servicePath) && Directory.Exists(Path.Combine(servicePath, cat)),
//                        canWrite = !string.IsNullOrEmpty(servicePath) && Directory.Exists(Path.Combine(servicePath, cat)) &&
//                                   TestWriteAccess(Path.Combine(servicePath, cat))
//                    }).ToArray()
//            };

//            return Ok(results);
//        }

//        [HttpGet("create-directories")]
//        public IActionResult CreateDirectories()
//        {
//            try
//            {
//                var configPath = _configuration["CDN:StoragePath"];
//                if (string.IsNullOrEmpty(configPath))
//                {
//                    return BadRequest(new { success = false, message = "CDN:StoragePath is not configured" });
//                }

//                var results = new Dictionary<string, object>();

//                // Create main directory
//                if (!Directory.Exists(configPath))
//                {
//                    Directory.CreateDirectory(configPath);
//                    results["mainDirectory"] = $"Created {configPath}";
//                }
//                else
//                {
//                    results["mainDirectory"] = $"{configPath} already exists";
//                }

//                // Create category directories
//                var categories = new[] { "documents", "images", "test-uploads" };
//                foreach (var category in categories)
//                {
//                    var categoryPath = Path.Combine(configPath, category);
//                    if (!Directory.Exists(categoryPath))
//                    {
//                        Directory.CreateDirectory(categoryPath);
//                        results[category] = $"Created {categoryPath}";
//                    }
//                    else
//                    {
//                        results[category] = $"{categoryPath} already exists";
//                    }
//                }

//                return Ok(new { success = true, results });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
//            }
//        }

//        [HttpGet("disk-usage")]
//        public IActionResult GetDiskUsage()
//        {
//            try
//            {
//                var configPath = _configuration["CDN:StoragePath"];
//                if (string.IsNullOrEmpty(configPath) || !Directory.Exists(configPath))
//                {
//                    return BadRequest(new { success = false, message = "CDN storage path not found" });
//                }

//                var directoryInfo = new DirectoryInfo(configPath);
//                var results = new List<object>();

//                // Get all category directories
//                var categoryDirs = directoryInfo.GetDirectories();
//                long totalSize = 0;

//                foreach (var dir in categoryDirs)
//                {
//                    long size = CalculateDirectorySize(dir);
//                    totalSize += size;

//                    results.Add(new
//                    {
//                        category = dir.Name,
//                        fileCount = GetFileCount(dir),
//                        size = size,
//                        formattedSize = FormatFileSize(size),
//                        lastModified = dir.LastWriteTime
//                    });
//                }

//                return Ok(new
//                {
//                    success = true,
//                    totalSize = totalSize,
//                    formattedTotalSize = FormatFileSize(totalSize),
//                    categories = results
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { success = false, message = ex.Message });
//            }
//        }

//        [HttpGet("database")]
//        public async Task<IActionResult> GetDatabaseInfo()
//        {
//            try
//            {
//                // Get counts from database
//                int configCount = await _dbContext.Set<CdnConfiguration>().CountAsync();
//                int apiKeyCount = await _dbContext.Set<CdnApiKey>().CountAsync();
//                int categoryCount = await _dbContext.Set<CdnCategory>().CountAsync();
//                int folderCount = await _dbContext.Set<CdnFolder>().CountAsync();
//                int fileCount = await _dbContext.Set<CdnFileMetadata>().CountAsync();
//                int activeFileCount = await _dbContext.Set<CdnFileMetadata>().Where(f => !f.IsDeleted).CountAsync();
//                int accessLogCount = await _dbContext.Set<CdnAccessLog>().CountAsync();

//                // Get active configuration
//                var activeConfig = await _dbContext.Set<CdnConfiguration>()
//                    .Where(c => c.IsActive)
//                    .OrderByDescending(c => c.Id)
//                    .FirstOrDefaultAsync();

//                // Get all categories with file counts
//                var categories = await _dbContext.Set<CdnCategory>()
//                    .Select(c => new
//                    {
//                        c.Id,
//                        c.Name,
//                        c.DisplayName,
//                        c.IsActive,
//                        FileCount = _dbContext.Set<CdnFileMetadata>()
//                            .Count(f => f.CategoryId == c.Id && !f.IsDeleted)
//                    })
//                    .ToListAsync();

//                return Ok(new
//                {
//                    success = true,
//                    connectionString = MaskConnectionString(_configuration.GetConnectionString("DefaultConnection")),
//                    counts = new
//                    {
//                        configurations = configCount,
//                        apiKeys = apiKeyCount,
//                        categories = categoryCount,
//                        folders = folderCount,
//                        totalFiles = fileCount,
//                        activeFiles = activeFileCount,
//                        accessLogs = accessLogCount
//                    },
//                    activeConfiguration = activeConfig != null ? new
//                    {
//                        id = activeConfig.Id,
//                        baseUrl = activeConfig.BaseUrl,
//                        storagePath = activeConfig.StoragePath,
//                        apiKey = MaskKey(activeConfig.ApiKey),
//                        maxFileSizeMB = activeConfig.MaxFileSizeMB,
//                        enforceAuthentication = activeConfig.EnforceAuthentication,
//                        allowDirectAccess = activeConfig.AllowDirectAccess,
//                        enableCaching = activeConfig.EnableCaching,
//                        modifiedDate = activeConfig.ModifiedDate,
//                        modifiedBy = activeConfig.ModifiedBy
//                    } : null,
//                    categoryDetails = categories
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { success = false, message = ex.Message });
//            }
//        }

//        [HttpGet("verify-files")]
//        public async Task<IActionResult> VerifyFiles(string category = null, int limit = 50)
//        {
//            try
//            {
//                // Limit to a reasonable number to avoid timeouts
//                if (limit > 1000) limit = 1000;

//                // Get files from database
//                var query = _dbContext.Set<CdnFileMetadata>().Where(f => !f.IsDeleted);

//                if (!string.IsNullOrEmpty(category))
//                {
//                    var categoryObj = await _dbContext.Set<CdnCategory>()
//                        .FirstOrDefaultAsync(c => c.Name == category);

//                    if (categoryObj != null)
//                    {
//                        query = query.Where(f => f.CategoryId == categoryObj.Id);
//                    }
//                }

//                var files = await query
//                    .OrderByDescending(f => f.UploadDate)
//                    .Take(limit)
//                    .Select(f => new
//                    {
//                        f.Id,
//                        f.FileName,
//                        f.FilePath,
//                        f.Url,
//                        f.FileSize,
//                        f.CategoryId,
//                        f.UploadDate
//                    })
//                    .ToListAsync();

//                var categories = await _dbContext.Set<CdnCategory>()
//                    .ToDictionaryAsync(c => c.Id, c => c.Name);

//                var results = new List<object>();

//                foreach (var file in files)
//                {
//                    bool exists = !string.IsNullOrEmpty(file.FilePath) && System.IO.File.Exists(file.FilePath);

//                    string physicalPath = _cdnService.GetPhysicalPath(file.Url);
//                    bool urlResolves = !string.IsNullOrEmpty(physicalPath);
//                    bool physicalExists = urlResolves && System.IO.File.Exists(physicalPath);

//                    results.Add(new
//                    {
//                        id = file.Id,
//                        fileName = file.FileName,
//                        url = file.Url,
//                        categoryName = file.CategoryId.HasValue && categories.ContainsKey(file.CategoryId.Value)
//                            ? categories[file.CategoryId.Value]
//                            : null,
//                        uploadDate = file.UploadDate,
//                        databaseFilePath = file.FilePath,
//                        resolvedPhysicalPath = physicalPath,
//                        fileExistsAtDatabasePath = exists,
//                        fileExistsAtResolvedPath = physicalExists,
//                        status = exists && physicalExists ? "OK" :
//                                 exists ? "URL Resolution Failed" :
//                                 physicalExists ? "Database Path Incorrect" : "File Not Found",
//                        isConsistent = exists && physicalExists && (file.FilePath == physicalPath ||
//                                       (string.IsNullOrEmpty(file.FilePath) && !string.IsNullOrEmpty(physicalPath)))
//                    });
//                }

//                return Ok(new
//                {
//                    success = true,
//                    totalChecked = files.Count,
//                    missingFiles = results.Count(r => (bool)((dynamic)r).fileExistsAtDatabasePath == false),
//                    urlResolutionFailures = results.Count(r => (bool)((dynamic)r).fileExistsAtResolvedPath == false),
//                    inconsistentPaths = results.Count(r => (bool)((dynamic)r).isConsistent == false),
//                    details = results
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { success = false, message = ex.Message });
//            }
//        }

//        [HttpGet("usage-stats")]
//        public async Task<IActionResult> GetUsageStatistics(DateTime? startDate = null, DateTime? endDate = null)
//        {
//            try
//            {
//                // Default to last 30 days if not specified
//                startDate ??= DateTime.Today.AddDays(-30);
//                endDate ??= DateTime.Today;

//                // Ensure end date is set to the end of the day
//                endDate = endDate.Value.Date.AddDays(1).AddSeconds(-1);

//                // Get statistics from database
//                var stats = await _dbContext.Set<CdnUsageStatistic>()
//                    .Where(s => s.Date >= startDate.Value.Date && s.Date <= endDate.Value)
//                    .OrderBy(s => s.Date)
//                    .ToListAsync();

//                // Get categories for lookup
//                var categories = await _dbContext.Set<CdnCategory>()
//                    .ToDictionaryAsync(c => c.Id, c => c.Name);

//                var results = stats.Select(s => new
//                {
//                    date = s.Date.ToString("yyyy-MM-dd"),
//                    category = s.CategoryId.HasValue && categories.ContainsKey(s.CategoryId.Value)
//                        ? categories[s.CategoryId.Value]
//                        : "Unknown",
//                    fileCount = s.FileCount,
//                    storageUsedBytes = s.StorageUsedBytes,
//                    formattedStorageUsed = FormatFileSize(s.StorageUsedBytes),
//                    uploadCount = s.UploadCount,
//                    downloadCount = s.DownloadCount,
//                    deleteCount = s.DeleteCount
//                }).ToList();

//                // Group by date for summary
//                var dailySummary = results
//                    .GroupBy(r => r.date)
//                    .Select(g => new
//                    {
//                        date = g.Key,
//                        totalUploads = g.Sum(x => x.uploadCount),
//                        totalDownloads = g.Sum(x => x.downloadCount),
//                        totalDeletes = g.Sum(x => x.deleteCount),
//                        totalStorageBytes = g.Sum(x => x.storageUsedBytes),
//                        formattedTotalStorage = FormatFileSize(g.Sum(x => x.storageUsedBytes))
//                    })
//                    .OrderBy(s => s.date)
//                    .ToList();

//                return Ok(new
//                {
//                    success = true,
//                    startDate = startDate.Value.ToString("yyyy-MM-dd"),
//                    endDate = endDate.Value.ToString("yyyy-MM-dd"),
//                    summary = new
//                    {
//                        totalDays = (endDate.Value - startDate.Value).TotalDays + 1,
//                        totalUploads = results.Sum(r => r.uploadCount),
//                        totalDownloads = results.Sum(r => r.downloadCount),
//                        totalDeletes = results.Sum(r => r.deleteCount),
//                        averageDailyUploads = dailySummary.Any() ? dailySummary.Average(s => s.totalUploads) : 0,
//                        averageDailyDownloads = dailySummary.Any() ? dailySummary.Average(s => s.totalDownloads) : 0
//                    },
//                    dailySummary,
//                    detailedStats = results
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { success = false, message = ex.Message });
//            }
//        }

//        [HttpGet("system-check")]
//        public async Task<IActionResult> SystemCheck()
//        {
//            try
//            {
//                var checks = new List<object>();
//                bool allPassed = true;

//                // Check 1: Database Connection
//                bool dbConnected = false;
//                try
//                {
//                    // Try a simple query
//                    var categoryCount = await _dbContext.Set<CdnCategory>().CountAsync();
//                    dbConnected = true;
//                    checks.Add(new
//                    {
//                        name = "Database Connection",
//                        status = "Passed",
//                        details = $"Successfully connected to database. Found {categoryCount} categories."
//                    });
//                }
//                catch (Exception ex)
//                {
//                    allPassed = false;
//                    checks.Add(new
//                    {
//                        name = "Database Connection",
//                        status = "Failed",
//                        details = $"Error connecting to database: {ex.Message}"
//                    });
//                }

//                // Check 2: Storage Path
//                string storagePath = _cdnService.GetPhysicalPath("");
//                bool storageExists = !string.IsNullOrEmpty(storagePath) && Directory.Exists(storagePath);
//                bool canWrite = TestWriteAccess(storagePath);

//                if (storageExists && canWrite)
//                {
//                    checks.Add(new
//                    {
//                        name = "Storage Path",
//                        status = "Passed",
//                        details = $"Storage path {storagePath} exists and is writable."
//                    });
//                }
//                else
//                {
//                    allPassed = false;
//                    checks.Add(new
//                    {
//                        name = "Storage Path",
//                        status = "Failed",
//                        details = storageExists
//                            ? $"Storage path {storagePath} exists but is not writable."
//                            : $"Storage path {storagePath} does not exist."
//                    });
//                }

//                // Check 3: Category Directories
//                var categoryDirs = new[] { "documents", "images", "test-uploads" };
//                foreach (var cat in categoryDirs)
//                {
//                    if (storageExists)
//                    {
//                        string catPath = Path.Combine(storagePath, cat);
//                        bool catExists = Directory.Exists(catPath);
//                        bool catWritable = TestWriteAccess(catPath);

//                        if (catExists && catWritable)
//                        {
//                            checks.Add(new
//                            {
//                                name = $"Category: {cat}",
//                                status = "Passed",
//                                details = $"Category directory exists and is writable."
//                            });
//                        }
//                        else
//                        {
//                            allPassed = false;
//                            checks.Add(new
//                            {
//                                name = $"Category: {cat}",
//                                status = "Failed",
//                                details = catExists
//                                    ? $"Category directory exists but is not writable."
//                                    : $"Category directory does not exist."
//                            });
//                        }
//                    }
//                }

//                // Check 4: API Key Configuration
//                string apiKey = _cdnService.GetApiKey();
//                if (!string.IsNullOrEmpty(apiKey))
//                {
//                    checks.Add(new
//                    {
//                        name = "API Key",
//                        status = "Passed",
//                        details = $"API key is configured: {MaskKey(apiKey)}"
//                    });
//                }
//                else
//                {
//                    allPassed = false;
//                    checks.Add(new
//                    {
//                        name = "API Key",
//                        status = "Failed",
//                        details = "API key is not configured."
//                    });
//                }

//                // Check 5: Database configuration
//                if (dbConnected)
//                {
//                    var activeConfig = await _dbContext.Set<CdnConfiguration>()
//                        .Where(c => c.IsActive)
//                        .OrderByDescending(c => c.Id)
//                        .FirstOrDefaultAsync();

//                    if (activeConfig != null)
//                    {
//                        checks.Add(new
//                        {
//                            name = "Database Configuration",
//                            status = "Passed",
//                            details = $"Active configuration found (ID: {activeConfig.Id})."
//                        });
//                    }
//                    else
//                    {
//                        // Not critical, bootstrap values will be used
//                        checks.Add(new
//                        {
//                            name = "Database Configuration",
//                            status = "Warning",
//                            details = "No active configuration found in database. Bootstrap values will be used."
//                        });
//                    }
//                }

//                // Check 6: File consistency
//                if (dbConnected && storageExists)
//                {
//                    // Check a small sample of files
//                    const int sampleSize = 5;
//                    var fileSample = await _dbContext.Set<CdnFileMetadata>()
//                        .Where(f => !f.IsDeleted)
//                        .OrderByDescending(f => f.UploadDate)
//                        .Take(sampleSize)
//                        .ToListAsync();

//                    int existingCount = 0;
//                    foreach (var file in fileSample)
//                    {
//                        if (!string.IsNullOrEmpty(file.FilePath) && System.IO.File.Exists(file.FilePath))
//                        {
//                            existingCount++;
//                        }
//                    }

//                    if (fileSample.Count == 0)
//                    {
//                        checks.Add(new
//                        {
//                            name = "File Consistency",
//                            status = "Warning",
//                            details = "No files found in database to check."
//                        });
//                    }
//                    else if (existingCount == fileSample.Count)
//                    {
//                        checks.Add(new
//                        {
//                            name = "File Consistency",
//                            status = "Passed",
//                            details = $"All {existingCount} sampled files exist on disk."
//                        });
//                    }
//                    else
//                    {
//                        allPassed = false;
//                        checks.Add(new
//                        {
//                            name = "File Consistency",
//                            status = "Failed",
//                            details = $"Only {existingCount} out of {fileSample.Count} sampled files exist on disk."
//                        });
//                    }
//                }

//                return Ok(new
//                {
//                    success = true,
//                    timestamp = DateTime.Now,
//                    overallStatus = allPassed ? "Passed" : "Failed",
//                    checks
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { success = false, message = ex.Message });
//            }
//        }

//        #region Helper Methods

//        private string MaskKey(string key)
//        {
//            if (string.IsNullOrEmpty(key))
//                return null;

//            if (key.Length <= 8)
//                return "****";

//            return key.Substring(0, 4) + "..." + key.Substring(key.Length - 4);
//        }

//        private string MaskConnectionString(string connectionString)
//        {
//            if (string.IsNullOrEmpty(connectionString))
//                return null;

//            // Replace password
//            if (connectionString.Contains("password=") || connectionString.Contains("Password="))
//            {
//                var parts = connectionString.Split(';');
//                for (int i = 0; i < parts.Length; i++)
//                {
//                    if (parts[i].ToLower().Contains("password="))
//                    {
//                        var subParts = parts[i].Split('=');
//                        if (subParts.Length > 1)
//                        {
//                            parts[i] = subParts[0] + "=*****";
//                        }
//                    }
//                }
//                return string.Join(';', parts);
//            }

//            // Don't try to parse other connection string formats, just mask them partially
//            if (connectionString.Length > 20)
//            {
//                return connectionString.Substring(0, 10) + "..." + connectionString.Substring(connectionString.Length - 10);
//            }

//            return "*****";
//        }

//        private bool TestWriteAccess(string path)
//        {
//            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
//                return false;

//            try
//            {
//                string testFile = Path.Combine(path, $"test_{Guid.NewGuid()}.txt");
//                System.IO.File.WriteAllText(testFile, "Test write access");
//                bool result = System.IO.File.Exists(testFile);
//                if (result)
//                {
//                    System.IO.File.Delete(testFile);
//                }
//                return result;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error testing write access to {Path}", path);
//                return false;
//            }
//        }

//        private long CalculateDirectorySize(DirectoryInfo dir)
//        {
//            long size = 0;

//            // Add size of all files
//            foreach (var file in dir.GetFiles())
//            {
//                size += file.Length;
//            }

//            // Add size of all subdirectories
//            foreach (var subdir in dir.GetDirectories())
//            {
//                size += CalculateDirectorySize(subdir);
//            }

//            return size;
//        }

//        private int GetFileCount(DirectoryInfo dir)
//        {
//            int count = dir.GetFiles().Length;

//            // Add count of files in subdirectories
//            foreach (var subdir in dir.GetDirectories())
//            {
//                count += GetFileCount(subdir);
//            }

//            return count;
//        }

//        private string FormatFileSize(long bytes)
//        {
//            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
//            double len = bytes;
//            int order = 0;

//            while (len >= 1024 && order < sizes.Length - 1)
//            {
//                order++;
//                len = len / 1024;
//            }

//            return $"{len:0.##} {sizes[order]}";
//        }

//        #endregion
//    }
//}