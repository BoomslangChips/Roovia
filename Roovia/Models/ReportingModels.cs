using Roovia.Models.UserCompanyModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roovia.Models.ReportingModels
{
    [Table("CustomReports")]
    public class CustomReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [StringLength(4000)]
        public string? Query { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Parameters stored as JSON string
        [StringLength(2000)]
        public string? Parameters { get; set; }

        public int CompanyId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? CreatedByUser { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual ApplicationUser? UpdatedByUser { get; set; }
    }

    [Table("ReportSchedules")]
    public class ReportSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        public int? CustomReportId { get; set; }

        [StringLength(50)]
        public string? StandardReportType { get; set; }

        [StringLength(2000)]
        public string? Parameters { get; set; } // JSON string with parameters

        [Required]
        public int FrequencyTypeId { get; set; } // Daily, Weekly, Monthly, etc.

        public int? DayOfWeek { get; set; } // 1-7 for weekly schedules

        public int? DayOfMonth { get; set; } // 1-31 for monthly schedules

        public TimeSpan ExecutionTime { get; set; } // Time of day to run

        [StringLength(255)]
        public string? RecipientEmails { get; set; } // Comma-separated email addresses

        [Required]
        [StringLength(20)]
        public string? ExportFormat { get; set; } // PDF, Excel, CSV, etc.

        public DateTime LastRunDate { get; set; }

        public DateTime? NextRunDate { get; set; }

        public bool IsActive { get; set; } = true;

        public int CompanyId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CustomReportId")]
        public virtual CustomReport? CustomReport { get; set; }

        [ForeignKey("FrequencyTypeId")]
        public virtual ReportFrequencyType? FrequencyType { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? CreatedByUser { get; set; }
    }

    [Table("ReportFrequencyTypes")]
    public class ReportFrequencyType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int? DaysInterval { get; set; } // Number of days between executions

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("ReportExecutionLogs")]
    public class ReportExecutionLog
    {
        [Key]
        public int Id { get; set; }

        public int? ReportScheduleId { get; set; }

        [StringLength(100)]
        public string? ReportType { get; set; } // Custom or standard report type

        [StringLength(2000)]
        public string? Parameters { get; set; } // JSON string with parameters used

        public DateTime ExecutionStartTime { get; set; }

        public DateTime? ExecutionEndTime { get; set; }

        public bool IsSuccess { get; set; }

        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        public int? RowCount { get; set; }

        [StringLength(255)]
        public string? OutputFilePath { get; set; }

        public int? CdnFileMetadataId { get; set; }

        public bool EmailSent { get; set; } = false;

        [StringLength(255)]
        public string? RecipientEmails { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string? ExecutedBy { get; set; }

        // Navigation properties
        [ForeignKey("ReportScheduleId")]
        public virtual ReportSchedule? ReportSchedule { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("ExecutedBy")]
        public virtual ApplicationUser? ExecutedByUser { get; set; }
    }

    [Table("ReportDashboards")]
    public class ReportDashboard
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; } = false;

        public int LayoutColumns { get; set; } = 3;

        [StringLength(4000)]
        public string? Configuration { get; set; } // JSON configuration for the dashboard

        public int CompanyId { get; set; }

        [StringLength(100)]
        public string? UserId { get; set; } // If null, it's a company-wide dashboard

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? CreatedByUser { get; set; }

        public virtual ICollection<ReportDashboardWidget> Widgets { get; set; } = new List<ReportDashboardWidget>();
    }

    [Table("ReportDashboardWidgets")]
    public class ReportDashboardWidget
    {
        [Key]
        public int Id { get; set; }

        public int DashboardId { get; set; }

        [Required]
        [StringLength(100)]
        public string? Title { get; set; }

        [Required]
        [StringLength(50)]
        public string? WidgetType { get; set; } // Chart, Table, KPI, etc.

        [Required]
        [StringLength(50)]
        public string? DataSource { get; set; } // CustomReport, StandardReport, DirectQuery

        public int? CustomReportId { get; set; }

        [StringLength(100)]
        public string? StandardReportType { get; set; }

        [StringLength(2000)]
        public string? Parameters { get; set; } // JSON string with parameters

        [StringLength(50)]
        public string? VisualizationType { get; set; } // Bar, Line, Pie, Table, etc.

        public int GridColumn { get; set; } = 1;

        public int GridRow { get; set; } = 1;

        public int GridWidth { get; set; } = 1;

        public int GridHeight { get; set; } = 1;

        [StringLength(4000)]
        public string? Configuration { get; set; } // JSON configuration for the widget

        public bool AutoRefresh { get; set; } = false;

        public int? RefreshInterval { get; set; } // Minutes

        public DateTime? LastRefreshed { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("DashboardId")]
        public virtual ReportDashboard? Dashboard { get; set; }

        [ForeignKey("CustomReportId")]
        public virtual CustomReport? CustomReport { get; set; }
    }
}