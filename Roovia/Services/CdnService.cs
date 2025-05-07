// CdnService.cs
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Roovia.Interfaces;
using Roovia.Models.Helper;

namespace Roovia.Services
{
    public class CdnService : ICdnService
    {
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _cdnBaseUrl;
        private readonly string _cdnStoragePath;

        public CdnService(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = "Roovia-OilProduction-CDN-8A4F9C2E7D31B5";
            _cdnBaseUrl = "https://cdn.roovia.co.za:8443";
            _cdnStoragePath = "/var/www/cdn";
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string category = "documents")
        {
            try
            {
                // Ensure category is valid
                category = ValidateCategory(category);

                // Generate a unique filename to prevent collisions
                var uniqueFileName = GenerateUniqueFileName(fileName);
                
                // Create the physical directory if it doesn't exist
                var directoryPath = Path.Combine(_cdnStoragePath, category);
                Directory.CreateDirectory(directoryPath);
                
                // Save file to disk
                var filePath = Path.Combine(directoryPath, uniqueFileName);
                using (var fileStream2 = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStream2);
                }
                
                // Return the public URL
                return $"{_cdnBaseUrl}/{category}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload file: {ex.Message}", ex);
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string category = "documents")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            using var stream = file.OpenReadStream();
            return await UploadFileAsync(stream, file.FileName, file.ContentType, category);
        }

        public string GetCdnUrl(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // If path already starts with the CDN base URL, return it as is
            if (path.StartsWith(_cdnBaseUrl))
                return path;

            // If path starts with a slash, remove it
            if (path.StartsWith("/"))
                path = path.Substring(1);

            return $"{_cdnBaseUrl}/{path}";
        }
        
        public async Task<bool> DeleteFileAsync(string path)
        {
            try
            {
                // Extract the relative path from the URL
                string relativePath = path.Replace(_cdnBaseUrl, "").TrimStart('/');
                string fullPath = Path.Combine(_cdnStoragePath, relativePath);
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private string ValidateCategory(string category)
        {
            var allowedCategories = new[] { "documents", "images", "hr", "weighbridge", "lab" };
            return allowedCategories.Contains(category.ToLower()) ? category.ToLower() : "documents";
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
    }
}