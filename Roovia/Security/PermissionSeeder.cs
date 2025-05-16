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
            // Define permissions by category - using your provided permissions and adding missing ones
            var permissions = new List<Permission>
            {
                // Properties
                new Permission { Name = "Create/Update Properties", Description = "Create or update properties", Category = "Properties", SystemName = "properties.create", CreatedBy = "System" },
                new Permission { Name = "Archive/Restore Properties", Description = "Archive or restore properties", Category = "Properties", SystemName = "properties.archive", CreatedBy = "System" },
                new Permission { Name = "Access All Properties", Description = "Access all properties", Category = "Properties", SystemName = "properties.view.all", CreatedBy = "System" },
                new Permission { Name = "Access Budgets", Description = "Access property budgets", Category = "Properties", SystemName = "properties.budgets", CreatedBy = "System" },
                new Permission { Name = "Manage Property Types", Description = "Manage property types and categories", Category = "Properties", SystemName = "properties.types", CreatedBy = "System" },
                new Permission { Name = "View Property Documents", Description = "View property-related documents", Category = "Properties", SystemName = "properties.documents.view", CreatedBy = "System" },
                new Permission { Name = "Manage Property Status", Description = "Change property status", Category = "Properties", SystemName = "properties.status", CreatedBy = "System" },
                new Permission { Name = "Upload Property Images", Description = "Upload images for properties", Category = "Properties", SystemName = "properties.images", CreatedBy = "System" },

                // Beneficiaries
                new Permission { Name = "Create Beneficiaries", Description = "Create beneficiaries", Category = "Beneficiaries", SystemName = "beneficiaries.create", CreatedBy = "System" },
                new Permission { Name = "Update Beneficiaries", Description = "Update beneficiaries (excluding bank details)", Category = "Beneficiaries", SystemName = "beneficiaries.update", CreatedBy = "System" },
                new Permission { Name = "Update Bank Details", Description = "Update beneficiary bank details", Category = "Beneficiaries", SystemName = "beneficiaries.update.bank", CreatedBy = "System" },
                new Permission { Name = "Archive/Restore Beneficiaries", Description = "Archive or restore beneficiaries", Category = "Beneficiaries", SystemName = "beneficiaries.archive", CreatedBy = "System" },
                new Permission { Name = "Manage Payments", Description = "Create or update beneficiary payments", Category = "Beneficiaries", SystemName = "beneficiaries.payments", CreatedBy = "System" },
                new Permission { Name = "Approve Details", Description = "Approve beneficiary details", Category = "Beneficiaries", SystemName = "beneficiaries.approve", CreatedBy = "System" },
                new Permission { Name = "View Payment History", Description = "View beneficiary payment history", Category = "Beneficiaries", SystemName = "beneficiaries.payments.history", CreatedBy = "System" },
                new Permission { Name = "Calculate Amounts", Description = "Calculate beneficiary payment amounts", Category = "Beneficiaries", SystemName = "beneficiaries.calculate", CreatedBy = "System" },

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
                new Permission { Name = "Update Lease Details", Description = "Update lease dates and terms", Category = "Tenants", SystemName = "tenants.lease.update", CreatedBy = "System" },
                new Permission { Name = "Manage Tenant Types", Description = "Manage tenant types and categories", Category = "Tenants", SystemName = "tenants.types", CreatedBy = "System" },

                // Reports
                new Permission { Name = "View Commission Reports", Description = "View commission reports", Category = "Reports", SystemName = "reports.commission", CreatedBy = "System" },
                new Permission { Name = "View Transaction History", Description = "View transaction history reports", Category = "Reports", SystemName = "reports.transactions", CreatedBy = "System" },
                new Permission { Name = "View Payment Confirmations", Description = "View payment confirmations", Category = "Reports", SystemName = "reports.payments", CreatedBy = "System" },
                new Permission { Name = "Download Bulk Statements", Description = "Download bulk statements", Category = "Reports", SystemName = "reports.statements", CreatedBy = "System" },
                new Permission { Name = "View Audit Log", Description = "View audit log", Category = "Reports", SystemName = "reports.audit", CreatedBy = "System" },
                new Permission { Name = "View Commission QuickStats", Description = "View commission quickstats", Category = "Reports", SystemName = "reports.quickstats", CreatedBy = "System" },
                new Permission { Name = "View Contact Details", Description = "View contact details", Category = "Reports", SystemName = "reports.contacts", CreatedBy = "System" },
                new Permission { Name = "View Management Reports", Description = "View management reports", Category = "Reports", SystemName = "reports.management", CreatedBy = "System" },
                new Permission { Name = "Create Custom Reports", Description = "Create and save custom reports", Category = "Reports", SystemName = "reports.custom.create", CreatedBy = "System" },
                new Permission { Name = "Schedule Reports", Description = "Schedule automated reports", Category = "Reports", SystemName = "reports.schedule", CreatedBy = "System" },
                new Permission { Name = "Create Dashboard", Description = "Create and customize dashboards", Category = "Reports", SystemName = "reports.dashboard.create", CreatedBy = "System" },
                new Permission { Name = "View Dashboards", Description = "View report dashboards", Category = "Reports", SystemName = "reports.dashboard.view", CreatedBy = "System" },

                // Payments (renamed from "Bank Statements & Payments")
                new Permission { Name = "Reject Beneficiary Payments", Description = "Reject beneficiary payments", Category = "Payments", SystemName = "payments.reject", CreatedBy = "System" },
                new Permission { Name = "View Direct Deposits", Description = "View direct deposits", Category = "Payments", SystemName = "payments.deposits.view", CreatedBy = "System" },
                new Permission { Name = "Reconcile Direct Deposits", Description = "Reconcile direct deposits", Category = "Payments", SystemName = "payments.deposits.reconcile", CreatedBy = "System" },
                new Permission { Name = "Request Early Clearance", Description = "Request early cheque clearance", Category = "Payments", SystemName = "payments.clearance.request", CreatedBy = "System" },
                new Permission { Name = "Approve Payments", Description = "Approve beneficiary payments", Category = "Payments", SystemName = "payments.approve", CreatedBy = "System" },
                new Permission { Name = "Send Payment Proofs", Description = "Send proof of payment requests", Category = "Payments", SystemName = "payments.proofs", CreatedBy = "System" },
                new Permission { Name = "Create Property Payments", Description = "Create property payments", Category = "Payments", SystemName = "payments.property.create", CreatedBy = "System" },
                new Permission { Name = "Upload Payment Receipts", Description = "Upload payment receipts/proofs", Category = "Payments", SystemName = "payments.receipts.upload", CreatedBy = "System" },
                new Permission { Name = "Allocate Payments", Description = "Allocate payments to beneficiaries", Category = "Payments", SystemName = "payments.allocate", CreatedBy = "System" },
                new Permission { Name = "Manage Payment Schedules", Description = "Create and manage payment schedules", Category = "Payments", SystemName = "payments.schedules", CreatedBy = "System" },
                new Permission { Name = "Create Payment Rules", Description = "Create and manage payment rules", Category = "Payments", SystemName = "payments.rules", CreatedBy = "System" },

                // Maintenance (new category)
                new Permission { Name = "Create Maintenance Tickets", Description = "Create maintenance tickets", Category = "Maintenance", SystemName = "maintenance.tickets.create", CreatedBy = "System" },
                new Permission { Name = "Update Maintenance Tickets", Description = "Update maintenance tickets", Category = "Maintenance", SystemName = "maintenance.tickets.update", CreatedBy = "System" },
                new Permission { Name = "Assign Maintenance", Description = "Assign maintenance to vendors", Category = "Maintenance", SystemName = "maintenance.assign", CreatedBy = "System" },
                new Permission { Name = "Complete Maintenance", Description = "Mark maintenance as complete", Category = "Maintenance", SystemName = "maintenance.complete", CreatedBy = "System" },
                new Permission { Name = "Add Maintenance Comments", Description = "Add comments to maintenance tickets", Category = "Maintenance", SystemName = "maintenance.comments", CreatedBy = "System" },
                new Permission { Name = "Add Maintenance Expenses", Description = "Add expenses to maintenance tickets", Category = "Maintenance", SystemName = "maintenance.expenses", CreatedBy = "System" },
                new Permission { Name = "Approve Maintenance Expenses", Description = "Approve maintenance expenses", Category = "Maintenance", SystemName = "maintenance.expenses.approve", CreatedBy = "System" },
                new Permission { Name = "Upload Maintenance Documents", Description = "Upload maintenance-related documents", Category = "Maintenance", SystemName = "maintenance.documents", CreatedBy = "System" },
                new Permission { Name = "View All Maintenance", Description = "View all maintenance tickets", Category = "Maintenance", SystemName = "maintenance.view.all", CreatedBy = "System" },
                new Permission { Name = "View Maintenance Statistics", Description = "View maintenance statistics and reports", Category = "Maintenance", SystemName = "maintenance.statistics", CreatedBy = "System" },

                // Inspections (new category)
                new Permission { Name = "Create Inspections", Description = "Create property inspections", Category = "Inspections", SystemName = "inspections.create", CreatedBy = "System" },
                new Permission { Name = "Update Inspections", Description = "Update property inspections", Category = "Inspections", SystemName = "inspections.update", CreatedBy = "System" },
                new Permission { Name = "Complete Inspections", Description = "Mark inspections as complete", Category = "Inspections", SystemName = "inspections.complete", CreatedBy = "System" },
                new Permission { Name = "Generate Inspection Reports", Description = "Generate inspection reports", Category = "Inspections", SystemName = "inspections.reports", CreatedBy = "System" },
                new Permission { Name = "Add Inspection Items", Description = "Add items to inspections", Category = "Inspections", SystemName = "inspections.items", CreatedBy = "System" },
                new Permission { Name = "Upload Inspection Images", Description = "Upload inspection images", Category = "Inspections", SystemName = "inspections.images", CreatedBy = "System" },
                new Permission { Name = "Schedule Recurring Inspections", Description = "Schedule recurring inspections", Category = "Inspections", SystemName = "inspections.recurring", CreatedBy = "System" },
                new Permission { Name = "View Inspection Statistics", Description = "View inspection statistics", Category = "Inspections", SystemName = "inspections.statistics", CreatedBy = "System" },
                new Permission { Name = "View All Inspections", Description = "View all property inspections", Category = "Inspections", SystemName = "inspections.view.all", CreatedBy = "System" },

                // Documents (new category)
                new Permission { Name = "Upload Documents", Description = "Upload documents to the system", Category = "Documents", SystemName = "documents.upload", CreatedBy = "System" },
                new Permission { Name = "View Documents", Description = "View uploaded documents", Category = "Documents", SystemName = "documents.view", CreatedBy = "System" },
                new Permission { Name = "Approve Documents", Description = "Approve submitted documents", Category = "Documents", SystemName = "documents.approve", CreatedBy = "System" },
                new Permission { Name = "Reject Documents", Description = "Reject submitted documents", Category = "Documents", SystemName = "documents.reject", CreatedBy = "System" },
                new Permission { Name = "Update Document Status", Description = "Update document status", Category = "Documents", SystemName = "documents.status", CreatedBy = "System" },
                new Permission { Name = "Manage Document Types", Description = "Manage document types and categories", Category = "Documents", SystemName = "documents.types", CreatedBy = "System" },
                new Permission { Name = "Set Document Requirements", Description = "Set document requirements for entities", Category = "Documents", SystemName = "documents.requirements", CreatedBy = "System" },
                new Permission { Name = "Delete Documents", Description = "Delete uploaded documents", Category = "Documents", SystemName = "documents.delete", CreatedBy = "System" },
                new Permission { Name = "Export Documents", Description = "Export documents from the system", Category = "Documents", SystemName = "documents.export", CreatedBy = "System" },

                // Communications (new category)
                new Permission { Name = "Send Emails", Description = "Send email communications", Category = "Communications", SystemName = "communications.email", CreatedBy = "System" },
                new Permission { Name = "Send SMS", Description = "Send SMS communications", Category = "Communications", SystemName = "communications.sms", CreatedBy = "System" },
                new Permission { Name = "Send Bulk Communications", Description = "Send bulk communications", Category = "Communications", SystemName = "communications.bulk", CreatedBy = "System" },
                new Permission { Name = "Create Communication Templates", Description = "Create communication templates", Category = "Communications", SystemName = "communications.templates", CreatedBy = "System" },
                new Permission { Name = "View Communication History", Description = "View communication history", Category = "Communications", SystemName = "communications.history", CreatedBy = "System" },
                new Permission { Name = "Import Communications", Description = "Import communications from external sources", Category = "Communications", SystemName = "communications.import", CreatedBy = "System" },
                new Permission { Name = "Export Communications", Description = "Export communication records", Category = "Communications", SystemName = "communications.export", CreatedBy = "System" },
                new Permission { Name = "Send Templated Email", Description = "Send emails using templates", Category = "Communications", SystemName = "communications.email.templated", CreatedBy = "System" },
                new Permission { Name = "View Communication Statistics", Description = "View communication statistics", Category = "Communications", SystemName = "communications.statistics", CreatedBy = "System" },

                // Owners (new category)
                new Permission { Name = "Create Property Owners", Description = "Create property owners", Category = "Owners", SystemName = "owners.create", CreatedBy = "System" },
                new Permission { Name = "Update Property Owners", Description = "Update property owner details", Category = "Owners", SystemName = "owners.update", CreatedBy = "System" },
                new Permission { Name = "Delete Property Owners", Description = "Delete/archive property owners", Category = "Owners", SystemName = "owners.delete", CreatedBy = "System" },
                new Permission { Name = "Manage Owner Contacts", Description = "Manage owner contact information", Category = "Owners", SystemName = "owners.contacts", CreatedBy = "System" },
                new Permission { Name = "Update Owner Addresses", Description = "Update owner addresses", Category = "Owners", SystemName = "owners.address", CreatedBy = "System" },
                new Permission { Name = "Update Owner Bank Accounts", Description = "Update owner bank account details", Category = "Owners", SystemName = "owners.bank", CreatedBy = "System" },
                new Permission { Name = "View Owner Properties", Description = "View properties associated with owners", Category = "Owners", SystemName = "owners.properties", CreatedBy = "System" },
                new Permission { Name = "Export Owners", Description = "Export owner data", Category = "Owners", SystemName = "owners.export", CreatedBy = "System" },
                new Permission { Name = "Search Owners", Description = "Search for property owners", Category = "Owners", SystemName = "owners.search", CreatedBy = "System" },

                // Vendors (new category)
                new Permission { Name = "Create Vendors", Description = "Create vendors in the system", Category = "Vendors", SystemName = "vendors.create", CreatedBy = "System" },
                new Permission { Name = "Update Vendors", Description = "Update vendor information", Category = "Vendors", SystemName = "vendors.update", CreatedBy = "System" },
                new Permission { Name = "Delete Vendors", Description = "Delete/archive vendors", Category = "Vendors", SystemName = "vendors.delete", CreatedBy = "System" },
                new Permission { Name = "Manage Vendor Contacts", Description = "Manage vendor contact information", Category = "Vendors", SystemName = "vendors.contacts", CreatedBy = "System" },
                new Permission { Name = "Rate Vendors", Description = "Rate vendor performance", Category = "Vendors", SystemName = "vendors.rate", CreatedBy = "System" },
                new Permission { Name = "Set Preferred Vendors", Description = "Set preferred vendor status", Category = "Vendors", SystemName = "vendors.preferred", CreatedBy = "System" },
                new Permission { Name = "Upload Vendor Documents", Description = "Upload vendor-related documents", Category = "Vendors", SystemName = "vendors.documents", CreatedBy = "System" },
                new Permission { Name = "View Vendor Performance", Description = "View vendor performance statistics", Category = "Vendors", SystemName = "vendors.performance", CreatedBy = "System" },
                new Permission { Name = "View All Vendors", Description = "View all vendors in the system", Category = "Vendors", SystemName = "vendors.view.all", CreatedBy = "System" },
                new Permission { Name = "Filter Vendors by Specialization", Description = "Search vendors by specialization", Category = "Vendors", SystemName = "vendors.filter", CreatedBy = "System" },

                // System Settings
                new Permission { Name = "Manage Users", Description = "Create or update users", Category = "Settings", SystemName = "settings.users", CreatedBy = "System" },
                new Permission { Name = "Update Profile", Description = "Update profile settings", Category = "Settings", SystemName = "settings.profile", CreatedBy = "System" },
                new Permission { Name = "Import Data", Description = "Import data", Category = "Settings", SystemName = "settings.import", CreatedBy = "System" },
                new Permission { Name = "Export Data", Description = "Export data", Category = "Settings", SystemName = "settings.export", CreatedBy = "System" },
                new Permission { Name = "Manage Application Settings", Description = "Manage application settings", Category = "Settings", SystemName = "settings.application", CreatedBy = "System" },
                new Permission { Name = "Upload Documents", Description = "Upload documents", Category = "Settings", SystemName = "settings.documents", CreatedBy = "System" },
                new Permission { Name = "Manage Roles & Permissions", Description = "Manage roles and permissions", Category = "Settings", SystemName = "settings.permissions", CreatedBy = "System" },
                new Permission { Name = "Manage System Lookups", Description = "Manage system lookup tables", Category = "Settings", SystemName = "settings.lookups", CreatedBy = "System" },
                new Permission { Name = "Manage Companies", Description = "Create and manage companies", Category = "Settings", SystemName = "settings.companies", CreatedBy = "System" },
                new Permission { Name = "Manage Branches", Description = "Create and manage branches", Category = "Settings", SystemName = "settings.branches", CreatedBy = "System" },
                new Permission { Name = "Configure CDN", Description = "Configure content delivery network settings", Category = "Settings", SystemName = "settings.cdn", CreatedBy = "System" },
                new Permission { Name = "View System Logs", Description = "View system logs and diagnostics", Category = "Settings", SystemName = "settings.logs", CreatedBy = "System" },
                new Permission { Name = "Manage Notification Settings", Description = "Configure notification settings", Category = "Settings", SystemName = "settings.notifications", CreatedBy = "System" }
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
                           p.Category == "Tenants" ||
                           p.Category == "Inspections" ||
                           p.Category == "Maintenance" ||
                           p.Category == "Documents" ||
                           p.SystemName == "vendors.view.all" ||
                           p.SystemName == "vendors.filter" ||
                           p.SystemName == "owners.properties" ||
                           p.SystemName == "owners.search" ||
                           p.SystemName == "communications.email" ||
                           p.SystemName == "communications.sms" ||
                           p.SystemName == "communications.history" ||
                           p.SystemName == "settings.profile")
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
                // Beneficiary related
                "beneficiaries.payments", "beneficiaries.approve", "beneficiaries.update.bank",
                "beneficiaries.payments.history", "beneficiaries.calculate",
                
                // Payments related
                "payments.reject", "payments.deposits.view", "payments.deposits.reconcile",
                "payments.approve", "payments.proofs", "payments.clearance.request",
                "payments.property.create", "payments.receipts.upload", "payments.allocate",
                "payments.schedules", "payments.rules",
                
                // Reports related
                "reports.commission", "reports.transactions", "reports.payments",
                "reports.statements", "reports.quickstats", "reports.management",
                
                // Documents related
                "documents.upload", "documents.view", "documents.approve", 
                
                // Other essentials
                "settings.profile", "settings.export",
                "communications.email", "communications.history"
            };

            var financialPermissions = permissions
                .Where(p => financialPermissionNames.Contains(p.SystemName) ||
                           p.Category == "Payments")
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
                // Tenant related
                "tenants.create", "tenants.archive", "tenants.invoices", "tenants.notes",
                "tenants.deposit.request", "tenants.tpn.view", "tenants.credit.check",
                "tenants.reminders", "tenants.arrears.view", "tenants.lease.update", 
                
                // Properties limited view
                "properties.view.all", 
                
                // Communications
                "communications.email", "communications.sms", "communications.history",
                
                // Reports & Documents
                "reports.contacts", "documents.upload", "documents.view",
                
                // Profile management
                "settings.profile"
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
                // Reports
                "reports.commission", "reports.transactions", "reports.payments",
                "reports.statements", "reports.quickstats", "reports.contacts",
                "reports.management", "reports.dashboard.view",
                
                // View-only permissions
                "properties.view.all", "tenants.arrears.view", "documents.view",
                "communications.history", "maintenance.statistics", "inspections.statistics",
                "owners.properties", "vendors.performance",
                
                // Profile
                "settings.profile"
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
                "settings.permissions", "settings.application", "settings.lookups",
                "settings.companies", "settings.cdn", "settings.logs",
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
                "settings.permissions", "settings.application", "settings.lookups",
                "settings.cdn", "settings.logs"
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

            // Find all SystemAdministrator users
            var admins = await dbContext.Users
                .Where(u => u.Role == SystemRole.SystemAdministrator)
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