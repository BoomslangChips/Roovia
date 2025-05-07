// ICdnService.cs
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Roovia.Interfaces
{
    public interface ICdnService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string category = "documents");
        Task<string> UploadFileAsync(IFormFile file, string category = "documents");
        string GetCdnUrl(string path);
        Task<bool> DeleteFileAsync(string path);
    }
}