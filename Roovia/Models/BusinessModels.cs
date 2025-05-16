using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roovia.Models.BusinessMappingModels;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.UserCompanyModels;
using Roovia.Models.ProjectCdnConfigModels;

namespace Roovia.Models.BusinessModels
{
    #region Properties

    public class Property
    {
        [Key]
        public int Id { get; set; }

        public int OwnerId { get; set; }

        public int CompanyId { get; set; }

        public int? BranchId { get; set; }

        public int PropertyTypeId { get; set; } // FK to PropertyType

        [Required]
        [StringLength(100)]
        public string? PropertyName { get; set; }

        [Required]
        [StringLength(50)]
        public string? PropertyCode { get; set; } // Unique identifier for property

        [StringLength(50)]
        public string? CustomerRef { get; set; } // Customer reference from CSV

        public decimal RentalAmount { get; set; }

        public decimal? PropertyAccountBalance { get; set; }

        public int StatusId { get; set; } // FK to PropertyStatusType

        [StringLength(20)]
        public string? ServiceLevel { get; set; } // Could also be made into a mapping table

        public bool HasTenant { get; set; }

        public DateTime? LeaseOriginalStartDate { get; set; }

        public DateTime? CurrentLeaseStartDate { get; set; }

        public DateTime? LeaseEndDate { get; set; }

        public int? CurrentTenantId { get; set; }

        // Commission settings
        public int CommissionTypeId { get; set; } // FK to CommissionType

        public decimal CommissionValue { get; set; } // Percentage or fixed amount

        // Payment settings
        public bool PaymentsEnabled { get; set; } = true;

        public bool PaymentsVerify { get; set; } = true;

        // CDN Integration for Main Image
        public int? MainImageId { get; set; } // FK to CdnFileMetadata

        // Address (owned entity)
        public Address Address { get; set; } = new Address();

        // Tags for categorization
        [StringLength(500)]
        public string? Tags { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public bool IsRemoved { get; set; }

        public DateTime? RemovedDate { get; set; }

        [StringLength(100)]
        public string? RemovedBy { get; set; }

        // Navigation properties
        [ForeignKey("OwnerId")]
        public virtual PropertyOwner? Owner { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        [ForeignKey("StatusId")]
        public virtual PropertyStatusType? Status { get; set; }

        [ForeignKey("CommissionTypeId")]
        public virtual CommissionType? CommissionType { get; set; }

        [ForeignKey("MainImageId")]
        public virtual CdnFileMetadata? MainImage { get; set; }

        [ForeignKey("PropertyTypeId")]
        public virtual PropertyType? PropertyType { get; set; }

        public virtual ICollection<PropertyBeneficiary> Beneficiaries { get; set; } = new List<PropertyBeneficiary>();

        public virtual ICollection<PropertyTenant> Tenants { get; set; } = new List<PropertyTenant>();

        public virtual ICollection<PropertyInspection> Inspections { get; set; } = new List<PropertyInspection>();

        public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

        public virtual ICollection<PropertyPayment> Payments { get; set; } = new List<PropertyPayment>();

        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();

        public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();

        // Helper methods
        public void EnsureValidDates()
        {
            if (LeaseOriginalStartDate == DateTime.MinValue)
                LeaseOriginalStartDate = null;
            if (CurrentLeaseStartDate == DateTime.MinValue)
                CurrentLeaseStartDate = null;
            if (LeaseEndDate == DateTime.MinValue)
                LeaseEndDate = null;
        }
    }

    public class PropertyBeneficiary
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }

        public int CompanyId { get; set; }

        // Beneficiary details
        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        // Using Email and ContactNumber collections
        public virtual ICollection<Email> EmailAddresses { get; set; } = new List<Email>();
        public virtual ICollection<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();

        // Address
        public Address Address { get; set; } = new Address();

        // Beneficiary Type
        public int BenTypeId { get; set; } // FK to BeneficiaryType

        // Commission/Payment details
        public int CommissionTypeId { get; set; } // FK to CommissionType

        public decimal CommissionValue { get; set; } // Percentage or fixed amount

        public decimal Amount { get; set; } // Calculated amount

        public decimal PropertyAmount { get; set; } // Reference to property amount

        // Status
        public int BenStatusId { get; set; } // FK to BeneficiaryStatusType

