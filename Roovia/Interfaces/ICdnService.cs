using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Roovia.Models.CDN;

namespace Roovia.Interfaces
{
    public interface ICdnService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string category = "documents", string folderPath = "");
        Task<string> UploadFileAsync(IFormFile file, string category = "documents", string folderPath = "");
        string GetCdnUrl(string path);
        Task<bool> DeleteFileAsync(string path);
        string GetApiKey();
        bool IsDirectAccessAvailable();
        string GetPhysicalPath(string cdnUrl);
        Stream GetFileStream(string cdnUrl);
        bool IsDevEnvironment();
        Task<List<CdnCategory>> GetCategoriesAsync();
        Task<List<CdnFolder>> GetFoldersAsync(string category);
        Task<List<CdnFileMetadata>> GetFilesAsync(string category = "documents", string folderPath = null, string searchTerm = null);
    }
}