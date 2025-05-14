using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Roovia.Models.Helper;
using Roovia.Models.Users;
using Roovia.Models.CDN;

namespace Roovia.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<ContactNumber> ContactNumbers { get; set; }
        public DbSet<BranchLogo> BranchLogos { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; }
        public DbSet<CdnConfiguration> CdnConfigurations { get; set; }
        public DbSet<CdnCategory> CdnCategories { get; set; }
        public DbSet<CdnFolder> CdnFolders { get; set; }
        public DbSet<CdnFileMetadata> CdnFileMetadata { get; set; }
        public DbSet<CdnBase64Storage> CdnBase64Storage { get; set; }
        public DbSet<CdnUsageStatistic> CdnUsageStatistics { get; set; }
        public DbSet<CdnAccessLog> CdnAccessLogs { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Address as an owned entity for Company
            modelBuilder.Entity<Company>().OwnsOne(c => c.Address);

            // Configure Address as an owned entity for Branch
            modelBuilder.Entity<Branch>().OwnsOne(b => b.Address);

            // Configure BankAccount as an owned entity for Branch
            modelBuilder.Entity<Branch>().OwnsOne(b => b.BankDetails);

            // Configure Email entity
            modelBuilder.Entity<Email>(entity =>
            {
                // Set up identity configuration for Id column
                entity.Property(e => e.Id).UseIdentityColumn();

                // Configure unique indexes
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
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne<Company>()
                    .WithMany(c => c.EmailAddresses)
                    .HasForeignKey(e => e.CompanyId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Branch>()
                    .WithMany(b => b.EmailAddresses)
                    .HasForeignKey(e => e.BranchId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            // Configure ContactNumber entity
            modelBuilder.Entity<ContactNumber>(entity =>
            {
                // Set up identity configuration for Id column
                entity.Property(c => c.Id).UseIdentityColumn();

                // Configure unique indexes
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
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne<Company>()
                    .WithMany(c => c.ContactNumbers)
                    .HasForeignKey(c => c.CompanyId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Branch>()
                    .WithMany(b => b.ContactNumbers)
                    .HasForeignKey(c => c.BranchId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            // Configure BranchLogo relationships
            modelBuilder.Entity<BranchLogo>()
                .HasOne(b => b.Branch)
                .WithMany(b => b.Logos)
                .HasForeignKey(b => b.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Branch relationships
            modelBuilder.Entity<Branch>()
                .HasOne(b => b.Company)
                .WithMany(c => c.Branches)
                .HasForeignKey(b => b.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ApplicationUser relationships
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Branch)
                .WithMany(b => b.Users)
                .HasForeignKey(u => u.BranchId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);

            // Configure UserRoleAssignment relationships
            modelBuilder.Entity<UserRoleAssignment>()
                .HasOne<ApplicationUser>()
                .WithMany(u => u.CustomRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRoleAssignment>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure RolePermission relationships
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPermissionOverride>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(upo => upo.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPermissionOverride>()
                .HasOne(upo => upo.Permission)
                .WithMany()
                .HasForeignKey(upo => upo.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPermissionOverride>().ToTable("UserPermissionOverrides");

            // Define table names
            modelBuilder.Entity<Company>().ToTable("Companies");
            modelBuilder.Entity<Branch>().ToTable("Branches");
            modelBuilder.Entity<Email>().ToTable("Emails");
            modelBuilder.Entity<ContactNumber>().ToTable("ContactNumbers");
            modelBuilder.Entity<BranchLogo>().ToTable("BranchLogos");
            modelBuilder.Entity<Media>().ToTable("Media");
            modelBuilder.Entity<Permission>().ToTable("Permissions");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<RolePermission>().ToTable("RolePermissions");
            modelBuilder.Entity<UserRoleAssignment>().ToTable("UserRoleAssignments");

            // Configure CDN entities

            // CdnConfiguration
            modelBuilder.Entity<CdnConfiguration>(entity =>
            {
                entity.ToTable("CdnConfigurations");
                entity.Property(e => e.BaseUrl).IsRequired().HasMaxLength(255);
                entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AllowedFileTypes).IsRequired().HasMaxLength(500);
            });

            // CdnCategory
            modelBuilder.Entity<CdnCategory>(entity =>
            {
                entity.ToTable("CdnCategories");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // CdnFolder
            modelBuilder.Entity<CdnFolder>(entity =>
            {
                entity.ToTable("CdnFolders");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Path).IsRequired().HasMaxLength(500);

                entity.HasOne(f => f.Parent)
                    .WithMany()
                    .HasForeignKey(f => f.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Category)
                    .WithMany()
                    .HasForeignKey(f => f.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.CategoryId, e.Path }).IsUnique();
            });

            // CdnFileMetadata
            modelBuilder.Entity<CdnFileMetadata>(entity =>
            {
                entity.ToTable("CdnFileMetadata");
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
                entity.ToTable("CdnBase64Storage");
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
                entity.ToTable("CdnUsageStatistics");

                entity.HasOne(s => s.Category)
                    .WithMany()
                    .HasForeignKey(s => s.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.Date, e.CategoryId });
            });

            // CdnAccessLog
            modelBuilder.Entity<CdnAccessLog>(entity =>
            {
                entity.ToTable("CdnAccessLogs");
                entity.Property(e => e.ActionType).IsRequired().HasMaxLength(20);

                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ActionType);
            });
        }
    }
}