        // References
        [StringLength(50)]
        public string? CustomerRefBeneficiary { get; set; }

        [StringLength(50)]
        public string? CustomerRefProperty { get; set; }

        [StringLength(100)]
        public string? Agent { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; }

        // Bank details
        public BankAccount BankAccount { get; set; } = new BankAccount();

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("PropertyId")]
        public virtual Property? Property { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("BenTypeId")]
        public virtual BeneficiaryType? BenType { get; set; }

        [ForeignKey("CommissionTypeId")]
        public virtual CommissionType? CommissionType { get; set; }

        [ForeignKey("BenStatusId")]
        public virtual BeneficiaryStatusType? BenStatus { get; set; }

        public virtual ICollection<BeneficiaryPayment> Payments { get; set; } = new List<BeneficiaryPayment>();

        public virtual ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();

        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();

        public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();

        // Helper property to get primary email
        [NotMapped]
        public string? PrimaryEmail => EmailAddresses?.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;

        // Helper property to get primary contact number
        [NotMapped]
        public string? PrimaryContactNumber => ContactNumbers?.FirstOrDefault(c => c.IsPrimary)?.Number;
    }

    #endregion

    #region Owners

    public class PropertyOwner
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public int PropertyOwnerTypeId { get; set; } // FK to PropertyOwnerType (Individual, Company, Trust, etc.)

        public int StatusId { get; set; } // FK to PropertyOwnerStatusType

        // Individual owner fields
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? IdNumber { get; set; }

        // Company/Organization owner fields
        [StringLength(200)]
        public string? CompanyName { get; set; }

        [StringLength(50)]
        public string? RegistrationNumber { get; set; }

        [StringLength(50)]
        public string? VatNumber { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        // Using Email and ContactNumber collections
        public virtual ICollection<Email> EmailAddresses { get; set; } = new List<Email>();
        public virtual ICollection<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();

        // Address
        public Address Address { get; set; } = new Address();

        // Bank details
        public BankAccount BankAccount { get; set; } = new BankAccount();

        // References
        [StringLength(50)]
        public string? CustomerRef { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public bool IsRemoved { get; set; }

        public DateTime? RemovedDate { get; set; }

        [StringLength(100)]
        public string? RemovedBy { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("PropertyOwnerTypeId")]
        public virtual PropertyOwnerType? OwnerType { get; set; }

        [ForeignKey("StatusId")]
        public virtual PropertyOwnerStatusType? Status { get; set; }

        public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

        public virtual ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();

        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();

        public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();

        // Helper properties
        [NotMapped]
        public string DisplayName => PropertyOwnerTypeId == 1 ? $"{FirstName} {LastName}" : CompanyName;

        [NotMapped]
        public string? PrimaryEmail => EmailAddresses?.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;

        [NotMapped]
        public string? PrimaryContactNumber => ContactNumbers?.FirstOrDefault(c => c.IsPrimary)?.Number;
    }

    #endregion

    #region Tenants

    public class PropertyTenant
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }

        public int CompanyId { get; set; }

        public int TenantTypeId { get; set; } // FK to TenantType (Individual, Company, etc.)

        // Individual tenant fields
        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? IdNumber { get; set; }

        // Company/Organization tenant fields
        [StringLength(200)]
        public string? CompanyName { get; set; }

        [StringLength(50)]
        public string? RegistrationNumber { get; set; }

        [StringLength(50)]
        public string? VatNumber { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        // Using Email and ContactNumber collections
        public virtual ICollection<Email> EmailAddresses { get; set; } = new List<Email>();
        public virtual ICollection<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();

        // Lease details
        public DateTime LeaseStartDate { get; set; }

        public DateTime LeaseEndDate { get; set; }

        public decimal RentAmount { get; set; }

        public decimal? DepositAmount { get; set; }

        public int DebitDayOfMonth { get; set; } = 1;

        public int StatusId { get; set; } // FK to TenantStatusType

        // Payment details
        public decimal Balance { get; set; } = 0;

        public decimal? DepositBalance { get; set; }

        public DateTime? LastPaymentDate { get; set; }

        public DateTime? LastInvoiceDate { get; set; }

        public DateTime? LastReminderDate { get; set; }

        // Bank details
        public BankAccount BankAccount { get; set; } = new BankAccount();

        // Current address (if different from property)
        public Address Address { get; set; } = new Address();

        // Emergency contact
        [StringLength(100)]
        public string? EmergencyContactName { get; set; }

        [StringLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [StringLength(100)]
        public string? EmergencyContactRelationship { get; set; }

        // References
        [StringLength(50)]
        public string? CustomerRefTenant { get; set; }

        [StringLength(50)]
        public string? CustomerRefProperty { get; set; }

        [StringLength(100)]
        public string? ResponsibleAgent { get; set; }

        [StringLength(100)]
        public string? ResponsibleUser { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; }

        // Lease Document CDN Integration
        public int? LeaseDocumentId { get; set; } // FK to CdnFileMetadata

        // Move-in/out
        public DateTime? MoveInDate { get; set; }

        public DateTime? MoveOutDate { get; set; }

        public bool MoveInInspectionCompleted { get; set; } = false;

        public int? MoveInInspectionId { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public bool IsRemoved { get; set; } = false;

        public DateTime? RemovedDate { get; set; }

        [StringLength(100)]
        public string? RemovedBy { get; set; }

        // Navigation properties
        [ForeignKey("PropertyId")]
        public virtual Property? Property { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("MoveInInspectionId")]
        public virtual PropertyInspection? MoveInInspection { get; set; }

        [ForeignKey("StatusId")]
        public virtual TenantStatusType? Status { get; set; }

        [ForeignKey("TenantTypeId")]
        public virtual TenantType? TenantType { get; set; }

        [ForeignKey("LeaseDocumentId")]
        public virtual CdnFileMetadata? LeaseDocument { get; set; }

        public virtual ICollection<PropertyPayment> Payments { get; set; } = new List<PropertyPayment>();

        public virtual ICollection<MaintenanceTicket> MaintenanceRequests { get; set; } = new List<MaintenanceTicket>();

        public virtual ICollection<PaymentSchedule> PaymentSchedules { get; set; } = new List<PaymentSchedule>();

        public virtual ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();

        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();

        public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();

        // Helper properties
        [NotMapped]
        public string DisplayName => TenantTypeId == 1 ? $"{FirstName} {LastName}" : CompanyName;

        [NotMapped]
        public bool IsLeaseActive => StatusId == 1 && // Assuming 1 is Active status
                                    LeaseEndDate >= DateTime.Today &&
                                    LeaseStartDate <= DateTime.Today;

        [NotMapped]
        public string? PrimaryEmail => EmailAddresses?.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;

        [NotMapped]
        public string? PrimaryContactNumber => ContactNumbers?.FirstOrDefault(c => c.IsPrimary)?.Number;
    }

    #endregion

    #region Inspections

    public class PropertyInspection
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string? InspectionCode { get; set; } // Unique inspection identifier

        public int InspectionTypeId { get; set; } // FK to InspectionType

        public int StatusId { get; set; } // FK to InspectionStatusType

        public DateTime ScheduledDate { get; set; }

        public DateTime? ActualDate { get; set; }

        [StringLength(100)]
        public string? InspectorName { get; set; }

        [StringLength(100)]
        public string? InspectorUserId { get; set; }

        [StringLength(2000)]
        public string? GeneralNotes { get; set; }

        // Overall ratings
        public int? OverallRating { get; set; } // 1-5 scale

        public int? OverallConditionId { get; set; } // FK to ConditionLevel

        // Tenant information (if applicable)
        [StringLength(100)]
        public string? TenantName { get; set; }

        public bool? TenantPresent { get; set; }

        // Report generation - CDN Integration
        public int? ReportDocumentId { get; set; } // FK to CdnFileMetadata

        // Next inspection scheduling
        public DateTime? NextInspectionDue { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("PropertyId")]
        public virtual Property? Property { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("InspectionTypeId")]
        public virtual InspectionType? InspectionType { get; set; }

        [ForeignKey("StatusId")]
        public virtual InspectionStatusType? Status { get; set; }

        [ForeignKey("OverallConditionId")]
        public virtual ConditionLevel? OverallCondition { get; set; }

        [ForeignKey("ReportDocumentId")]
        public virtual CdnFileMetadata? ReportDocument { get; set; }

        public virtual ICollection<InspectionItem> InspectionItems { get; set; } = new List<InspectionItem>();

        public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();
    }

    public class InspectionItem
    {
        [Key]
        public int Id { get; set; }

        public int InspectionId { get; set; }

        [Required]
        [StringLength(100)]
        public string? ItemName { get; set; } // e.g., "Kitchen", "Bathroom", "Windows"

        public int AreaId { get; set; } // FK to InspectionArea

        public int ConditionId { get; set; } // FK to ConditionLevel

        public int? Rating { get; set; } // 1-5 scale

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool RequiresMaintenance { get; set; } = false;

        public int? MaintenancePriorityId { get; set; } // FK to MaintenancePriority

        [StringLength(1000)]
        public string? MaintenanceNotes { get; set; }

        // Images - CDN Integration
        public int? ImageId { get; set; } // FK to CdnFileMetadata

        // Navigation property
        [ForeignKey("InspectionId")]
        public virtual PropertyInspection? Inspection { get; set; }

        [ForeignKey("AreaId")]
        public virtual InspectionArea? Area { get; set; }

        [ForeignKey("ConditionId")]
        public virtual ConditionLevel? Condition { get; set; }

        [ForeignKey("MaintenancePriorityId")]
        public virtual MaintenancePriority? MaintenancePriority { get; set; }

        [ForeignKey("ImageId")]
        public virtual CdnFileMetadata? Image { get; set; }
    }

    #endregion

    #region Maintenance

    public class MaintenanceTicket
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }

        public int CompanyId { get; set; }

        public int? TenantId { get; set; }

        public int? InspectionId { get; set; } // If created from inspection

        [Required]
        [StringLength(100)]
        public string? TicketNumber { get; set; } // Unique ticket identifier

        [Required]
        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public int CategoryId { get; set; } // FK to MaintenanceCategory

        public int PriorityId { get; set; } // FK to MaintenancePriority

        public int StatusId { get; set; } // FK to MaintenanceStatusType

        // Assignment
        [StringLength(100)]
        public string? AssignedToUserId { get; set; }

        [StringLength(100)]
        public string? AssignedToName { get; set; }

        public int? VendorId { get; set; }

        // Scheduling
        public DateTime? ScheduledDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public TimeSpan? EstimatedDuration { get; set; }

        public TimeSpan? ActualDuration { get; set; }

        // Costs
        public decimal? EstimatedCost { get; set; }

        public decimal? ActualCost { get; set; }

        public bool? TenantResponsible { get; set; }

        // Approval
        public bool RequiresApproval { get; set; } = false;

        public bool? IsApproved { get; set; }

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        // Access coordination
        public bool RequiresTenantAccess { get; set; } = false;

        public bool? TenantNotified { get; set; }

        public DateTime? TenantNotificationDate { get; set; }

        [StringLength(500)]
        public string? AccessInstructions { get; set; }

        // Completion details
        [StringLength(2000)]
        public string? CompletionNotes { get; set; }

        public bool? IssueResolved { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("PropertyId")]
        public virtual Property? Property { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("TenantId")]
        public virtual PropertyTenant? Tenant { get; set; }

        [ForeignKey("InspectionId")]
        public virtual PropertyInspection? Inspection { get; set; }

        [ForeignKey("VendorId")]
        public virtual Vendor? Vendor { get; set; }

        [ForeignKey("CategoryId")]
        public virtual MaintenanceCategory? Category { get; set; }

        [ForeignKey("PriorityId")]
        public virtual MaintenancePriority? Priority { get; set; }

        [ForeignKey("StatusId")]
        public virtual MaintenanceStatusType? Status { get; set; }

        public virtual ICollection<MaintenanceComment> Comments { get; set; } = new List<MaintenanceComment>();

        public virtual ICollection<MaintenanceExpense> Expenses { get; set; } = new List<MaintenanceExpense>();

        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();
    }

    public class MaintenanceComment
    {
        [Key]
        public int Id { get; set; }

        public int MaintenanceTicketId { get; set; }

        [Required]
        [StringLength(2000)]
        public string? Comment { get; set; }

        public bool IsInternal { get; set; } = false; // Internal notes vs. tenant-visible

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation property
        [ForeignKey("MaintenanceTicketId")]
        public virtual MaintenanceTicket? MaintenanceTicket { get; set; }
    }

    public class MaintenanceExpense
    {
        [Key]
        public int Id { get; set; }

        public int MaintenanceTicketId { get; set; }

        [Required]
        [StringLength(200)]
        public string? Description { get; set; }

        public int CategoryId { get; set; } // FK to ExpenseCategory

        public decimal Amount { get; set; }

        public int? VendorId { get; set; }

        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        public DateTime? InvoiceDate { get; set; }

        // Receipt/Invoice attachment - CDN Integration
        public int? ReceiptDocumentId { get; set; } // FK to CdnFileMetadata

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("MaintenanceTicketId")]
        public virtual MaintenanceTicket? MaintenanceTicket { get; set; }

        [ForeignKey("VendorId")]
        public virtual Vendor? Vendor { get; set; }

        [ForeignKey("CategoryId")]
        public virtual ExpenseCategory? Category { get; set; }

        [ForeignKey("ReceiptDocumentId")]
        public virtual CdnFileMetadata? ReceiptDocument { get; set; }
    }

    public class Vendor
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        // Using Email and ContactNumber collections
        public virtual ICollection<Email> EmailAddresses { get; set; } = new List<Email>();
        public virtual ICollection<ContactNumber> ContactNumbers { get; set; } = new List<ContactNumber>();

        public Address Address { get; set; } = new Address();

        // Specializations
        [StringLength(500)]
        public string? Specializations { get; set; } // Comma-separated

        public bool IsPreferred { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // Rating
        public decimal? Rating { get; set; } // Average rating

        public int? TotalJobs { get; set; }

        // Banking details
        public BankAccount BankAccount { get; set; } = new BankAccount();

        // Insurance
        public bool HasInsurance { get; set; } = false;

        [StringLength(50)]
        public string? InsurancePolicyNumber { get; set; }

        public DateTime? InsuranceExpiryDate { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

        public virtual ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();

        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();

        public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();

        // Helper properties
        [NotMapped]
        public string? PrimaryEmail => EmailAddresses?.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;

        [NotMapped]
        public string? PrimaryContactNumber => ContactNumbers?.FirstOrDefault(c => c.IsPrimary)?.Number;
    }

    #endregion

    #region Payments

    public class PropertyPayment
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }

        public int CompanyId { get; set; }

        public int? TenantId { get; set; }

        [Required]
        [StringLength(100)]
        public string? PaymentReference { get; set; } // Unique payment reference

        public int PaymentTypeId { get; set; } // FK to PaymentType

        public decimal Amount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "ZAR";

        public int StatusId { get; set; } // FK to PaymentStatusType

        public int? PaymentMethodId { get; set; } // FK to PaymentMethod

        public DateTime DueDate { get; set; }

        public DateTime? PaymentDate { get; set; }

        // Bank transaction details
        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(100)]
        public string? BankReference { get; set; }

        // Late payment
        public bool IsLate { get; set; } = false;

        public int? DaysLate { get; set; }

        public decimal? LateFee { get; set; }

        // Processing fees
        public decimal? ProcessingFee { get; set; }

        public decimal NetAmount { get; set; } // Amount minus fees

        // Allocation
        public bool IsAllocated { get; set; } = false;

        public DateTime? AllocationDate { get; set; }

        [StringLength(100)]
        public string? AllocatedBy { get; set; }


        // Receipt - CDN Integration
        public int? ReceiptDocumentId { get; set; } // FK to CdnFileMetadata

        [StringLength(100)]
        public string? ReceiptNumber { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("PropertyId")]
        public virtual Property? Property { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("TenantId")]
        public virtual PropertyTenant? Tenant { get; set; }

        [ForeignKey("PaymentTypeId")]
        public virtual PaymentType? PaymentType { get; set; }

        [ForeignKey("StatusId")]
        public virtual PaymentStatusType? Status { get; set; }

        [ForeignKey("PaymentMethodId")]
        public virtual PaymentMethod? PaymentMethod { get; set; }

        [ForeignKey("ReceiptDocumentId")]
        public virtual CdnFileMetadata? ReceiptDocument { get; set; }

        public virtual ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();

        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

        public virtual ICollection<EntityDocument> Documents { get; set; } = new List<EntityDocument>();
    }

    public class PaymentAllocation
    {
        [Key]
        public int Id { get; set; }

        public int PaymentId { get; set; }

        public int? BeneficiaryId { get; set; }

        public int AllocationTypeId { get; set; } // FK to AllocationType

        public decimal Amount { get; set; }

        public decimal Percentage { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public DateTime AllocationDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? AllocatedBy { get; set; }

        // Navigation properties
        [ForeignKey("PaymentId")]
        public virtual PropertyPayment? Payment { get; set; }

        [ForeignKey("BeneficiaryId")]
        public virtual PropertyBeneficiary? Beneficiary { get; set; }

        [ForeignKey("AllocationTypeId")]
        public virtual AllocationType? AllocationType { get; set; }
    }

    public class BeneficiaryPayment
    {
        [Key]
        public int Id { get; set; }

        public int BeneficiaryId { get; set; }

        public int? PaymentAllocationId { get; set; }

        [Required]
        [StringLength(100)]
        public string? PaymentReference { get; set; }

        public decimal Amount { get; set; }

        public int StatusId { get; set; } // FK to BeneficiaryPaymentStatusType

        public DateTime? PaymentDate { get; set; }

        [StringLength(100)]
        public string? TransactionReference { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("BeneficiaryId")]
        public virtual PropertyBeneficiary? Beneficiary { get; set; }

        [ForeignKey("PaymentAllocationId")]
        public virtual PaymentAllocation? PaymentAllocation { get; set; }

        [ForeignKey("StatusId")]
        public virtual BeneficiaryPaymentStatusType? Status { get; set; }
    }

    public class PaymentSchedule
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }

        public int TenantId { get; set; }

        [Required]
        [StringLength(100)]
        public string? ScheduleName { get; set; }

        public int FrequencyId { get; set; } // FK to PaymentFrequency

        public int DayOfMonth { get; set; } // 1-31

        public decimal Amount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Auto-generation settings
        public bool AutoGenerate { get; set; } = true;

        public int DaysBeforeDue { get; set; } = 5; // Generate invoice X days before due

        // Last generated
        public DateTime? LastGeneratedDate { get; set; }

        public DateTime? NextDueDate { get; set; }

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("PropertyId")]
        public virtual Property? Property { get; set; }

        [ForeignKey("TenantId")]
        public virtual PropertyTenant? Tenant { get; set; }

        [ForeignKey("FrequencyId")]
        public virtual PaymentFrequency? Frequency { get; set; }
    }

    public class PaymentRule
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string? RuleName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int RuleTypeId { get; set; } // FK to PaymentRuleType

        public bool IsActive { get; set; } = true;

        // Late payment rules
        public int? GracePeriodDays { get; set; }

        public decimal? LateFeeAmount { get; set; }

        public decimal? LateFeePercentage { get; set; }

        public bool CompoundLateFees { get; set; } = false;

        // Reminder rules
        public bool SendReminders { get; set; } = true;

        public int? FirstReminderDays { get; set; } // Days before due date

        public int? SecondReminderDays { get; set; }

        public int? FinalReminderDays { get; set; }

        // Allocation rules
        public bool AutoAllocate { get; set; } = true;

        [StringLength(1000)]
        public string? AllocationOrder { get; set; } // JSON configuration

        // Audit fields
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation property
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("RuleTypeId")]
        public virtual PaymentRuleType? RuleType { get; set; }
    }

    #endregion

    #region Notifications

    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int NotificationEventTypeId { get; set; } // FK to NotificationEventType

        [Required]
        [StringLength(200)]
        public string? Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string? Message { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ReadDate { get; set; }

        public bool IsRead { get; set; } = false;

        // Recipient details
        [StringLength(50)]
        public string? RecipientUserId { get; set; } // FK to ApplicationUser.Id

        // If sent via email/SMS
        public bool EmailSent { get; set; } = false;

        public bool SmsSent { get; set; } = false;

        public DateTime? EmailSentDate { get; set; }

        public DateTime? SmsSentDate { get; set; }

        // Related entity
        [StringLength(20)]
        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        [StringLength(100)]
        public string? RelatedEntityReference { get; set; } // E.g., property code, tenant name

        // Navigation property
        [ForeignKey("NotificationEventTypeId")]
        public virtual NotificationEventType? NotificationEventType { get; set; }
    }

    #endregion
}