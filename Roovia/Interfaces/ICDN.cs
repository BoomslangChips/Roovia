using Roovia.Models.ProjectCdnConfigModels;

namespace Roovia.Interfaces
{
    public interface ICdnService
    {
        // File operations
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string category = "documents", string folderPath = "");

        Task<string> UploadFileAsync(IFormFile file, string category = "documents", string folderPath = "");

        Task<string> UploadFileWithBase64BackupAsync(Stream fileStream, string fileName, string contentType, string category = "documents", string folderPath = "");

        Task<(Stream stream, bool isFromBase64)> GetFileStreamAsync(string cdnUrl);

        Task<Stream> GetBase64StreamAsync(string cdnUrl);

        Task<bool> DeleteFileAsync(string cdnUrl);

        Task<string> RenameFileAsync(string cdnUrl, string newFileName);

        Task<bool> FileExistsAsync(string cdnUrl);

        Task<CdnFileMetadata> GetFileMetadataAsync(string cdnUrl);

        Task MigrateFileToBase64Async(string cdnUrl);

        Task<bool> RestoreFromBase64Async(string cdnUrl);

        // Folder operations
        Task<CdnFolder> CreateFolderAsync(string category, string folderPath, string folderName);

        Task<bool> DeleteFolderAsync(string category, string folderPath);

        Task<List<CdnFolder>> GetFoldersAsync(string category, string parentPath = null);

        Task<bool> RenameFolderAsync(string category, string oldPath, string newName);

        Task<bool> MoveFolderAsync(string category, string sourcePath, string destinationPath);

        // File/folder listing
        Task<List<CdnFileMetadata>> GetFilesAsync(string category = "documents", string folderPath = null, string searchTerm = null);

        Task<(List<CdnFolder> folders, List<CdnFileMetadata> files)> GetContentAsync(string category, string folderPath = null);

        Task<long> GetFolderSizeAsync(string category, string folderPath);

        Task<int> GetFileCountAsync(string category, string folderPath = null);

        // Category management
        Task<List<CdnCategory>> GetCategoriesAsync();

        Task<CdnCategory> GetCategoryAsync(string name);

        Task<CdnCategory> CreateCategoryAsync(string name, string displayName, string allowedFileTypes = "*", string description = null);

        Task<CdnCategory> UpdateCategoryAsync(int categoryId, string displayName, string allowedFileTypes, string description);

        Task<bool> DeleteCategoryAsync(int categoryId);

        Task<bool> CategoryExistsAsync(string name);

        // Configuration management
        Task<CdnConfiguration> GetConfigurationAsync();

        Task<CdnConfiguration> UpdateConfigurationAsync(CdnConfiguration config);

        Task<bool> ValidateFileTypeAsync(string fileName, string category);

        Task<bool> ValidateFileSizeAsync(long fileSize);

        // Usage statistics
        Task<CdnUsageStatistic> GetUsageStatisticsAsync(DateTime date, int? categoryId = null);

        Task<List<CdnUsageStatistic>> GetUsageStatisticsRangeAsync(DateTime startDate, DateTime endDate, int? categoryId = null);

        Task UpdateUsageStatisticsAsync(int categoryId, int fileCountChange, long storageBytesChange, int uploadCount = 0, int downloadCount = 0, int deleteCount = 0);

        // Access logs
        Task LogAccessAsync(string actionType, string filePath, string username, bool isSuccess, string errorMessage = null, long? fileSizeBytes = null);

        Task<List<CdnAccessLog>> GetAccessLogsAsync(DateTime startDate, DateTime endDate, string actionType = null);

        // Utility methods
        string GetCdnUrl(string path);

        bool IsDirectAccessAvailable();

        string GetPhysicalPath(string cdnUrl);

        Task<string> CalculateChecksumAsync(string filePath);

        Task CleanupDeletedFilesAsync(int daysOld = 30);
    }
}