using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.UserCompanyModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.BusinessMappingModels;
using Roovia.Models.UserCompanyMappingModels;
using Roovia.Models.ProjectCdnConfigModels;

namespace Roovia.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        #region Company Organization Structure

        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        // ApplicationUser is handled by Identity Framework as AspNetUsers

        #endregion

        #region Custom Authorization System

        public DbSet<Role> Roles { get; set; } // Renamed to avoid conflict with AspNetRoles
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; }

        #endregion

        #region Property Management Core

        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyOwner> PropertyOwners { get; set; }
        public DbSet<PropertyTenant> PropertyTenants { get; set; }
        public DbSet<PropertyBeneficiary> PropertyBeneficiaries { get; set; }

        #endregion

        #region Inspection & Maintenance

        public DbSet<PropertyInspection> PropertyInspections { get; set; }
        public DbSet<InspectionItem> InspectionItems { get; set; }
        public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }
        public DbSet<MaintenanceComment> MaintenanceComments { get; set; }
        public DbSet<MaintenanceExpense> MaintenanceExpenses { get; set; }
        public DbSet<Vendor> Vendors { get; set; }

        #endregion

        #region Financial & Payments

        public DbSet<PropertyPayment> PropertyPayments { get; set; }
        public DbSet<PaymentAllocation> PaymentAllocations { get; set; }
        public DbSet<BeneficiaryPayment> BeneficiaryPayments { get; set; }
        public DbSet<PaymentSchedule> PaymentSchedules { get; set; }
        public DbSet<PaymentRule> PaymentRules { get; set; }

        #endregion

        #region Communication & Contact (Helper Models)

        public DbSet<Email> Emails { get; set; }
        public DbSet<ContactNumber> ContactNumbers { get; set; }
        public DbSet<Media> Media { get; set; }

        #endregion

        #region Business Mapping/Lookup Models

        // Property Mappings
        public DbSet<PropertyStatusType> PropertyStatusTypes { get; set; }
        public DbSet<CommissionType> CommissionTypes { get; set; }
        public DbSet<PropertyImageType> PropertyImageTypes { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<DocumentAccessLevel> DocumentAccessLevels { get; set; }

        // Beneficiary & Tenant Mappings
        public DbSet<BeneficiaryType> BeneficiaryTypes { get; set; }
        public DbSet<BeneficiaryStatusType> BeneficiaryStatusTypes { get; set; }
        public DbSet<TenantStatusType> TenantStatusTypes { get; set; }

        // Inspection Mappings
        public DbSet<InspectionType> InspectionTypes { get; set; }
        public DbSet<InspectionStatusType> InspectionStatusTypes { get; set; }
        public DbSet<InspectionArea> InspectionAreas { get; set; }
        public DbSet<ConditionLevel> ConditionLevels { get; set; }

        // Maintenance Mappings
        public DbSet<MaintenanceCategory> MaintenanceCategories { get; set; }
        public DbSet<MaintenancePriority> MaintenancePriorities { get; set; }
        public DbSet<MaintenanceStatusType> MaintenanceStatusTypes { get; set; }
        public DbSet<MaintenanceImageType> MaintenanceImageTypes { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }

        // Payment Mappings
        public DbSet<PaymentType> PaymentTypes { get; set; }
        public DbSet<PaymentStatusType> PaymentStatusTypes { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<AllocationType> AllocationTypes { get; set; }
        public DbSet<BeneficiaryPaymentStatusType> BeneficiaryPaymentStatusTypes { get; set; }
        public DbSet<PaymentFrequency> PaymentFrequencies { get; set; }
        public DbSet<PaymentRuleType> PaymentRuleTypes { get; set; }

        #endregion

        #region User & Company Mapping Models

        public DbSet<CompanyStatusType> CompanyStatusTypes { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<BranchStatusType> BranchStatusTypes { get; set; }
        public DbSet<UserStatusType> UserStatusTypes { get; set; }
        public DbSet<TwoFactorMethod> TwoFactorMethods { get; set; }
        public DbSet<PermissionCategory> PermissionCategories { get; set; }
        public DbSet<RoleType> RoleTypes { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<NotificationChannel> NotificationChannels { get; set; }
        public DbSet<ThemeType> ThemeTypes { get; set; }

        #endregion

        #region CDN & File Management

        public DbSet<CdnConfiguration> CdnConfigurations { get; set; }
        public DbSet<CdnCategory> CdnCategories { get; set; }
        public DbSet<CdnFolder> CdnFolders { get; set; }
        public DbSet<CdnFileMetadata> CdnFileMetadata { get; set; }
        public DbSet<CdnBase64Storage> CdnBase64Storage { get; set; }
        public DbSet<CdnUsageStatistic> CdnUsageStatistics { get; set; }
        public DbSet<CdnAccessLog> CdnAccessLogs { get; set; }

        #endregion

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region Company Organization Structure Configuration

            // Company configuration
            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("AspNetCompanies");
                entity.OwnsOne(c => c.Address);
                entity.OwnsOne(c => c.BankAccount);

                entity.HasOne(c => c.MainLogo)
                    .WithMany()
                    .HasForeignKey(c => c.MainLogoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Branch configuration
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.ToTable("AspNetBranches");
                entity.OwnsOne(b => b.Address);
                entity.OwnsOne(b => b.BankAccount);

                entity.HasOne(b => b.MainLogo)
                    .WithMany()
                    .HasForeignKey(b => b.MainLogoId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(b => b.Company)
                    .WithMany(c => c.Branches)
                    .HasForeignKey(b => b.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ApplicationUser configuration (extends IdentityUser)
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                // This inherits from IdentityUser and uses AspNetUsers table
                // No need to specify table name as Identity handles it

                entity.HasOne(u => u.ProfilePicture)
                    .WithMany()
                    .HasForeignKey(u => u.ProfilePictureId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.Company)
                    .WithMany(c => c.Users)
                    .HasForeignKey(u => u.CompanyId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Branch)
                    .WithMany(b => b.Users)
                    .HasForeignKey(u => u.BranchId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region Custom Authorization System Configuration

            // Custom Role configuration (not Identity Role)
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("AspNetCustomRoles");
            });

            // Permission configuration
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("AspNetPermissions");
            });

            // RolePermission configuration
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("AspNetRolePermissions");

                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.Permissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserRoleAssignment configuration
            modelBuilder.Entity<UserRoleAssignment>(entity =>
            {
                entity.ToTable("AspNetUserRoleAssignments");

                entity.HasOne(ura => ura.User)
                    .WithMany(u => u.CustomRoles)
                    .HasForeignKey(ura => ura.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ura => ura.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ura => ura.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserPermissionOverride configuration
            modelBuilder.Entity<UserPermissionOverride>(entity =>
            {
                entity.ToTable("AspNetUserPermissionOverrides");

                entity.HasOne(upo => upo.User)
                    .WithMany(u => u.PermissionOverrides)
                    .HasForeignKey(upo => upo.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(upo => upo.Permission)
                    .WithMany(p => p.UserPermissionOverrides)
                    .HasForeignKey(upo => upo.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            #endregion

            #region Property Management Core Configuration

            // Property configuration
            modelBuilder.Entity<Property>(entity =>
            {
                entity.ToTable("Data_Properties");
                entity.OwnsOne(p => p.Address);

                entity.HasOne(p => p.Owner)
                    .WithMany(o => o.Properties)
                    .HasForeignKey(p => p.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.MainImage)
                    .WithMany()
                    .HasForeignKey(p => p.MainImageId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.Company)
                    .WithMany()
                    .HasForeignKey(p => p.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Branch)
                    .WithMany()
                    .HasForeignKey(p => p.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Property Owner configuration
            modelBuilder.Entity<PropertyOwner>(entity =>
            {
                entity.ToTable("Data_PropertyOwners");
                entity.OwnsOne(po => po.Address);
                entity.OwnsOne(po => po.BankAccount);

                entity.HasOne(po => po.Company)
                    .WithMany()
                    .HasForeignKey(po => po.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Property Tenant configuration
            modelBuilder.Entity<PropertyTenant>(entity =>
            {
                entity.ToTable("Data_PropertyTenants");
                entity.OwnsOne(pt => pt.Address);
                entity.OwnsOne(pt => pt.BankAccount);

                entity.HasOne(pt => pt.LeaseDocument)
                    .WithMany()
                    .HasForeignKey(pt => pt.LeaseDocumentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(pt => pt.Property)
                    .WithMany(p => p.Tenants)
                    .HasForeignKey(pt => pt.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pt => pt.Company)
                    .WithMany()
                    .HasForeignKey(pt => pt.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Property Beneficiary configuration
            modelBuilder.Entity<PropertyBeneficiary>(entity =>
            {
                entity.ToTable("Data_PropertyBeneficiaries");
                entity.OwnsOne(pb => pb.Address);
                entity.OwnsOne(pb => pb.BankAccount);

                entity.HasOne(pb => pb.Property)
                    .WithMany(p => p.Beneficiaries)
                    .HasForeignKey(pb => pb.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pb => pb.Company)
                    .WithMany()
                    .HasForeignKey(pb => pb.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region Inspection & Maintenance Configuration

            // Property Inspection configuration
            modelBuilder.Entity<PropertyInspection>(entity =>
            {
                entity.ToTable("Inspect_PropertyInspections");

                entity.HasOne(pi => pi.ReportDocument)
                    .WithMany()
                    .HasForeignKey(pi => pi.ReportDocumentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(pi => pi.Property)
                    .WithMany(p => p.Inspections)
                    .HasForeignKey(pi => pi.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pi => pi.Company)
                    .WithMany()
                    .HasForeignKey(pi => pi.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Inspection Item configuration
            modelBuilder.Entity<InspectionItem>(entity =>
            {
                entity.ToTable("Inspect_InspectionItems");

                entity.HasOne(ii => ii.Inspection)
                    .WithMany(i => i.InspectionItems)
                    .HasForeignKey(ii => ii.InspectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Maintenance Ticket configuration
            modelBuilder.Entity<MaintenanceTicket>(entity =>
            {
                entity.ToTable("Maint_MaintenanceTickets");

                entity.HasOne(mt => mt.Property)
                    .WithMany(p => p.MaintenanceTickets)
                    .HasForeignKey(mt => mt.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(mt => mt.Company)
                    .WithMany()
                    .HasForeignKey(mt => mt.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(mt => mt.Tenant)
                    .WithMany(t => t.MaintenanceRequests)
                    .HasForeignKey(mt => mt.TenantId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(mt => mt.Vendor)
                    .WithMany(v => v.MaintenanceTickets)
                    .HasForeignKey(mt => mt.VendorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Maintenance Comment configuration
            modelBuilder.Entity<MaintenanceComment>(entity =>
            {
                entity.ToTable("Maint_MaintenanceComments");

                entity.HasOne(mc => mc.MaintenanceTicket)
                    .WithMany(mt => mt.Comments)
                    .HasForeignKey(mc => mc.MaintenanceTicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Maintenance Expense configuration
            modelBuilder.Entity<MaintenanceExpense>(entity =>
            {
                entity.ToTable("Maint_MaintenanceExpenses");

                entity.HasOne(me => me.ReceiptDocument)
                    .WithMany()
                    .HasForeignKey(me => me.ReceiptDocumentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(me => me.MaintenanceTicket)
                    .WithMany(mt => mt.Expenses)
                    .HasForeignKey(me => me.MaintenanceTicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(me => me.Vendor)
                    .WithMany()
                    .HasForeignKey(me => me.VendorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Vendor configuration
            modelBuilder.Entity<Vendor>(entity =>
            {
                entity.ToTable("Maint_Vendors");
                entity.OwnsOne(v => v.Address);
                entity.OwnsOne(v => v.BankAccount);

                entity.HasOne(v => v.Company)
                    .WithMany()
                    .HasForeignKey(v => v.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region Financial & Payments Configuration

            // Property Payment configuration
            modelBuilder.Entity<PropertyPayment>(entity =>
            {
                entity.ToTable("Finance_PropertyPayments");

                entity.HasOne(pp => pp.ReceiptDocument)
                    .WithMany()
                    .HasForeignKey(pp => pp.ReceiptDocumentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(pp => pp.Property)
                    .WithMany(p => p.Payments)
                    .HasForeignKey(pp => pp.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pp => pp.Company)
                    .WithMany()
                    .HasForeignKey(pp => pp.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pp => pp.Tenant)
                    .WithMany(t => t.Payments)
                    .HasForeignKey(pp => pp.TenantId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Payment Allocation configuration
            modelBuilder.Entity<PaymentAllocation>(entity =>
            {
                entity.ToTable("Finance_PaymentAllocations");

                entity.HasOne(pa => pa.Payment)
                    .WithMany(p => p.Allocations)
                    .HasForeignKey(pa => pa.PaymentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pa => pa.Beneficiary)
                    .WithMany()
                    .HasForeignKey(pa => pa.BeneficiaryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Beneficiary Payment configuration
            modelBuilder.Entity<BeneficiaryPayment>(entity =>
            {
                entity.ToTable("Finance_BeneficiaryPayments");

                entity.HasOne(bp => bp.Beneficiary)
                    .WithMany(b => b.Payments)
                    .HasForeignKey(bp => bp.BeneficiaryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(bp => bp.PaymentAllocation)
                    .WithMany()
                    .HasForeignKey(bp => bp.PaymentAllocationId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Payment Schedule configuration
            modelBuilder.Entity<PaymentSchedule>(entity =>
            {
                entity.ToTable("Finance_PaymentSchedules");

                entity.HasOne(ps => ps.Property)
                    .WithMany()
                    .HasForeignKey(ps => ps.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ps => ps.Tenant)
                    .WithMany(t => t.PaymentSchedules)
                    .HasForeignKey(ps => ps.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment Rule configuration
            modelBuilder.Entity<PaymentRule>(entity =>
            {
                entity.ToTable("Finance_PaymentRules");

                entity.HasOne(pr => pr.Company)
                    .WithMany()
                    .HasForeignKey(pr => pr.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region Communication & Contact Configuration

            // Email configuration
            modelBuilder.Entity<Email>(entity =>
            {
                entity.ToTable("Contact_Emails");
                entity.Property(e => e.Id).UseIdentityColumn();

                // Configure unique indexes for primary emails
                entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId, e.IsPrimary })
                    .HasFilter("[IsPrimary] = 1 AND [RelatedEntityId] IS NOT NULL")
                    .IsUnique();

                entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityStringId, e.IsPrimary })
                    .HasFilter("[IsPrimary] = 1 AND [RelatedEntityStringId] IS NOT NULL")
                    .IsUnique();

                // Configure relationships
                entity.HasOne<ApplicationUser>()
                    .WithMany(u => u.EmailAddresses)
                    .HasForeignKey(e => e.ApplicationUserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Company>()
                    .WithMany(c => c.EmailAddresses)
                    .HasForeignKey(e => e.CompanyId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Branch>()
                    .WithMany(b => b.EmailAddresses)
                    .HasForeignKey(e => e.BranchId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<PropertyOwner>()
                    .WithMany(po => po.EmailAddresses)
                    .HasForeignKey(e => e.PropertyOwnerId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<PropertyTenant>()
                    .WithMany(pt => pt.EmailAddresses)
                    .HasForeignKey(e => e.PropertyTenantId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<PropertyBeneficiary>()
                    .WithMany(pb => pb.EmailAddresses)
                    .HasForeignKey(e => e.PropertyBeneficiaryId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Vendor>()
                    .WithMany(v => v.EmailAddresses)
                    .HasForeignKey(e => e.VendorId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ContactNumber configuration
            modelBuilder.Entity<ContactNumber>(entity =>
            {
                entity.ToTable("Contact_ContactNumbers");
                entity.Property(c => c.Id).UseIdentityColumn();

                // Configure unique indexes for primary phone numbers
                entity.HasIndex(c => new { c.RelatedEntityType, c.RelatedEntityId, c.IsPrimary })
                    .HasFilter("[IsPrimary] = 1 AND [RelatedEntityId] IS NOT NULL")
                    .IsUnique();

                entity.HasIndex(c => new { c.RelatedEntityType, c.RelatedEntityStringId, c.IsPrimary })
                    .HasFilter("[IsPrimary] = 1 AND [RelatedEntityStringId] IS NOT NULL")
                    .IsUnique();

                // Configure relationships
                entity.HasOne<ApplicationUser>()
                    .WithMany(u => u.ContactNumbers)
                    .HasForeignKey(c => c.ApplicationUserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Company>()
                    .WithMany(c => c.ContactNumbers)
                    .HasForeignKey(c => c.CompanyId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Branch>()
                    .WithMany(b => b.ContactNumbers)
                    .HasForeignKey(c => c.BranchId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<PropertyOwner>()
                    .WithMany(po => po.ContactNumbers)
                    .HasForeignKey(c => c.PropertyOwnerId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<PropertyTenant>()
                    .WithMany(pt => pt.ContactNumbers)
                    .HasForeignKey(c => c.PropertyTenantId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<PropertyBeneficiary>()
                    .WithMany(pb => pb.ContactNumbers)
                    .HasForeignKey(c => c.PropertyBeneficiaryId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Vendor>()
                    .WithMany(v => v.ContactNumbers)
                    .HasForeignKey(c => c.VendorId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Media configuration
            modelBuilder.Entity<Media>(entity =>
            {
                entity.ToTable("Contact_Media");
            });

            #endregion

            #region Business Mapping Models Configuration

            // PropertyStatusType
            modelBuilder.Entity<PropertyStatusType>(entity =>
            {
                entity.ToTable("Lookup_PropertyStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // CommissionType
            modelBuilder.Entity<CommissionType>(entity =>
            {
                entity.ToTable("Lookup_CommissionTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PropertyImageType
            modelBuilder.Entity<PropertyImageType>(entity =>
            {
                entity.ToTable("Lookup_PropertyImageTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // DocumentType
            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.ToTable("Lookup_DocumentTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // DocumentCategory
            modelBuilder.Entity<DocumentCategory>(entity =>
            {
                entity.ToTable("Lookup_DocumentCategories");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // DocumentAccessLevel
            modelBuilder.Entity<DocumentAccessLevel>(entity =>
            {
                entity.ToTable("Lookup_DocumentAccessLevels");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // BeneficiaryType
            modelBuilder.Entity<BeneficiaryType>(entity =>
            {
                entity.ToTable("Lookup_BeneficiaryTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // BeneficiaryStatusType
            modelBuilder.Entity<BeneficiaryStatusType>(entity =>
            {
                entity.ToTable("Lookup_BeneficiaryStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // TenantStatusType
            modelBuilder.Entity<TenantStatusType>(entity =>
            {
                entity.ToTable("Lookup_TenantStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // InspectionType
            modelBuilder.Entity<InspectionType>(entity =>
            {
                entity.ToTable("Lookup_InspectionTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // InspectionStatusType
            modelBuilder.Entity<InspectionStatusType>(entity =>
            {
                entity.ToTable("Lookup_InspectionStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // InspectionArea
            modelBuilder.Entity<InspectionArea>(entity =>
            {
                entity.ToTable("Lookup_InspectionAreas");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // ConditionLevel
            modelBuilder.Entity<ConditionLevel>(entity =>
            {
                entity.ToTable("Lookup_ConditionLevels");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // MaintenanceCategory
            modelBuilder.Entity<MaintenanceCategory>(entity =>
            {
                entity.ToTable("Lookup_MaintenanceCategories");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // MaintenancePriority
            modelBuilder.Entity<MaintenancePriority>(entity =>
            {
                entity.ToTable("Lookup_MaintenancePriorities");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // MaintenanceStatusType
            modelBuilder.Entity<MaintenanceStatusType>(entity =>
            {
                entity.ToTable("Lookup_MaintenanceStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // MaintenanceImageType
            modelBuilder.Entity<MaintenanceImageType>(entity =>
            {
                entity.ToTable("Lookup_MaintenanceImageTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // ExpenseCategory
            modelBuilder.Entity<ExpenseCategory>(entity =>
            {
                entity.ToTable("Lookup_ExpenseCategories");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentType
            modelBuilder.Entity<PaymentType>(entity =>
            {
                entity.ToTable("Lookup_PaymentTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentStatusType
            modelBuilder.Entity<PaymentStatusType>(entity =>
            {
                entity.ToTable("Lookup_PaymentStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentMethod
            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.ToTable("Lookup_PaymentMethods");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // AllocationType
            modelBuilder.Entity<AllocationType>(entity =>
            {
                entity.ToTable("Lookup_AllocationTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // BeneficiaryPaymentStatusType
            modelBuilder.Entity<BeneficiaryPaymentStatusType>(entity =>
            {
                entity.ToTable("Lookup_BeneficiaryPaymentStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentFrequency
            modelBuilder.Entity<PaymentFrequency>(entity =>
            {
                entity.ToTable("Lookup_PaymentFrequencies");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentRuleType
            modelBuilder.Entity<PaymentRuleType>(entity =>
            {
                entity.ToTable("Lookup_PaymentRuleTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            #endregion

            #region User & Company Mapping Models Configuration

            // CompanyStatusType
            modelBuilder.Entity<CompanyStatusType>(entity =>
            {
                entity.ToTable("Lookup_CompanyStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // SubscriptionPlan
            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.ToTable("Lookup_SubscriptionPlans");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // BranchStatusType
            modelBuilder.Entity<BranchStatusType>(entity =>
            {
                entity.ToTable("Lookup_BranchStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // UserStatusType
            modelBuilder.Entity<UserStatusType>(entity =>
            {
                entity.ToTable("Lookup_UserStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // TwoFactorMethod
            modelBuilder.Entity<TwoFactorMethod>(entity =>
            {
                entity.ToTable("Lookup_TwoFactorMethods");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PermissionCategory
            modelBuilder.Entity<PermissionCategory>(entity =>
            {
                entity.ToTable("Lookup_PermissionCategories");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // RoleType
            modelBuilder.Entity<RoleType>(entity =>
            {
                entity.ToTable("Lookup_RoleTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // NotificationType
            modelBuilder.Entity<NotificationType>(entity =>
            {
                entity.ToTable("Lookup_NotificationTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // NotificationChannel
            modelBuilder.Entity<NotificationChannel>(entity =>
            {
                entity.ToTable("Lookup_NotificationChannels");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // ThemeType
            modelBuilder.Entity<ThemeType>(entity =>
            {
                entity.ToTable("Lookup_ThemeTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            #endregion

            #region CDN & File Management Configuration

            // CdnConfiguration
            modelBuilder.Entity<CdnConfiguration>(entity =>
            {
                entity.ToTable("CDN_Configurations");
                entity.Property(e => e.BaseUrl).IsRequired().HasMaxLength(255);
                entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AllowedFileTypes).IsRequired().HasMaxLength(500);
            });

            // CdnCategory
            modelBuilder.Entity<CdnCategory>(entity =>
            {
                entity.ToTable("CDN_Categories");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // CdnFolder
            modelBuilder.Entity<CdnFolder>(entity =>
            {
                entity.ToTable("CDN_Folders");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Path).IsRequired().HasMaxLength(500);

                entity.HasOne(f => f.Parent)
                    .WithMany(p => p.Children)
                    .HasForeignKey(f => f.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Category)
                    .WithMany(c => c.Folders)
                    .HasForeignKey(f => f.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.CategoryId, e.Path }).IsUnique();
            });

            // CdnFileMetadata
            modelBuilder.Entity<CdnFileMetadata>(entity =>
            {
                entity.ToTable("CDN_FileMetadata");
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Url).IsRequired().HasMaxLength(255);

                entity.HasOne(f => f.Category)
                    .WithMany(c => c.Files)
                    .HasForeignKey(f => f.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Folder)
                    .WithMany(f => f.Files)
                    .HasForeignKey(f => f.FolderId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Url);
                entity.HasIndex(e => e.IsDeleted);
                entity.HasIndex(e => new { e.CategoryId, e.FolderId });
            });

            // CdnBase64Storage
            modelBuilder.Entity<CdnBase64Storage>(entity =>
            {
                entity.ToTable("CDN_Base64Storage");
                entity.Property(e => e.Base64Data).IsRequired();
                entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);

                entity.HasOne(b => b.FileMetadata)
                    .WithOne(f => f.Base64Storage)
                    .HasForeignKey<CdnBase64Storage>(b => b.FileMetadataId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.FileMetadataId).IsUnique();
            });

            // CdnUsageStatistic
            modelBuilder.Entity<CdnUsageStatistic>(entity =>
            {
                entity.ToTable("CDN_UsageStatistics");

                entity.HasOne(s => s.Category)
                    .WithMany()
                    .HasForeignKey(s => s.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.Date, e.CategoryId });
            });

            // CdnAccessLog
            modelBuilder.Entity<CdnAccessLog>(entity =>
            {
                entity.ToTable("CDN_AccessLogs");
                entity.Property(e => e.ActionType).IsRequired().HasMaxLength(20);

                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ActionType);
            });

            #endregion

            #region Decimal Precision Configuration

            // PaymentMethod
            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.Property(e => e.ProcessingFeeFixed)
                    .HasPrecision(18, 2);

                entity.Property(e => e.ProcessingFeePercentage)
                    .HasPrecision(5, 2);
            });

            // BeneficiaryPayment
            modelBuilder.Entity<BeneficiaryPayment>(entity =>
            {
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);
            });

            // MaintenanceExpense
            modelBuilder.Entity<MaintenanceExpense>(entity =>
            {
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);
            });

            // MaintenanceTicket
            modelBuilder.Entity<MaintenanceTicket>(entity =>
            {
                entity.Property(e => e.ActualCost)
                    .HasPrecision(18, 2);

                entity.Property(e => e.EstimatedCost)
                    .HasPrecision(18, 2);
            });

            // PaymentAllocation
            modelBuilder.Entity<PaymentAllocation>(entity =>
            {
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Percentage)
                    .HasPrecision(5, 2);
            });

            // PaymentRule
            modelBuilder.Entity<PaymentRule>(entity =>
            {
                entity.Property(e => e.LateFeeAmount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.LateFeePercentage)
                    .HasPrecision(5, 2);
            });

            // PaymentSchedule
            modelBuilder.Entity<PaymentSchedule>(entity =>
            {
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);
            });

            // Property
            modelBuilder.Entity<Property>(entity =>
            {
                entity.Property(e => e.CommissionValue)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PropertyAccountBalance)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RentalAmount)
                    .HasPrecision(18, 2);
            });

            // PropertyBeneficiary
            modelBuilder.Entity<PropertyBeneficiary>(entity =>
            {
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.CommissionValue)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PropertyAmount)
                    .HasPrecision(18, 2);
            });

            // PropertyPayment
            modelBuilder.Entity<PropertyPayment>(entity =>
            {
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.LateFee)
                    .HasPrecision(18, 2);

                entity.Property(e => e.NetAmount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.ProcessingFee)
                    .HasPrecision(18, 2);
            });

            // PropertyTenant
            modelBuilder.Entity<PropertyTenant>(entity =>
            {
                entity.Property(e => e.Balance)
                    .HasPrecision(18, 2);

                entity.Property(e => e.DepositAmount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.DepositBalance)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RentAmount)
                    .HasPrecision(18, 2);
            });

            // Vendor
            modelBuilder.Entity<Vendor>(entity =>
            {
                entity.Property(e => e.Rating)
                    .HasPrecision(3, 2);
            });

            // SubscriptionPlan
            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.Property(e => e.Price)
                    .HasPrecision(18, 2);
            });

            #endregion
        }
    }
}