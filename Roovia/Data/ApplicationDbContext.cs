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
        #region Business Models

        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyOwner> PropertyOwners { get; set; }
        public DbSet<PropertyTenant> PropertyTenants { get; set; }
        public DbSet<PropertyBeneficiary> PropertyBeneficiaries { get; set; }
        public DbSet<PropertyInspection> PropertyInspections { get; set; }
        public DbSet<InspectionItem> InspectionItems { get; set; }
        public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }
        public DbSet<MaintenanceComment> MaintenanceComments { get; set; }
        public DbSet<MaintenanceExpense> MaintenanceExpenses { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<PropertyPayment> PropertyPayments { get; set; }
        public DbSet<PaymentAllocation> PaymentAllocations { get; set; }
        public DbSet<BeneficiaryPayment> BeneficiaryPayments { get; set; }
        public DbSet<PaymentSchedule> PaymentSchedules { get; set; }
        public DbSet<PaymentRule> PaymentRules { get; set; }

        #endregion

        #region User Company Models

        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; }

        #endregion

        #region Business Helper Models

        public DbSet<Email> Emails { get; set; }
        public DbSet<ContactNumber> ContactNumbers { get; set; }
        public DbSet<Media> Media { get; set; }

        #endregion

        #region Business Mapping Models

        public DbSet<PropertyStatusType> PropertyStatusTypes { get; set; }
        public DbSet<CommissionType> CommissionTypes { get; set; }
        public DbSet<PropertyImageType> PropertyImageTypes { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<DocumentAccessLevel> DocumentAccessLevels { get; set; }
        public DbSet<BeneficiaryType> BeneficiaryTypes { get; set; }
        public DbSet<BeneficiaryStatusType> BeneficiaryStatusTypes { get; set; }
        public DbSet<TenantStatusType> TenantStatusTypes { get; set; }
        public DbSet<InspectionType> InspectionTypes { get; set; }
        public DbSet<InspectionStatusType> InspectionStatusTypes { get; set; }
        public DbSet<InspectionArea> InspectionAreas { get; set; }
        public DbSet<ConditionLevel> ConditionLevels { get; set; }
        public DbSet<MaintenanceCategory> MaintenanceCategories { get; set; }
        public DbSet<MaintenancePriority> MaintenancePriorities { get; set; }
        public DbSet<MaintenanceStatusType> MaintenanceStatusTypes { get; set; }
        public DbSet<MaintenanceImageType> MaintenanceImageTypes { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<PaymentType> PaymentTypes { get; set; }
        public DbSet<PaymentStatusType> PaymentStatusTypes { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<AllocationType> AllocationTypes { get; set; }
        public DbSet<BeneficiaryPaymentStatusType> BeneficiaryPaymentStatusTypes { get; set; }
        public DbSet<PaymentFrequency> PaymentFrequencies { get; set; }
        public DbSet<PaymentRuleType> PaymentRuleTypes { get; set; }

        #endregion

        #region User Company Mapping Models

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

        #region CDN Models

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

            #region Business Models Configuration

            // Property configuration
            modelBuilder.Entity<Property>(entity =>
            {
                entity.ToTable("Properties");
                entity.OwnsOne(p => p.Address);

                entity.HasOne(p => p.Owner)
                    .WithMany(o => o.Properties)
                    .HasForeignKey(p => p.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.MainImage)
                    .WithMany()
                    .HasForeignKey(p => p.MainImageId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Property Owner configuration
            modelBuilder.Entity<PropertyOwner>(entity =>
            {
                entity.ToTable("PropertyOwners");
                entity.OwnsOne(po => po.Address);
                entity.OwnsOne(po => po.BankAccount);
            });

            // Property Tenant configuration
            modelBuilder.Entity<PropertyTenant>(entity =>
            {
                entity.ToTable("PropertyTenants");
                entity.OwnsOne(pt => pt.Address);
                entity.OwnsOne(pt => pt.BankAccount);

                entity.HasOne(pt => pt.LeaseDocument)
                    .WithMany()
                    .HasForeignKey(pt => pt.LeaseDocumentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Property Beneficiary configuration
            modelBuilder.Entity<PropertyBeneficiary>(entity =>
            {
                entity.ToTable("PropertyBeneficiaries");
                entity.OwnsOne(pb => pb.Address);
                entity.OwnsOne(pb => pb.BankAccount);
            });

            // Property Inspection configuration
            modelBuilder.Entity<PropertyInspection>(entity =>
            {
                entity.ToTable("PropertyInspections");

                entity.HasOne(pi => pi.ReportDocument)
                    .WithMany()
                    .HasForeignKey(pi => pi.ReportDocumentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Inspection Item configuration
            modelBuilder.Entity<InspectionItem>(entity =>
            {
                entity.ToTable("InspectionItems");
            });

            // Maintenance Ticket configuration
            modelBuilder.Entity<MaintenanceTicket>(entity =>
            {
                entity.ToTable("MaintenanceTickets");
            });

            // Maintenance Comment configuration
            modelBuilder.Entity<MaintenanceComment>(entity =>
            {
                entity.ToTable("MaintenanceComments");
            });

            // Maintenance Expense configuration
            modelBuilder.Entity<MaintenanceExpense>(entity =>
            {
                entity.ToTable("MaintenanceExpenses");

                entity.HasOne(me => me.ReceiptDocument)
                    .WithMany()
                    .HasForeignKey(me => me.ReceiptDocumentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Vendor configuration
            modelBuilder.Entity<Vendor>(entity =>
            {
                entity.ToTable("Vendors");
                entity.OwnsOne(v => v.Address);
                entity.OwnsOne(v => v.BankAccount);
            });

            // Property Payment configuration
            modelBuilder.Entity<PropertyPayment>(entity =>
            {
                entity.ToTable("PropertyPayments");

                entity.HasOne(pp => pp.ReceiptDocument)
                    .WithMany()
                    .HasForeignKey(pp => pp.ReceiptDocumentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Payment Allocation configuration
            modelBuilder.Entity<PaymentAllocation>(entity =>
            {
                entity.ToTable("PaymentAllocations");
            });

            // Beneficiary Payment configuration
            modelBuilder.Entity<BeneficiaryPayment>(entity =>
            {
                entity.ToTable("BeneficiaryPayments");
            });

            // Payment Schedule configuration
            modelBuilder.Entity<PaymentSchedule>(entity =>
            {
                entity.ToTable("PaymentSchedules");
            });

            // Payment Rule configuration
            modelBuilder.Entity<PaymentRule>(entity =>
            {
                entity.ToTable("PaymentRules");
            });

            #endregion

            #region User Company Models Configuration

            // Company configuration
            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("Companies");
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
                entity.ToTable("Branches");
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

            // ApplicationUser configuration
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");

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

            // Permission configuration
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permissions");
            });

            // Role configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
            });

            // RolePermission configuration
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("RolePermissions");

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
                entity.ToTable("UserRoleAssignments");

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
                entity.ToTable("UserPermissionOverrides");

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

            #region Business Helper Models Configuration

            // Email configuration
            modelBuilder.Entity<Email>(entity =>
            {
                entity.ToTable("Emails");
                entity.Property(e => e.Id).UseIdentityColumn();

                // Configure unique indexes for primary emails
                entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId, e.IsPrimary })
                    .HasFilter("[IsPrimary] = 1 AND [RelatedEntityId] IS NOT NULL")
                    .IsUnique();

                entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityStringId, e.IsPrimary })
                    .HasFilter("[IsPrimary] = 1 AND [RelatedEntityStringId] IS NOT NULL")
                    .IsUnique();

                // Configure relationships - prevent cascade delete cycles
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
                entity.ToTable("ContactNumbers");
                entity.Property(c => c.Id).UseIdentityColumn();

                // Configure unique indexes for primary phone numbers
                entity.HasIndex(c => new { c.RelatedEntityType, c.RelatedEntityId, c.IsPrimary })
                    .HasFilter("[IsPrimary] = 1 AND [RelatedEntityId] IS NOT NULL")
                    .IsUnique();

                entity.HasIndex(c => new { c.RelatedEntityType, c.RelatedEntityStringId, c.IsPrimary })
                    .HasFilter("[IsPrimary] = 1 AND [RelatedEntityStringId] IS NOT NULL")
                    .IsUnique();

                // Configure relationships - prevent cascade delete cycles
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
                entity.ToTable("Media");
            });

            #endregion

            #region Business Mapping Models Configuration

            // PropertyStatusType
            modelBuilder.Entity<PropertyStatusType>(entity =>
            {
                entity.ToTable("Mappings_PropertyStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // CommissionType
            modelBuilder.Entity<CommissionType>(entity =>
            {
                entity.ToTable("Mappings_CommissionTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PropertyImageType
            modelBuilder.Entity<PropertyImageType>(entity =>
            {
                entity.ToTable("Mappings_PropertyImageTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // DocumentType
            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.ToTable("Mappings_DocumentTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // DocumentCategory
            modelBuilder.Entity<DocumentCategory>(entity =>
            {
                entity.ToTable("Mappings_DocumentCategories");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // DocumentAccessLevel
            modelBuilder.Entity<DocumentAccessLevel>(entity =>
            {
                entity.ToTable("Mappings_DocumentAccessLevels");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // BeneficiaryType
            modelBuilder.Entity<BeneficiaryType>(entity =>
            {
                entity.ToTable("Mappings_BeneficiaryTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // BeneficiaryStatusType
            modelBuilder.Entity<BeneficiaryStatusType>(entity =>
            {
                entity.ToTable("Mappings_BeneficiaryStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // TenantStatusType
            modelBuilder.Entity<TenantStatusType>(entity =>
            {
                entity.ToTable("Mappings_TenantStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // InspectionType
            modelBuilder.Entity<InspectionType>(entity =>
            {
                entity.ToTable("Mappings_InspectionTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // InspectionStatusType
            modelBuilder.Entity<InspectionStatusType>(entity =>
            {
                entity.ToTable("Mappings_InspectionStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // InspectionArea
            modelBuilder.Entity<InspectionArea>(entity =>
            {
                entity.ToTable("Mappings_InspectionAreas");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // ConditionLevel
            modelBuilder.Entity<ConditionLevel>(entity =>
            {
                entity.ToTable("Mappings_ConditionLevels");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // MaintenanceCategory
            modelBuilder.Entity<MaintenanceCategory>(entity =>
            {
                entity.ToTable("Mappings_MaintenanceCategories");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // MaintenancePriority
            modelBuilder.Entity<MaintenancePriority>(entity =>
            {
                entity.ToTable("Mappings_MaintenancePriorities");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // MaintenanceStatusType
            modelBuilder.Entity<MaintenanceStatusType>(entity =>
            {
                entity.ToTable("Mappings_MaintenanceStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // MaintenanceImageType
            modelBuilder.Entity<MaintenanceImageType>(entity =>
            {
                entity.ToTable("Mappings_MaintenanceImageTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // ExpenseCategory
            modelBuilder.Entity<ExpenseCategory>(entity =>
            {
                entity.ToTable("Mappings_ExpenseCategories");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentType
            modelBuilder.Entity<PaymentType>(entity =>
            {
                entity.ToTable("Mappings_PaymentTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentStatusType
            modelBuilder.Entity<PaymentStatusType>(entity =>
            {
                entity.ToTable("Mappings_PaymentStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentMethod
            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.ToTable("Mappings_PaymentMethods");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // AllocationType
            modelBuilder.Entity<AllocationType>(entity =>
            {
                entity.ToTable("Mappings_AllocationTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // BeneficiaryPaymentStatusType
            modelBuilder.Entity<BeneficiaryPaymentStatusType>(entity =>
            {
                entity.ToTable("Mappings_BeneficiaryPaymentStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentFrequency
            modelBuilder.Entity<PaymentFrequency>(entity =>
            {
                entity.ToTable("Mappings_PaymentFrequencies");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PaymentRuleType
            modelBuilder.Entity<PaymentRuleType>(entity =>
            {
                entity.ToTable("Mappings_PaymentRuleTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            #endregion

            #region User Company Mapping Models Configuration

            // CompanyStatusType
            modelBuilder.Entity<CompanyStatusType>(entity =>
            {
                entity.ToTable("Mappings_CompanyStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // SubscriptionPlan
            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.ToTable("Mappings_SubscriptionPlans");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // BranchStatusType
            modelBuilder.Entity<BranchStatusType>(entity =>
            {
                entity.ToTable("Mappings_BranchStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // UserStatusType
            modelBuilder.Entity<UserStatusType>(entity =>
            {
                entity.ToTable("Mappings_UserStatusTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // TwoFactorMethod
            modelBuilder.Entity<TwoFactorMethod>(entity =>
            {
                entity.ToTable("Mappings_TwoFactorMethods");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // PermissionCategory
            modelBuilder.Entity<PermissionCategory>(entity =>
            {
                entity.ToTable("Mappings_PermissionCategories");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // RoleType
            modelBuilder.Entity<RoleType>(entity =>
            {
                entity.ToTable("Mappings_RoleTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // NotificationType
            modelBuilder.Entity<NotificationType>(entity =>
            {
                entity.ToTable("Mappings_NotificationTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // NotificationChannel
            modelBuilder.Entity<NotificationChannel>(entity =>
            {
                entity.ToTable("Mappings_NotificationChannels");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // ThemeType
            modelBuilder.Entity<ThemeType>(entity =>
            {
                entity.ToTable("Mappings_ThemeTypes");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            #endregion

            #region CDN Configuration

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
                    .OnDelete(DeleteBehavior.Cascade);

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
                    .OnDelete(DeleteBehavior.Cascade);

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
        }
    }
}