using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roovia.Models.BusinessMappingModels
{
    #region General Mappings

    [Table("BankNameTypes")]
    public class BankNameType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(6)]
        public string? DefaultBranchCode { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("ContactNumberTypes")]
    public class ContactNumberType
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

    [Table("MediaTypes")]
    public class MediaType
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

    [Table("EntityTypes")]
    public class EntityType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? SystemName { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    #endregion General Mappings

    #region Document Mappings

    [Table("DocumentTypes")]
    public class DocumentType
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

    [Table("DocumentCategories")]
    public class DocumentCategory
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

    [Table("DocumentAccessLevels")]
    public class DocumentAccessLevel
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

    [Table("DocumentStatuses")]
    public class DocumentStatus
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

    [Table("DocumentRequirementTypes")]
    public class DocumentRequirementType
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

    [Table("EntityDocumentRequirements")]
    public class EntityDocumentRequirement
    {
        [Key]
        public int Id { get; set; }

        public int EntityTypeId { get; set; } // FK to EntityType

        public int DocumentTypeId { get; set; } // FK to DocumentType

        public int DocumentRequirementTypeId { get; set; } // FK to DocumentRequirementType (Required, Optional, etc.)

        public bool IsDefault { get; set; } = false;

        public int? CompanyId { get; set; } // Null means system-wide, populated for company-specific requirements

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("EntityTypeId")]
        public virtual EntityType? EntityType { get; set; }

        [ForeignKey("DocumentTypeId")]
        public virtual DocumentType? DocumentType { get; set; }

        [ForeignKey("DocumentRequirementTypeId")]
        public virtual DocumentRequirementType? DocumentRequirementType { get; set; }
    }

    #endregion Document Mappings

    #region Property Mappings

    [Table("PropertyStatusTypes")]
    public class PropertyStatusType
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

    [Table("CommissionTypes")]
    public class CommissionType
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

    [Table("PropertyImageTypes")]
    public class PropertyImageType
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

    [Table("PropertyTypes")]
    public class PropertyType
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

    [Table("PropertyOwnerTypes")]
    public class PropertyOwnerType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; } // Individual, Company, Trust, etc.

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("PropertyOwnerStatusTypes")]
    public class PropertyOwnerStatusType
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

    #endregion Property Mappings

    #region Beneficiary Mappings

    [Table("BeneficiaryTypes")]
    public class BeneficiaryType
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

    [Table("BeneficiaryStatusTypes")]
    public class BeneficiaryStatusType
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

    #endregion Beneficiary Mappings

    #region Tenant Mappings

    [Table("TenantStatusTypes")]
    public class TenantStatusType
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

    [Table("TenantTypes")]
    public class TenantType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; } // Individual, Company, etc.

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    #endregion Tenant Mappings

    #region Inspection Mappings

    [Table("InspectionTypes")]
    public class InspectionType
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

    [Table("InspectionStatusTypes")]
    public class InspectionStatusType
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

    [Table("InspectionAreas")]
    public class InspectionArea
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

    [Table("ConditionLevels")]
    public class ConditionLevel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public int? ScoreValue { get; set; } // Numeric value for calculations

        public bool IsActive { get; set; } = true;
    }

    #endregion Inspection Mappings

    #region Maintenance Mappings

    [Table("MaintenanceCategories")]
    public class MaintenanceCategory
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

    [Table("MaintenancePriorities")]
    public class MaintenancePriority
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public int? ResponseTimeHours { get; set; } // SLA response time

        public bool IsActive { get; set; } = true;
    }

    [Table("MaintenanceStatusTypes")]
    public class MaintenanceStatusType
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

    [Table("MaintenanceImageTypes")]
    public class MaintenanceImageType
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

    [Table("ExpenseCategories")]
    public class ExpenseCategory
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

    #endregion Maintenance Mappings

    #region Payment Mappings

    [Table("PaymentTypes")]
    public class PaymentType
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

    [Table("PaymentStatusTypes")]
    public class PaymentStatusType
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

    [Table("PaymentMethods")]
    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public decimal? ProcessingFeePercentage { get; set; }

        public decimal? ProcessingFeeFixed { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("AllocationTypes")]
    public class AllocationType
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

    [Table("BeneficiaryPaymentStatusTypes")]
    public class BeneficiaryPaymentStatusType
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

    [Table("PaymentFrequencies")]
    public class PaymentFrequency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int? DaysInterval { get; set; } // Number of days between payments

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("PaymentRuleTypes")]
    public class PaymentRuleType
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

    #endregion Payment Mappings

    #region Communication and Notification Mappings

    [Table("CommunicationChannels")]
    public class CommunicationChannel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; } // Email, SMS, WhatsApp, Phone call, In-person, etc.

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("CommunicationDirections")]
    public class CommunicationDirection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; } // Inbound, Outbound

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("NotificationEventTypes")]
    public class NotificationEventType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; } // RentDue, MaintenanceScheduled, LeaseExpiring, etc.

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; } // Property, Tenant, Payment, etc.

        [StringLength(100)]
        public string? SystemName { get; set; } // Used for programmatic reference

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsSystemEvent { get; set; } = true; // If false, it's a custom event
    }

    [Table("NotificationTemplates")]
    public class NotificationTemplate
    {
        [Key]
        public int Id { get; set; }

        public int NotificationEventTypeId { get; set; } // FK to NotificationEventType

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [StringLength(200)]
        public string? Subject { get; set; }

        [Required]
        [StringLength(4000)]
        public string? BodyTemplate { get; set; } // Can contain placeholders like {{PropertyName}}

        [StringLength(4000)]
        public string? SmsTemplate { get; set; }

        public int? CompanyId { get; set; } // Null for system templates, populated for company-specific templates

        public bool IsDefault { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("NotificationEventTypeId")]
        public virtual NotificationEventType? NotificationEventType { get; set; }
    }

    #endregion Communication and Notification Mappings

    #region Note and Reminder Mappings

    [Table("NoteTypes")]
    public class NoteType
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

    [Table("ReminderTypes")]
    public class ReminderType
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

    [Table("ReminderStatuses")]
    public class ReminderStatus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; } // Pending, Completed, Snoozed, Cancelled

        [StringLength(200)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [Table("RecurrenceFrequencies")]
    public class RecurrenceFrequency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; } // Daily, Weekly, Monthly, Quarterly, Yearly

        [StringLength(200)]
        public string? Description { get; set; }

        public int? DaysMultiplier { get; set; } // For calculating next occurrence: Daily=1, Weekly=7, etc.

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    #endregion Note and Reminder Mappings
}