using System.ComponentModel.DataAnnotations;

namespace Roovia.Models.Helper
{
    public class Media
    {
        public int Id { get; set; }

        [StringLength(255)]
        public string? FileName { get; set; }

        [StringLength(255)]
        public string? FilePath { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }
        public MediaType Type { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UploadedBy { get; set; }

        // Reference properties
        public int? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityType { get; set; }
    }

    public enum MediaType
    {
        Image,
        Document,
        Video,
        Audio,
        Other
    }
}