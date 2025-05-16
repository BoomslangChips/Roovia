using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Models.BusinessMappingModels;
using Roovia.Models.ProjectCdnConfigModels;
using Roovia.Models.UserCompanyMappingModels;

namespace Roovia.Services.General
{
    public interface ISeedDataService
    {
        Task InitializeAsync();

        Task SeedBusinessMappingDataAsync();

        Task SeedUserCompanyMappingDataAsync();

        Task SeedCdnConfigurationAsync();
    }

    public class SeedDataService : ISeedDataService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SeedDataService> _logger;

        public SeedDataService(
            IServiceProvider serviceProvider,
            ILogger<SeedDataService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Apply any pending migrations
                await context.Database.MigrateAsync();

                // Seed data in the correct order
                await SeedUserCompanyMappingDataAsync();
                await SeedBusinessMappingDataAsync();
                await SeedCdnConfigurationAsync();

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        #region Business Mapping Data

        public async Task SeedBusinessMappingDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if data already exists
            if (await context.PropertyStatusTypes.AnyAsync())
            {
                _logger.LogInformation("Business mapping data already exists. Skipping seed.");
                return;
            }

            try
            {
                await SeedPropertyStatusTypes(context);
                await SeedCommissionTypes(context);
                await SeedPropertyImageTypes(context);
                await SeedDocumentTypes(context);
                await SeedDocumentCategories(context);
                await SeedDocumentAccessLevels(context);
                await SeedBeneficiaryTypes(context);
                await SeedBeneficiaryStatusTypes(context);
                await SeedTenantStatusTypes(context);
                await SeedInspectionTypes(context);
                await SeedInspectionStatusTypes(context);
                await SeedInspectionAreas(context);
                await SeedConditionLevels(context);
                await SeedMaintenanceCategories(context);
                await SeedMaintenancePriorities(context);
                await SeedMaintenanceStatusTypes(context);
                await SeedMaintenanceImageTypes(context);
                await SeedExpenseCategories(context);
                await SeedPaymentTypes(context);
                await SeedPaymentStatusTypes(context);
                await SeedPaymentMethods(context);
                await SeedAllocationTypes(context);
                await SeedBeneficiaryPaymentStatusTypes(context);
                await SeedPaymentFrequencies(context);
                await SeedPaymentRuleTypes(context);

                await context.SaveChangesAsync();
                _logger.LogInformation("Business mapping data seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding business mapping data");
                throw;
            }
        }

