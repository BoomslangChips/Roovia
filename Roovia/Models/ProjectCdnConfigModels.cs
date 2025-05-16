using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roovia.Models.ProjectCdnConfigModels
{
    [Table("CdnConfigurations")]
    public class CdnConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string? BaseUrl { get; set; }

        [Required, MaxLength(255)]
        public string? StoragePath { get; set; }

        public int MaxFileSizeMB { get; set; } = 200;

        [Required, MaxLength(500)]
        public string AllowedFileTypes { get; set; } = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.mp4,.mp3,.zip";

        public bool EnableCaching { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("CdnCategories")]
    public class CdnCategory
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? DisplayName { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? AllowedFileTypes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        // Add navigation properties
        public virtual ICollection<CdnFileMetadata> Files { get; set; } = new List<CdnFileMetadata>();

        public virtual ICollection<CdnFolder> Folders { get; set; } = new List<CdnFolder>();
    }

    [Table("CdnFolders")]
    public class CdnFolder
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string? Name { get; set; }

        [Required, MaxLength(500)]
        public string? Path { get; set; }

        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public CdnFolder? Parent { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public CdnCategory? Category { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public bool IsActive { get; set; } = true;

        // Add navigation properties
        public virtual ICollection<CdnFolder> Children { get; set; } = new List<CdnFolder>();

        public virtual ICollection<CdnFileMetadata> Files { get; set; } = new List<CdnFileMetadata>();
    }

    [Table("CdnFileMetadata")]
    public class CdnFileMetadata
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string? FilePath { get; set; }

        [Required, MaxLength(255)]
        public string? FileName { get; set; }

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public CdnCategory? Category { get; set; }

        public int? FolderId { get; set; }

        [ForeignKey("FolderId")]
        public CdnFolder? Folder { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? UploadedBy { get; set; }

        public DateTime? LastAccessDate { get; set; }

        public int AccessCount { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedDate { get; set; }

        [Required, MaxLength(255)]
        public string? Url { get; set; }

        [MaxLength(100)]
        public string? Checksum { get; set; }

        public bool HasBase64Backup { get; set; }

        // Navigation property for base64 storage
        public virtual CdnBase64Storage? Base64Storage { get; set; }
    }

    [Table("CdnBase64Storage")]
    public class CdnBase64Storage
    {
        [Key]
        public int Id { get; set; }

        public int FileMetadataId { get; set; }

        [ForeignKey("FileMetadataId")]
        public CdnFileMetadata? FileMetadata { get; set; }

        [Required]
        public string? Base64Data { get; set; }

        [Required, MaxLength(100)]
        public string? MimeType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    [Table("CdnUsageStatistics")]
    public class CdnUsageStatistic
    {
        [Key]
        public int Id { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public CdnCategory? Category { get; set; }

        public int FileCount { get; set; }

        public long StorageUsedBytes { get; set; }

        public int UploadCount { get; set; }

        public int DownloadCount { get; set; }

        public int DeleteCount { get; set; }

        [MaxLength(100)]
        public string? RecordedBy { get; set; }
    }

    [Table("CdnAccessLogs")]
    public class CdnAccessLog
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required, MaxLength(20)]
        public string? ActionType { get; set; } // Upload, Download, Delete, View, Create, Rename

        [MaxLength(500)]
        public string? FilePath { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(255)]
        public string? UserAgent { get; set; }

        public bool IsSuccess { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        public long? FileSizeBytes { get; set; }

        public int? ResponseTimeMs { get; set; }
    }
}