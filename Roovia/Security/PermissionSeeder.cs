using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Security
{
    public static class PermissionSeeder
    {
        public static async Task SeedPermissionsAndRoles(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure database is created
            await dbContext.Database.EnsureCreatedAsync();

            // Seed permissions if none exist
            if (!await dbContext.Permissions.AnyAsync())
            {
                await SeedPermissions(dbContext);
                await SeedRoles(dbContext);
                await AssignPermissionsToRoles(dbContext);

                // Assign Admin role to existing GlobalAdmin users
                await AssignRolesToAdmins(dbContext, userManager);
            }
        }

        private static async Task SeedPermissions(ApplicationDbContext dbContext)
        {
            // Define permission categories - use your provided categories
            var permissionCategories = new[]
            {
                "Properties", "Beneficiaries", "Tenants", "Reports",
                "Bank Statements & Payments", "System Settings"
            };

            // Define permissions by category - using your provided permissions
            var permissions = new List<Permission>
            {
                // Properties
                new Permission { Name = "Create/Update Properties", Description = "Create or update properties", Category = "Properties", SystemName = "properties.create", CreatedBy = "System" },
                new Permission { Name = "Archive/Restore Properties", Description = "Archive or restore properties", Category = "Properties", SystemName = "properties.archive", CreatedBy = "System" },
                new Permission { Name = "Access All Properties", Description = "Access all properties", Category = "Properties", SystemName = "properties.view.all", CreatedBy = "System" },
                new Permission { Name = "Access Budgets", Description = "Access property budgets", Category = "Properties", SystemName = "properties.budgets", CreatedBy = "System" },
                
                // Beneficiaries
                new Permission { Name = "Create Beneficiaries", Description = "Create beneficiaries", Category = "Beneficiaries", SystemName = "beneficiaries.create", CreatedBy = "System" },
                new Permission { Name = "Update Beneficiaries", Description = "Update beneficiaries (excluding bank details)", Category = "Beneficiaries", SystemName = "beneficiaries.update", CreatedBy = "System" },
                new Permission { Name = "Update Bank Details", Description = "Update beneficiary bank details", Category = "Beneficiaries", SystemName = "beneficiaries.update.bank", CreatedBy = "System" },
                new Permission { Name = "Archive/Restore Beneficiaries", Description = "Archive or restore beneficiaries", Category = "Beneficiaries", SystemName = "beneficiaries.archive", CreatedBy = "System" },
                new Permission { Name = "Manage Payments", Description = "Create or update beneficiary payments", Category = "Beneficiaries", SystemName = "beneficiaries.payments", CreatedBy = "System" },
                new Permission { Name = "Approve Details", Description = "Approve beneficiary details", Category = "Beneficiaries", SystemName = "beneficiaries.approve", CreatedBy = "System" },
                
                // Tenants
                new Permission { Name = "Create/Update Tenants", Description = "Create or update tenants", Category = "Tenants", SystemName = "tenants.create", CreatedBy = "System" },
                new Permission { Name = "Archive/Restore Tenants", Description = "Archive or restore tenants", Category = "Tenants", SystemName = "tenants.archive", CreatedBy = "System" },
                new Permission { Name = "Change Bank Details", Description = "Change tenant's bank details", Category = "Tenants", SystemName = "tenants.update.bank", CreatedBy = "System" },
                new Permission { Name = "Manage Invoices", Description = "Create or update tenant invoices", Category = "Tenants", SystemName = "tenants.invoices", CreatedBy = "System" },
                new Permission { Name = "Create Notes", Description = "Create credit/debit notes", Category = "Tenants", SystemName = "tenants.notes", CreatedBy = "System" },
                new Permission { Name = "Request Deposit Release", Description = "Request damage deposit release", Category = "Tenants", SystemName = "tenants.deposit.request", CreatedBy = "System" },
                new Permission { Name = "Approve Deposit Release", Description = "Approve damage deposit release", Category = "Tenants", SystemName = "tenants.deposit.approve", CreatedBy = "System" },
                new Permission { Name = "View TPN Reports", Description = "View tenant TPN reports", Category = "Tenants", SystemName = "tenants.tpn.view", CreatedBy = "System" },
                new Permission { Name = "Do Credit Checks", Description = "Perform credit checks", Category = "Tenants", SystemName = "tenants.credit.check", CreatedBy = "System" },
                new Permission { Name = "Export TPN Data", Description = "Download TPN data exports", Category = "Tenants", SystemName = "tenants.tpn.export", CreatedBy = "System" },
                new Permission { Name = "Send Payment Reminders", Description = "Send payment reminders", Category = "Tenants", SystemName = "tenants.reminders", CreatedBy = "System" },
                new Permission { Name = "Send Demand Letters", Description = "Send letters of demand", Category = "Tenants", SystemName = "tenants.demand.letters", CreatedBy = "System" },
                new Permission { Name = "View Arrears", Description = "View tenants in arrears", Category = "Tenants", SystemName = "tenants.arrears.view", CreatedBy = "System" },
                
                // Reports
                new Permission { Name = "View Commission Reports", Description = "View commission reports", Category = "Reports", SystemName = "reports.commission", CreatedBy = "System" },
                new Permission { Name = "View Transaction History", Description = "View transaction history reports", Category = "Reports", SystemName = "reports.transactions", CreatedBy = "System" },
                new Permission { Name = "View Payment Confirmations", Description = "View payment confirmations", Category = "Reports", SystemName = "reports.payments", CreatedBy = "System" },
                new Permission { Name = "Download Bulk Statements", Description = "Download bulk statements", Category = "Reports", SystemName = "reports.statements", CreatedBy = "System" },
                new Permission { Name = "View Audit Log", Description = "View audit log", Category = "Reports", SystemName = "reports.audit", CreatedBy = "System" },
                new Permission { Name = "View Commission QuickStats", Description = "View commission quickstats", Category = "Reports", SystemName = "reports.quickstats", CreatedBy = "System" },
                new Permission { Name = "View Contact Details", Description = "View contact details", Category = "Reports", SystemName = "reports.contacts", CreatedBy = "System" },
                new Permission { Name = "View Management Reports", Description = "View management reports", Category = "Reports", SystemName = "reports.management", CreatedBy = "System" },
                
                // Bank Statements & Payments
                new Permission { Name = "Reject Beneficiary Payments", Description = "Reject beneficiary payments", Category = "Bank Statements & Payments", SystemName = "payments.reject", CreatedBy = "System" },
                new Permission { Name = "View Direct Deposits", Description = "View direct deposits", Category = "Bank Statements & Payments", SystemName = "payments.deposits.view", CreatedBy = "System" },
                new Permission { Name = "Reconcile Direct Deposits", Description = "Reconcile direct deposits", Category = "Bank Statements & Payments", SystemName = "payments.deposits.reconcile", CreatedBy = "System" },
                new Permission { Name = "Request Early Clearance", Description = "Request early cheque clearance", Category = "Bank Statements & Payments", SystemName = "payments.clearance.request", CreatedBy = "System" },
                new Permission { Name = "Approve Payments", Description = "Approve beneficiary payments", Category = "Bank Statements & Payments", SystemName = "payments.approve", CreatedBy = "System" },
                new Permission { Name = "Send Payment Proofs", Description = "Send proof of payment requests", Category = "Bank Statements & Payments", SystemName = "payments.proofs", CreatedBy = "System" },
                
                // System Settings
                new Permission { Name = "Manage Users", Description = "Create or update users", Category = "System Settings", SystemName = "settings.users", CreatedBy = "System" },
                new Permission { Name = "Update Profile", Description = "Update profile settings", Category = "System Settings", SystemName = "settings.profile", CreatedBy = "System" },
                new Permission { Name = "Import Data", Description = "Import data", Category = "System Settings", SystemName = "settings.import", CreatedBy = "System" },
                new Permission { Name = "Export Data", Description = "Export data", Category = "System Settings", SystemName = "settings.export", CreatedBy = "System" },
                new Permission { Name = "Manage Application Settings", Description = "Manage application settings", Category = "System Settings", SystemName = "settings.application", CreatedBy = "System" },
                new Permission { Name = "Upload Documents", Description = "Upload documents", Category = "System Settings", SystemName = "settings.documents", CreatedBy = "System" },
                new Permission { Name = "Manage Roles & Permissions", Description = "Manage roles and permissions", Category = "System Settings", SystemName = "settings.permissions", CreatedBy = "System" }
            };

            await dbContext.Permissions.AddRangeAsync(permissions);
            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedRoles(ApplicationDbContext dbContext)
        {
            // Define preset roles - more carefully aligned with your business needs
            var roles = new List<Role>
            {
                new Role
                {
                    Name = "System Administrator",
                    Description = "Full system access with all permissions",
                    IsPreset = true,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "Property Manager",
                    Description = "Manages properties, tenants, and related operations",
                    IsPreset = true,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "Financial Officer",
                    Description = "Manages financial operations, payments and reports",
                    IsPreset = true,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "Tenant Officer",
                    Description = "Manages tenant relationships and operations",
                    IsPreset = true,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "Reports Viewer",
                    Description = "View-only access to reports and dashboards",
                    IsPreset = true,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "Branch Manager",
                    Description = "Manages operations for a specific branch",
                    IsPreset = true,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "Company Administrator",
                    Description = "Administrates all branches within a company",
                    IsPreset = true,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System"
                }
            };

            await dbContext.Roles.AddRangeAsync(roles);
            await dbContext.SaveChangesAsync();
        }

        private static async Task AssignPermissionsToRoles(ApplicationDbContext dbContext)
        {
            var roles = await dbContext.Roles.ToListAsync();
            var permissions = await dbContext.Permissions.ToListAsync();

            // Get role IDs
            var adminRole = roles.First(r => r.Name == "System Administrator");
            var propertyManagerRole = roles.First(r => r.Name == "Property Manager");
            var financialRole = roles.First(r => r.Name == "Financial Officer");
            var tenantRole = roles.First(r => r.Name == "Tenant Officer");
            var reportsRole = roles.First(r => r.Name == "Reports Viewer");
            var branchManagerRole = roles.First(r => r.Name == "Branch Manager");
            var companyAdminRole = roles.First(r => r.Name == "Company Administrator");

            // System Administrator - all permissions
            var adminPermissions = permissions.Select(p => new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = p.Id,
                IsActive = true,
                CreatedBy = "System"
            }).ToList();

            await dbContext.RolePermissions.AddRangeAsync(adminPermissions);

            // Property Manager
            var propertyManagerPermissions = permissions
                .Where(p => p.Category == "Properties" ||
                           (p.Category == "System Settings" && p.SystemName == "settings.profile") ||
                           p.SystemName == "settings.documents")
                .Select(p => new RolePermission
                {
                    RoleId = propertyManagerRole.Id,
                    PermissionId = p.Id,
                    IsActive = true,
                    CreatedBy = "System"
                }).ToList();

            await dbContext.RolePermissions.AddRangeAsync(propertyManagerPermissions);

            // Financial Officer
            var financialPermissionNames = new HashSet<string>
            {
                "beneficiaries.payments", "beneficiaries.approve", "beneficiaries.update.bank",
                "payments.reject", "payments.deposits.view", "payments.deposits.reconcile",
                "payments.approve", "payments.proofs", "payments.clearance.request",
                "reports.commission", "reports.transactions", "reports.payments",
                "reports.statements", "reports.quickstats", "reports.management",
                "settings.profile", "settings.documents", "settings.export"
            };

            var financialPermissions = permissions
                .Where(p => financialPermissionNames.Contains(p.SystemName))
                .Select(p => new RolePermission
                {
                    RoleId = financialRole.Id,
                    PermissionId = p.Id,
                    IsActive = true,
                    CreatedBy = "System"
                }).ToList();

            await dbContext.RolePermissions.AddRangeAsync(financialPermissions);

            // Tenant Officer
            var tenantPermissionNames = new HashSet<string>
            {
                "tenants.create", "tenants.archive", "tenants.invoices", "tenants.notes",
                "tenants.deposit.request", "tenants.tpn.view", "tenants.credit.check",
                "tenants.reminders", "tenants.arrears.view", "reports.contacts",
                "settings.profile", "settings.documents"
            };

            var tenantPermissions = permissions
                .Where(p => tenantPermissionNames.Contains(p.SystemName))
                .Select(p => new RolePermission
                {
                    RoleId = tenantRole.Id,
                    PermissionId = p.Id,
                    IsActive = true,
                    CreatedBy = "System"
                }).ToList();

            await dbContext.RolePermissions.AddRangeAsync(tenantPermissions);

            // Reports Viewer
            var reportPermissionNames = new HashSet<string>
            {
                "reports.commission", "reports.transactions", "reports.payments",
                "reports.statements", "reports.quickstats", "reports.contacts",
                "reports.management", "settings.profile", "properties.view.all",
                "tenants.arrears.view"
            };

            var reportPermissions = permissions
                .Where(p => reportPermissionNames.Contains(p.SystemName))
                .Select(p => new RolePermission
                {
                    RoleId = reportsRole.Id,
                    PermissionId = p.Id,
                    IsActive = true,
                    CreatedBy = "System"
                }).ToList();

            await dbContext.RolePermissions.AddRangeAsync(reportPermissions);

            // Branch Manager - most permissions except some system settings
            var branchManagerExclusions = new HashSet<string>
            {
                "settings.permissions", "settings.application", "settings.import",
                "beneficiaries.approve", "tenants.deposit.approve", "payments.approve"
            };

            var branchManagerPermissions = permissions
                .Where(p => !branchManagerExclusions.Contains(p.SystemName))
                .Select(p => new RolePermission
                {
                    RoleId = branchManagerRole.Id,
                    PermissionId = p.Id,
                    IsActive = true,
                    CreatedBy = "System"
                }).ToList();

            await dbContext.RolePermissions.AddRangeAsync(branchManagerPermissions);

            // Company Administrator - all permissions except system-wide settings
            var companyAdminExclusions = new HashSet<string>
            {
                "settings.permissions", "settings.application"
            };

            var companyAdminPermissions = permissions
                .Where(p => !companyAdminExclusions.Contains(p.SystemName))
                .Select(p => new RolePermission
                {
                    RoleId = companyAdminRole.Id,
                    PermissionId = p.Id,
                    IsActive = true,
                    CreatedBy = "System"
                }).ToList();

            await dbContext.RolePermissions.AddRangeAsync(companyAdminPermissions);

            await dbContext.SaveChangesAsync();
        }

        private static async Task AssignRolesToAdmins(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            // Get admin role
            var adminRole = await dbContext.Roles.FirstAsync(r => r.Name == "System Administrator");

            // Find all GlobalAdmin users
            var admins = await dbContext.Users
                .Where(u => u.Role == 0)
                .ToListAsync();

            foreach (var admin in admins)
            {
                // Check if user already has the role
                var hasRole = await dbContext.UserRoleAssignments
                    .AnyAsync(ur => ur.UserId == admin.Id && ur.RoleId == adminRole.Id);

                if (!hasRole)
                {
                    // Assign admin role
                    var userRole = new UserRoleAssignment
                    {
                        UserId = admin.Id,
                        RoleId = adminRole.Id,
                        AssignedDate = DateTime.Now,
                        AssignedBy = "System"
                    };

                    await dbContext.UserRoleAssignments.AddAsync(userRole);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}