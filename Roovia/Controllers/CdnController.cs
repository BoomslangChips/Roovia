// Controllers/CdnController.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roovia.Interfaces;

namespace Roovia.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CdnController : ControllerBase
    {
        private readonly ICdnService _cdnService;

        public CdnController(ICdnService cdnService)
        {
            _cdnService = cdnService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string category = "documents")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file was uploaded" });

            try
            {
                // Check file size (limit to 20MB)
                if (file.Length > 20 * 1024 * 1024)
                    return BadRequest(new { success = false, message = "File size exceeds the 20MB limit" });

                // Validate file type (basic validation)
                var allowedTypes = new[] { "image/", "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument", "text/plain", "text/csv" };
                bool isValidType = false;
                foreach (var type in allowedTypes)
                {
                    if (file.ContentType.StartsWith(type))
                    {
                        isValidType = true;
                        break;
                    }
                }

                if (!isValidType)
                    return BadRequest(new { success = false, message = "File type not allowed" });

                // Upload the file
                var fileUrl = await _cdnService.UploadFileAsync(file, category);

                // Return success with the file URL
                return Ok(new
                {
                    success = true,
                    url = fileUrl,
                    fileName = file.FileName,
                    contentType = file.ContentType,
                    size = file.Length,
                    category = category
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path))
                return BadRequest(new { success = false, message = "No file path provided" });

            try
            {
                var result = await _cdnService.DeleteFileAsync(path);
                if (result)
                    return Ok(new { success = true, message = "File deleted successfully" });
                else
                    return NotFound(new { success = false, message = "File not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }
    }
}