        private async Task SeedPropertyStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PropertyStatusType { Name = "Active", Description = "Property is active", DisplayOrder = 1, IsActive = true },
                new PropertyStatusType { Name = "Inactive", Description = "Property is inactive", DisplayOrder = 2, IsActive = true },
                new PropertyStatusType { Name = "Under Maintenance", Description = "Property is under maintenance", DisplayOrder = 3, IsActive = true },
                new PropertyStatusType { Name = "Vacant", Description = "Property is vacant", DisplayOrder = 4, IsActive = true },
                new PropertyStatusType { Name = "Occupied", Description = "Property is occupied", DisplayOrder = 5, IsActive = true }
            };

            await context.PropertyStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedCommissionTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new CommissionType { Name = "Percentage", Description = "Commission based on percentage", DisplayOrder = 1, IsActive = true },
                new CommissionType { Name = "Fixed Amount", Description = "Fixed commission amount", DisplayOrder = 2, IsActive = true }
            };

            await context.CommissionTypes.AddRangeAsync(items);
        }

        private async Task SeedPropertyImageTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PropertyImageType { Name = "General", Description = "General property image", DisplayOrder = 1, IsActive = true },
                new PropertyImageType { Name = "Exterior", Description = "Exterior view", DisplayOrder = 2, IsActive = true },
                new PropertyImageType { Name = "Interior", Description = "Interior view", DisplayOrder = 3, IsActive = true },
                new PropertyImageType { Name = "Kitchen", Description = "Kitchen area", DisplayOrder = 4, IsActive = true },
                new PropertyImageType { Name = "Bathroom", Description = "Bathroom area", DisplayOrder = 5, IsActive = true },
                new PropertyImageType { Name = "Bedroom", Description = "Bedroom area", DisplayOrder = 6, IsActive = true },
                new PropertyImageType { Name = "Living Room", Description = "Living room area", DisplayOrder = 7, IsActive = true },
                new PropertyImageType { Name = "Garden", Description = "Garden area", DisplayOrder = 8, IsActive = true },
                new PropertyImageType { Name = "Parking", Description = "Parking area", DisplayOrder = 9, IsActive = true },
                new PropertyImageType { Name = "Floor Plan", Description = "Property floor plan", DisplayOrder = 10, IsActive = true },
                new PropertyImageType { Name = "Document", Description = "Document image", DisplayOrder = 11, IsActive = true }
            };

            await context.PropertyImageTypes.AddRangeAsync(items);
        }

        private async Task SeedDocumentTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new DocumentType { Name = "Lease Agreement", Description = "Lease agreement document", DisplayOrder = 1, IsActive = true },
                new DocumentType { Name = "Title Deed", Description = "Property title deed", DisplayOrder = 2, IsActive = true },
                new DocumentType { Name = "Insurance", Description = "Insurance document", DisplayOrder = 3, IsActive = true },
                new DocumentType { Name = "Inspection Report", Description = "Property inspection report", DisplayOrder = 4, IsActive = true },
                new DocumentType { Name = "Maintenance Report", Description = "Maintenance report", DisplayOrder = 5, IsActive = true },
                new DocumentType { Name = "Financial Statement", Description = "Financial statement", DisplayOrder = 6, IsActive = true },
                new DocumentType { Name = "Tax Document", Description = "Tax related document", DisplayOrder = 7, IsActive = true },
                new DocumentType { Name = "Permit", Description = "Permit document", DisplayOrder = 8, IsActive = true },
                new DocumentType { Name = "Certificate", Description = "Certificate document", DisplayOrder = 9, IsActive = true },
                new DocumentType { Name = "Photo", Description = "Photo document", DisplayOrder = 10, IsActive = true },
                new DocumentType { Name = "Other", Description = "Other document type", DisplayOrder = 11, IsActive = true }
            };

            await context.DocumentTypes.AddRangeAsync(items);
        }

        private async Task SeedDocumentCategories(ApplicationDbContext context)
        {
            var items = new[]
            {
                new DocumentCategory { Name = "Legal", Description = "Legal documents", DisplayOrder = 1, IsActive = true },
                new DocumentCategory { Name = "Financial", Description = "Financial documents", DisplayOrder = 2, IsActive = true },
                new DocumentCategory { Name = "Maintenance", Description = "Maintenance documents", DisplayOrder = 3, IsActive = true },
                new DocumentCategory { Name = "Inspection", Description = "Inspection documents", DisplayOrder = 4, IsActive = true },
                new DocumentCategory { Name = "Insurance", Description = "Insurance documents", DisplayOrder = 5, IsActive = true },
                new DocumentCategory { Name = "Compliance", Description = "Compliance documents", DisplayOrder = 6, IsActive = true },
                new DocumentCategory { Name = "Marketing", Description = "Marketing documents", DisplayOrder = 7, IsActive = true },
                new DocumentCategory { Name = "General", Description = "General documents", DisplayOrder = 8, IsActive = true }
            };

            await context.DocumentCategories.AddRangeAsync(items);
        }

        private async Task SeedDocumentAccessLevels(ApplicationDbContext context)
        {
            var items = new[]
            {
                new DocumentAccessLevel { Name = "Public", Description = "Publicly accessible", DisplayOrder = 1, IsActive = true },
                new DocumentAccessLevel { Name = "Internal", Description = "Internal access only", DisplayOrder = 2, IsActive = true },
                new DocumentAccessLevel { Name = "Restricted", Description = "Restricted access", DisplayOrder = 3, IsActive = true },
                new DocumentAccessLevel { Name = "Confidential", Description = "Confidential access", DisplayOrder = 4, IsActive = true }
            };

            await context.DocumentAccessLevels.AddRangeAsync(items);
        }

        private async Task SeedBeneficiaryTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new BeneficiaryType { Name = "Owner", Description = "Property owner", DisplayOrder = 1, IsActive = true },
                new BeneficiaryType { Name = "Agent", Description = "Property agent", DisplayOrder = 2, IsActive = true },
                new BeneficiaryType { Name = "Manager", Description = "Property manager", DisplayOrder = 3, IsActive = true },
                new BeneficiaryType { Name = "Other", Description = "Other beneficiary type", DisplayOrder = 4, IsActive = true }
            };

            await context.BeneficiaryTypes.AddRangeAsync(items);
        }

        private async Task SeedBeneficiaryStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new BeneficiaryStatusType { Name = "Active", Description = "Active beneficiary", DisplayOrder = 1, IsActive = true },
                new BeneficiaryStatusType { Name = "Inactive", Description = "Inactive beneficiary", DisplayOrder = 2, IsActive = true },
                new BeneficiaryStatusType { Name = "Suspended", Description = "Suspended beneficiary", DisplayOrder = 3, IsActive = true },
                new BeneficiaryStatusType { Name = "Terminated", Description = "Terminated beneficiary", DisplayOrder = 4, IsActive = true }
            };

            await context.BeneficiaryStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedTenantStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new TenantStatusType { Name = "Pending", Description = "Pending tenant", DisplayOrder = 1, IsActive = true },
                new TenantStatusType { Name = "Active", Description = "Active tenant", DisplayOrder = 2, IsActive = true },
                new TenantStatusType { Name = "Inactive", Description = "Inactive tenant", DisplayOrder = 3, IsActive = true },
                new TenantStatusType { Name = "Evicted", Description = "Evicted tenant", DisplayOrder = 4, IsActive = true },
                new TenantStatusType { Name = "Moved Out", Description = "Moved out tenant", DisplayOrder = 5, IsActive = true }
            };

            await context.TenantStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedInspectionTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new InspectionType { Name = "Move In", Description = "Move-in inspection", DisplayOrder = 1, IsActive = true },
                new InspectionType { Name = "Move Out", Description = "Move-out inspection", DisplayOrder = 2, IsActive = true },
                new InspectionType { Name = "Routine", Description = "Routine inspection", DisplayOrder = 3, IsActive = true },
                new InspectionType { Name = "Annual", Description = "Annual inspection", DisplayOrder = 4, IsActive = true },
                new InspectionType { Name = "Pre Purchase", Description = "Pre-purchase inspection", DisplayOrder = 5, IsActive = true },
                new InspectionType { Name = "Post Renovation", Description = "Post-renovation inspection", DisplayOrder = 6, IsActive = true },
                new InspectionType { Name = "Emergency Damage", Description = "Emergency damage inspection", DisplayOrder = 7, IsActive = true }
            };

            await context.InspectionTypes.AddRangeAsync(items);
        }

        private async Task SeedInspectionStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new InspectionStatusType { Name = "Scheduled", Description = "Inspection scheduled", DisplayOrder = 1, IsActive = true },
                new InspectionStatusType { Name = "In Progress", Description = "Inspection in progress", DisplayOrder = 2, IsActive = true },
                new InspectionStatusType { Name = "Completed", Description = "Inspection completed", DisplayOrder = 3, IsActive = true },
                new InspectionStatusType { Name = "Cancelled", Description = "Inspection cancelled", DisplayOrder = 4, IsActive = true },
                new InspectionStatusType { Name = "Requires Review", Description = "Inspection requires review", DisplayOrder = 5, IsActive = true }
            };

            await context.InspectionStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedInspectionAreas(ApplicationDbContext context)
        {
            var items = new[]
            {
                new InspectionArea { Name = "Exterior", Description = "Property exterior", DisplayOrder = 1, IsActive = true },
                new InspectionArea { Name = "Interior", Description = "Property interior", DisplayOrder = 2, IsActive = true },
                new InspectionArea { Name = "Kitchen", Description = "Kitchen area", DisplayOrder = 3, IsActive = true },
                new InspectionArea { Name = "Bathroom", Description = "Bathroom area", DisplayOrder = 4, IsActive = true },
                new InspectionArea { Name = "Bedroom", Description = "Bedroom area", DisplayOrder = 5, IsActive = true },
                new InspectionArea { Name = "Living Room", Description = "Living room area", DisplayOrder = 6, IsActive = true },
                new InspectionArea { Name = "Dining Room", Description = "Dining room area", DisplayOrder = 7, IsActive = true },
                new InspectionArea { Name = "Garage", Description = "Garage area", DisplayOrder = 8, IsActive = true },
                new InspectionArea { Name = "Garden", Description = "Garden area", DisplayOrder = 9, IsActive = true },
                new InspectionArea { Name = "Roof", Description = "Roof area", DisplayOrder = 10, IsActive = true },
                new InspectionArea { Name = "Foundation", Description = "Foundation", DisplayOrder = 11, IsActive = true },
                new InspectionArea { Name = "Plumbing", Description = "Plumbing system", DisplayOrder = 12, IsActive = true },
                new InspectionArea { Name = "Electrical", Description = "Electrical system", DisplayOrder = 13, IsActive = true },
                new InspectionArea { Name = "HVAC", Description = "HVAC system", DisplayOrder = 14, IsActive = true },
                new InspectionArea { Name = "Windows", Description = "Windows", DisplayOrder = 15, IsActive = true },
                new InspectionArea { Name = "Doors", Description = "Doors", DisplayOrder = 16, IsActive = true },
                new InspectionArea { Name = "Floors", Description = "Floors", DisplayOrder = 17, IsActive = true },
                new InspectionArea { Name = "Walls", Description = "Walls", DisplayOrder = 18, IsActive = true },
                new InspectionArea { Name = "Ceilings", Description = "Ceilings", DisplayOrder = 19, IsActive = true },
                new InspectionArea { Name = "Stairs", Description = "Stairs", DisplayOrder = 20, IsActive = true },
                new InspectionArea { Name = "Other", Description = "Other areas", DisplayOrder = 21, IsActive = true }
            };

            await context.InspectionAreas.AddRangeAsync(items);
        }

        private async Task SeedConditionLevels(ApplicationDbContext context)
        {
            var items = new[]
            {
                new ConditionLevel { Name = "Excellent", Description = "Excellent condition", DisplayOrder = 1, ScoreValue = 5, IsActive = true },
                new ConditionLevel { Name = "Good", Description = "Good condition", DisplayOrder = 2, ScoreValue = 4, IsActive = true },
                new ConditionLevel { Name = "Fair", Description = "Fair condition", DisplayOrder = 3, ScoreValue = 3, IsActive = true },
                new ConditionLevel { Name = "Poor", Description = "Poor condition", DisplayOrder = 4, ScoreValue = 2, IsActive = true },
                new ConditionLevel { Name = "Critical", Description = "Critical condition", DisplayOrder = 5, ScoreValue = 1, IsActive = true }
            };

            await context.ConditionLevels.AddRangeAsync(items);
        }

        private async Task SeedMaintenanceCategories(ApplicationDbContext context)
        {
            var items = new[]
            {
                new MaintenanceCategory { Name = "Plumbing", Description = "Plumbing issues", DisplayOrder = 1, IsActive = true },
                new MaintenanceCategory { Name = "Electrical", Description = "Electrical issues", DisplayOrder = 2, IsActive = true },
                new MaintenanceCategory { Name = "HVAC", Description = "HVAC issues", DisplayOrder = 3, IsActive = true },
                new MaintenanceCategory { Name = "Appliances", Description = "Appliance issues", DisplayOrder = 4, IsActive = true },
                new MaintenanceCategory { Name = "Structural", Description = "Structural issues", DisplayOrder = 5, IsActive = true },
                new MaintenanceCategory { Name = "Painting", Description = "Painting work", DisplayOrder = 6, IsActive = true },
                new MaintenanceCategory { Name = "Cleaning", Description = "Cleaning services", DisplayOrder = 7, IsActive = true },
                new MaintenanceCategory { Name = "Landscaping", Description = "Landscaping work", DisplayOrder = 8, IsActive = true },
                new MaintenanceCategory { Name = "Security", Description = "Security issues", DisplayOrder = 9, IsActive = true },
                new MaintenanceCategory { Name = "Other", Description = "Other maintenance", DisplayOrder = 10, IsActive = true }
            };

            await context.MaintenanceCategories.AddRangeAsync(items);
        }

        private async Task SeedMaintenancePriorities(ApplicationDbContext context)
        {
            var items = new[]
            {
                new MaintenancePriority { Name = "Low", Description = "Low priority", DisplayOrder = 1, ResponseTimeHours = 168, IsActive = true },
                new MaintenancePriority { Name = "Medium", Description = "Medium priority", DisplayOrder = 2, ResponseTimeHours = 72, IsActive = true },
                new MaintenancePriority { Name = "High", Description = "High priority", DisplayOrder = 3, ResponseTimeHours = 24, IsActive = true },
                new MaintenancePriority { Name = "Urgent", Description = "Urgent priority", DisplayOrder = 4, ResponseTimeHours = 4, IsActive = true },
                new MaintenancePriority { Name = "Emergency", Description = "Emergency priority", DisplayOrder = 5, ResponseTimeHours = 1, IsActive = true }
            };

            await context.MaintenancePriorities.AddRangeAsync(items);
        }

        private async Task SeedMaintenanceStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new MaintenanceStatusType { Name = "Open", Description = "Ticket open", DisplayOrder = 1, IsActive = true },
                new MaintenanceStatusType { Name = "Assigned", Description = "Ticket assigned", DisplayOrder = 2, IsActive = true },
                new MaintenanceStatusType { Name = "In Progress", Description = "Work in progress", DisplayOrder = 3, IsActive = true },
                new MaintenanceStatusType { Name = "On Hold", Description = "Work on hold", DisplayOrder = 4, IsActive = true },
                new MaintenanceStatusType { Name = "Completed", Description = "Work completed", DisplayOrder = 5, IsActive = true },
                new MaintenanceStatusType { Name = "Cancelled", Description = "Ticket cancelled", DisplayOrder = 6, IsActive = true },
                new MaintenanceStatusType { Name = "Requires Approval", Description = "Requires approval", DisplayOrder = 7, IsActive = true }
            };

            await context.MaintenanceStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedMaintenanceImageTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new MaintenanceImageType { Name = "Before", Description = "Before repair", DisplayOrder = 1, IsActive = true },
                new MaintenanceImageType { Name = "During", Description = "During repair", DisplayOrder = 2, IsActive = true },
                new MaintenanceImageType { Name = "After", Description = "After repair", DisplayOrder = 3, IsActive = true },
                new MaintenanceImageType { Name = "Receipt", Description = "Receipt/Invoice", DisplayOrder = 4, IsActive = true },
                new MaintenanceImageType { Name = "Other", Description = "Other image", DisplayOrder = 5, IsActive = true }
            };

            await context.MaintenanceImageTypes.AddRangeAsync(items);
        }

        private async Task SeedExpenseCategories(ApplicationDbContext context)
        {
            var items = new[]
            {
                new ExpenseCategory { Name = "Materials", Description = "Material costs", DisplayOrder = 1, IsActive = true },
                new ExpenseCategory { Name = "Labor", Description = "Labor costs", DisplayOrder = 2, IsActive = true },
                new ExpenseCategory { Name = "Equipment", Description = "Equipment costs", DisplayOrder = 3, IsActive = true },
                new ExpenseCategory { Name = "Transportation", Description = "Transportation costs", DisplayOrder = 4, IsActive = true },
                new ExpenseCategory { Name = "Other", Description = "Other expenses", DisplayOrder = 5, IsActive = true }
            };

            await context.ExpenseCategories.AddRangeAsync(items);
        }

        private async Task SeedPaymentTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentType { Name = "Rent", Description = "Rent payment", DisplayOrder = 1, IsActive = true },
                new PaymentType { Name = "Deposit", Description = "Security deposit", DisplayOrder = 2, IsActive = true },
                new PaymentType { Name = "Utility Bill", Description = "Utility bill payment", DisplayOrder = 3, IsActive = true },
                new PaymentType { Name = "Maintenance Fee", Description = "Maintenance fee", DisplayOrder = 4, IsActive = true },
                new PaymentType { Name = "Late Fee", Description = "Late payment fee", DisplayOrder = 5, IsActive = true },
                new PaymentType { Name = "Other", Description = "Other payment type", DisplayOrder = 6, IsActive = true }
            };

            await context.PaymentTypes.AddRangeAsync(items);
        }

        private async Task SeedPaymentStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentStatusType { Name = "Pending", Description = "Payment pending", DisplayOrder = 1, IsActive = true },
                new PaymentStatusType { Name = "Paid", Description = "Payment received", DisplayOrder = 2, IsActive = true },
                new PaymentStatusType { Name = "Partially Paid", Description = "Partially paid", DisplayOrder = 3, IsActive = true },
                new PaymentStatusType { Name = "Overdue", Description = "Payment overdue", DisplayOrder = 4, IsActive = true },
                new PaymentStatusType { Name = "Cancelled", Description = "Payment cancelled", DisplayOrder = 5, IsActive = true },
                new PaymentStatusType { Name = "Refunded", Description = "Payment refunded", DisplayOrder = 6, IsActive = true }
            };

            await context.PaymentStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedPaymentMethods(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentMethod { Name = "Bank Transfer", Description = "Bank transfer payment", DisplayOrder = 1, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 0m, IsActive = true },
                new PaymentMethod { Name = "Credit Card", Description = "Credit card payment", DisplayOrder = 2, ProcessingFeePercentage = 2.9m, ProcessingFeeFixed = 0.30m, IsActive = true },
                new PaymentMethod { Name = "Debit Card", Description = "Debit card payment", DisplayOrder = 3, ProcessingFeePercentage = 1.5m, ProcessingFeeFixed = 0.25m, IsActive = true },
                new PaymentMethod { Name = "Cash", Description = "Cash payment", DisplayOrder = 4, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 0m, IsActive = true },
                new PaymentMethod { Name = "Cheque", Description = "Cheque payment", DisplayOrder = 5, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 0m, IsActive = true },
                new PaymentMethod { Name = "Online Payment", Description = "Online payment gateway", DisplayOrder = 6, ProcessingFeePercentage = 2.5m, ProcessingFeeFixed = 0m, IsActive = true },
                new PaymentMethod { Name = "Other", Description = "Other payment method", DisplayOrder = 7, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 0m, IsActive = true }
            };

            await context.PaymentMethods.AddRangeAsync(items);
        }

        private async Task SeedAllocationTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new AllocationType { Name = "Rent", Description = "Rent allocation", DisplayOrder = 1, IsActive = true },
                new AllocationType { Name = "Commission", Description = "Commission allocation", DisplayOrder = 2, IsActive = true },
                new AllocationType { Name = "Management Fee", Description = "Management fee allocation", DisplayOrder = 3, IsActive = true },
                new AllocationType { Name = "Maintenance", Description = "Maintenance allocation", DisplayOrder = 4, IsActive = true },
                new AllocationType { Name = "Other", Description = "Other allocation", DisplayOrder = 5, IsActive = true }
            };

            await context.AllocationTypes.AddRangeAsync(items);
        }

        private async Task SeedBeneficiaryPaymentStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new BeneficiaryPaymentStatusType { Name = "Pending", Description = "Payment pending", DisplayOrder = 1, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "Processed", Description = "Payment processed", DisplayOrder = 2, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "Paid", Description = "Payment paid", DisplayOrder = 3, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "Failed", Description = "Payment failed", DisplayOrder = 4, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "Cancelled", Description = "Payment cancelled", DisplayOrder = 5, IsActive = true }
            };

            await context.BeneficiaryPaymentStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedPaymentFrequencies(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentFrequency { Name = "Monthly", Description = "Monthly payment", DaysInterval = 30, DisplayOrder = 1, IsActive = true },
                new PaymentFrequency { Name = "Quarterly", Description = "Quarterly payment", DaysInterval = 90, DisplayOrder = 2, IsActive = true },
                new PaymentFrequency { Name = "Semi-Annual", Description = "Semi-annual payment", DaysInterval = 180, DisplayOrder = 3, IsActive = true },
                new PaymentFrequency { Name = "Annual", Description = "Annual payment", DaysInterval = 365, DisplayOrder = 4, IsActive = true },
                new PaymentFrequency { Name = "One Time", Description = "One-time payment", DaysInterval = null, DisplayOrder = 5, IsActive = true }
            };

            await context.PaymentFrequencies.AddRangeAsync(items);
        }

        private async Task SeedPaymentRuleTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentRuleType { Name = "Late Fee", Description = "Late fee rules", DisplayOrder = 1, IsActive = true },
                new PaymentRuleType { Name = "Reminder", Description = "Payment reminder rules", DisplayOrder = 2, IsActive = true },
                new PaymentRuleType { Name = "Allocation", Description = "Payment allocation rules", DisplayOrder = 3, IsActive = true },
                new PaymentRuleType { Name = "General", Description = "General payment rules", DisplayOrder = 4, IsActive = true }
            };

            await context.PaymentRuleTypes.AddRangeAsync(items);
        }

        #endregion Business Mapping Data

        #region User Company Mapping Data

        public async Task SeedUserCompanyMappingDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if data already exists
            if (await context.CompanyStatusTypes.AnyAsync())
            {
                _logger.LogInformation("User company mapping data already exists. Skipping seed.");
                return;
            }

            try
            {
                await SeedCompanyStatusTypes(context);
                await SeedSubscriptionPlans(context);
                await SeedBranchStatusTypes(context);
                await SeedUserStatusTypes(context);
                await SeedTwoFactorMethods(context);
                await SeedPermissionCategories(context);
                await SeedRoleTypes(context);
                await SeedNotificationTypes(context);
                await SeedNotificationChannels(context);
                await SeedThemeTypes(context);

                await context.SaveChangesAsync();
                _logger.LogInformation("User company mapping data seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding user company mapping data");
                throw;
            }
        }

        private async Task SeedCompanyStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new CompanyStatusType { Name = "Active", Description = "Company is active", DisplayOrder = 1, IsActive = true },
                new CompanyStatusType { Name = "Inactive", Description = "Company is inactive", DisplayOrder = 2, IsActive = true },
                new CompanyStatusType { Name = "Suspended", Description = "Company is suspended", DisplayOrder = 3, IsActive = true },
                new CompanyStatusType { Name = "Trial", Description = "Company is in trial period", DisplayOrder = 4, IsActive = true }
            };

            await context.CompanyStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedSubscriptionPlans(ApplicationDbContext context)
        {
            var items = new[]
            {
                new SubscriptionPlan
                {
                    Name = "Starter",
                    Description = "Starter plan for small companies",
                    Price = 99.99m,
                    BillingCycleDays = 30,
                    MaxUsers = 5,
                    MaxProperties = 10,
                    MaxBranches = 1,
                    HasTrialPeriod = true,
                    TrialPeriodDays = 14,
                    DisplayOrder = 1,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Name = "Professional",
                    Description = "Professional plan for growing companies",
                    Price = 299.99m,
                    BillingCycleDays = 30,
                    MaxUsers = 25,
                    MaxProperties = 50,
                    MaxBranches = 5,
                    HasTrialPeriod = true,
                    TrialPeriodDays = 14,
                    DisplayOrder = 2,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Name = "Enterprise",
                    Description = "Enterprise plan for large companies",
                    Price = 999.99m,
                    BillingCycleDays = 30,
                    MaxUsers = 100,
                    MaxProperties = 500,
                    MaxBranches = 20,
                    HasTrialPeriod = false,
                    TrialPeriodDays = null,
                    DisplayOrder = 3,
                    IsActive = true
                }
            };

            await context.SubscriptionPlans.AddRangeAsync(items);
        }

        private async Task SeedBranchStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new BranchStatusType { Name = "Active", Description = "Branch is active", DisplayOrder = 1, IsActive = true },
                new BranchStatusType { Name = "Inactive", Description = "Branch is inactive", DisplayOrder = 2, IsActive = true },
                new BranchStatusType { Name = "Temporary Closed", Description = "Branch is temporarily closed", DisplayOrder = 3, IsActive = true }
            };

            await context.BranchStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedUserStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new UserStatusType { Name = "Active", Description = "User is active", DisplayOrder = 1, IsActive = true },
                new UserStatusType { Name = "Inactive", Description = "User is inactive", DisplayOrder = 2, IsActive = true },
                new UserStatusType { Name = "Suspended", Description = "User is suspended", DisplayOrder = 3, IsActive = true },
                new UserStatusType { Name = "Locked", Description = "User account is locked", DisplayOrder = 4, IsActive = true }
            };

            await context.UserStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedTwoFactorMethods(ApplicationDbContext context)
        {
            var items = new[]
            {
                new TwoFactorMethod { Name = "SMS", Description = "SMS-based 2FA", DisplayOrder = 1, IsActive = true },
                new TwoFactorMethod { Name = "Email", Description = "Email-based 2FA", DisplayOrder = 2, IsActive = true },
                new TwoFactorMethod { Name = "Authenticator App", Description = "Authenticator app 2FA", DisplayOrder = 3, IsActive = true }
            };

            await context.TwoFactorMethods.AddRangeAsync(items);
        }

        private async Task SeedPermissionCategories(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PermissionCategory { Name = "Properties", Description = "Property management permissions", Icon = "fa-building", DisplayOrder = 1, IsActive = true },
                new PermissionCategory { Name = "Tenants", Description = "Tenant management permissions", Icon = "fa-users", DisplayOrder = 2, IsActive = true },
                new PermissionCategory { Name = "Payments", Description = "Payment management permissions", Icon = "fa-credit-card", DisplayOrder = 3, IsActive = true },
                new PermissionCategory { Name = "Maintenance", Description = "Maintenance management permissions", Icon = "fa-tools", DisplayOrder = 4, IsActive = true },
                new PermissionCategory { Name = "Reports", Description = "Reporting permissions", Icon = "fa-chart-bar", DisplayOrder = 5, IsActive = true },
                new PermissionCategory { Name = "Administration", Description = "Administration permissions", Icon = "fa-cog", DisplayOrder = 6, IsActive = true }
            };

            await context.PermissionCategories.AddRangeAsync(items);
        }

        private async Task SeedRoleTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new RoleType { Name = "System", Description = "System-level roles", DisplayOrder = 1, IsActive = true },
                new RoleType { Name = "Company", Description = "Company-level roles", DisplayOrder = 2, IsActive = true },
                new RoleType { Name = "Branch", Description = "Branch-level roles", DisplayOrder = 3, IsActive = true }
            };

            await context.RoleTypes.AddRangeAsync(items);
        }

        private async Task SeedNotificationTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new NotificationType { Name = "Payment Due", Description = "Payment due notification", Icon = "fa-dollar-sign", DisplayOrder = 1, IsActive = true },
                new NotificationType { Name = "Payment Received", Description = "Payment received notification", Icon = "fa-check-circle", DisplayOrder = 2, IsActive = true },
                new NotificationType { Name = "Maintenance Request", Description = "Maintenance request notification", Icon = "fa-wrench", DisplayOrder = 3, IsActive = true },
                new NotificationType { Name = "Lease Expiry", Description = "Lease expiry notification", Icon = "fa-calendar-alt", DisplayOrder = 4, IsActive = true },
                new NotificationType { Name = "System Alert", Description = "System alert notification", Icon = "fa-exclamation-triangle", DisplayOrder = 5, IsActive = true }
            };

            await context.NotificationTypes.AddRangeAsync(items);
        }

        private async Task SeedNotificationChannels(ApplicationDbContext context)
        {
            var items = new[]
            {
                new NotificationChannel { Name = "Email", Description = "Email notifications", DisplayOrder = 1, IsActive = true },
                new NotificationChannel { Name = "SMS", Description = "SMS notifications", DisplayOrder = 2, IsActive = true },
                new NotificationChannel { Name = "Push", Description = "Push notifications", DisplayOrder = 3, IsActive = true },
                new NotificationChannel { Name = "In-App", Description = "In-app notifications", DisplayOrder = 4, IsActive = true }
            };

            await context.NotificationChannels.AddRangeAsync(items);
        }

        private async Task SeedThemeTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new ThemeType
                {
                    Name = "Light",
                    Description = "Light theme",
                    CssVariables = "{ \"primary\": \"#007bff\", \"secondary\": \"#6c757d\", \"background\": \"#ffffff\" }",
                    IsDarkTheme = false,
                    DisplayOrder = 1,
                    IsActive = true
                },
                new ThemeType
                {
                    Name = "Dark",
                    Description = "Dark theme",
                    CssVariables = "{ \"primary\": \"#0099ff\", \"secondary\": \"#6c757d\", \"background\": \"#1a1a1a\" }",
                    IsDarkTheme = true,
                    DisplayOrder = 2,
                    IsActive = true
                },
                new ThemeType
                {
                    Name = "Blue",
                    Description = "Blue theme",
                    CssVariables = "{ \"primary\": \"#004ba0\", \"secondary\": \"#1976d2\", \"background\": \"#f5f5f5\" }",
                    IsDarkTheme = false,
                    DisplayOrder = 3,
                    IsActive = true
                }
            };

            await context.ThemeTypes.AddRangeAsync(items);
        }

        #endregion User Company Mapping Data

        #region CDN Configuration

        public async Task SeedCdnConfigurationAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if data already exists
            if (await context.CdnConfigurations.AnyAsync())
            {
                _logger.LogInformation("CDN configuration already exists. Skipping seed.");
                return;
            }

            try
            {
                // Seed default CDN configuration
                var config = new CdnConfiguration
                {
                    BaseUrl = "https://portal.roovia.co.za/cdn",
                    StoragePath = "/var/www/cdn",
                    MaxFileSizeMB = 200,
                    AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.mp4,.mp3,.zip",
                    EnableCaching = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = "System",
                    IsActive = true
                };

                await context.CdnConfigurations.AddAsync(config);

                // Seed default CDN categories
                var categories = new[]
                {
                    new CdnCategory { Name = "properties", DisplayName = "Properties", Description = "Property-related files", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.pdf", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "documents", DisplayName = "Documents", Description = "General documents", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.txt", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "profiles", DisplayName = "Profiles", Description = "User profile images", AllowedFileTypes = ".jpg,.jpeg,.png,.gif", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "logos", DisplayName = "Logos", Description = "Company and branch logos", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.svg", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "maintenance", DisplayName = "Maintenance", Description = "Maintenance-related files", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.pdf", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "inspections", DisplayName = "Inspections", Description = "Inspection-related files", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.pdf", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "receipts", DisplayName = "Receipts", Description = "Payment receipts and invoices", AllowedFileTypes = ".pdf,.jpg,.jpeg,.png", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                };

                await context.CdnCategories.AddRangeAsync(categories);

                await context.SaveChangesAsync();
                _logger.LogInformation("CDN configuration seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding CDN configuration");
                throw;
            }
        }

        #endregion CDN Configuration
    }
}