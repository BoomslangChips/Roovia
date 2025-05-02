using System.ComponentModel.DataAnnotations;

namespace Roovia.Models.Users
{
    public class BranchLogo
    {
        public int Id { get; set; }
        public int BranchId { get; set; }

        [StringLength(255)]
        public string? FileName { get; set; }

        [StringLength(255)]
        public string? FilePath { get; set; }

        public LogoSize Size { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UploadedBy { get; set; }

        // Navigation property
        public Branch? Branch { get; set; }
    }

    public enum LogoSize
    {
        Small, // For login screens, thumbnails (e.g., 64x64px)
        Medium, // For general use (e.g., 256x256px)
        Large // For invoices, high-resolution needs (e.g., 512x512px or higher)
    }
}