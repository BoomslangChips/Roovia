using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roovia.Models.UserCompanyMappingModels
{
    #region Company Mappings

    [Table("CompanyStatusTypes")]
    public class CompanyStatusType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("SubscriptionPlans")]
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int BillingCycleDays { get; set; } // 30, 365, etc.

        public int MaxUsers { get; set; }

        public int MaxProperties { get; set; }

        public int MaxBranches { get; set; }

        public bool HasTrialPeriod { get; set; }

        public int? TrialPeriodDays { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    #endregion Company Mappings

    #region Branch Mappings

    [Table("BranchStatusTypes")]
    public class BranchStatusType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    #endregion Branch Mappings

    #region User Mappings

    [Table("UserStatusTypes")]
    public class UserStatusType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("TwoFactorMethods")]
    public class TwoFactorMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    #endregion User Mappings

    #region Permission Mappings

    [Table("PermissionCategories")]
    public class PermissionCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Icon { get; set; } // FontAwesome icon class

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("RoleTypes")]
    public class RoleType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    #endregion Permission Mappings

    #region Notification Mappings

    [Table("NotificationTypes")]
    public class NotificationType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Icon { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("NotificationChannels")]
    public class NotificationChannel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    #endregion Notification Mappings

    #region Theme Mappings

    [Table("ThemeTypes")]
    public class ThemeType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? CssVariables { get; set; } // JSON configuration

        public bool IsDarkTheme { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    #endregion Theme Mappings
}