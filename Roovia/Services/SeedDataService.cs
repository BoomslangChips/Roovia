using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Models.BusinessMappingModels;
using Roovia.Models.UserCompanyMappingModels;
using Roovia.Models.ProjectCdnConfigModels;
using System;
using System.Threading.Tasks;

namespace Roovia.Services
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
                new PropertyStatusType { Id = 1, Name = "Active", Description = "Property is active", DisplayOrder = 1, IsActive = true },
                new PropertyStatusType { Id = 2, Name = "Inactive", Description = "Property is inactive", DisplayOrder = 2, IsActive = true },
                new PropertyStatusType { Id = 3, Name = "Under Maintenance", Description = "Property is under maintenance", DisplayOrder = 3, IsActive = true },
                new PropertyStatusType { Id = 4, Name = "Vacant", Description = "Property is vacant", DisplayOrder = 4, IsActive = true },
                new PropertyStatusType { Id = 5, Name = "Occupied", Description = "Property is occupied", DisplayOrder = 5, IsActive = true }
            };

            await context.PropertyStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedCommissionTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new CommissionType { Id = 1, Name = "Percentage", Description = "Commission based on percentage", DisplayOrder = 1, IsActive = true },
                new CommissionType { Id = 2, Name = "Fixed Amount", Description = "Fixed commission amount", DisplayOrder = 2, IsActive = true }
            };

            await context.CommissionTypes.AddRangeAsync(items);
        }

        private async Task SeedPropertyImageTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PropertyImageType { Id = 1, Name = "General", Description = "General property image", DisplayOrder = 1, IsActive = true },
                new PropertyImageType { Id = 2, Name = "Exterior", Description = "Exterior view", DisplayOrder = 2, IsActive = true },
                new PropertyImageType { Id = 3, Name = "Interior", Description = "Interior view", DisplayOrder = 3, IsActive = true },
                new PropertyImageType { Id = 4, Name = "Kitchen", Description = "Kitchen area", DisplayOrder = 4, IsActive = true },
                new PropertyImageType { Id = 5, Name = "Bathroom", Description = "Bathroom area", DisplayOrder = 5, IsActive = true },
                new PropertyImageType { Id = 6, Name = "Bedroom", Description = "Bedroom area", DisplayOrder = 6, IsActive = true },
                new PropertyImageType { Id = 7, Name = "Living Room", Description = "Living room area", DisplayOrder = 7, IsActive = true },
                new PropertyImageType { Id = 8, Name = "Garden", Description = "Garden area", DisplayOrder = 8, IsActive = true },
                new PropertyImageType { Id = 9, Name = "Parking", Description = "Parking area", DisplayOrder = 9, IsActive = true },
                new PropertyImageType { Id = 10, Name = "Floor Plan", Description = "Property floor plan", DisplayOrder = 10, IsActive = true },
                new PropertyImageType { Id = 11, Name = "Document", Description = "Document image", DisplayOrder = 11, IsActive = true }
            };

            await context.PropertyImageTypes.AddRangeAsync(items);
        }

        private async Task SeedDocumentTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new DocumentType { Id = 1, Name = "Lease Agreement", Description = "Lease agreement document", DisplayOrder = 1, IsActive = true },
                new DocumentType { Id = 2, Name = "Title Deed", Description = "Property title deed", DisplayOrder = 2, IsActive = true },
                new DocumentType { Id = 3, Name = "Insurance", Description = "Insurance document", DisplayOrder = 3, IsActive = true },
                new DocumentType { Id = 4, Name = "Inspection Report", Description = "Property inspection report", DisplayOrder = 4, IsActive = true },
                new DocumentType { Id = 5, Name = "Maintenance Report", Description = "Maintenance report", DisplayOrder = 5, IsActive = true },
                new DocumentType { Id = 6, Name = "Financial Statement", Description = "Financial statement", DisplayOrder = 6, IsActive = true },
                new DocumentType { Id = 7, Name = "Tax Document", Description = "Tax related document", DisplayOrder = 7, IsActive = true },
                new DocumentType { Id = 8, Name = "Permit", Description = "Permit document", DisplayOrder = 8, IsActive = true },
                new DocumentType { Id = 9, Name = "Certificate", Description = "Certificate document", DisplayOrder = 9, IsActive = true },
                new DocumentType { Id = 10, Name = "Photo", Description = "Photo document", DisplayOrder = 10, IsActive = true },
                new DocumentType { Id = 11, Name = "Other", Description = "Other document type", DisplayOrder = 11, IsActive = true }
            };

            await context.DocumentTypes.AddRangeAsync(items);
        }

        private async Task SeedDocumentCategories(ApplicationDbContext context)
        {
            var items = new[]
            {
                new DocumentCategory { Id = 1, Name = "Legal", Description = "Legal documents", DisplayOrder = 1, IsActive = true },
                new DocumentCategory { Id = 2, Name = "Financial", Description = "Financial documents", DisplayOrder = 2, IsActive = true },
                new DocumentCategory { Id = 3, Name = "Maintenance", Description = "Maintenance documents", DisplayOrder = 3, IsActive = true },
                new DocumentCategory { Id = 4, Name = "Inspection", Description = "Inspection documents", DisplayOrder = 4, IsActive = true },
                new DocumentCategory { Id = 5, Name = "Insurance", Description = "Insurance documents", DisplayOrder = 5, IsActive = true },
                new DocumentCategory { Id = 6, Name = "Compliance", Description = "Compliance documents", DisplayOrder = 6, IsActive = true },
                new DocumentCategory { Id = 7, Name = "Marketing", Description = "Marketing documents", DisplayOrder = 7, IsActive = true },
                new DocumentCategory { Id = 8, Name = "General", Description = "General documents", DisplayOrder = 8, IsActive = true }
            };

            await context.DocumentCategories.AddRangeAsync(items);
        }

        private async Task SeedDocumentAccessLevels(ApplicationDbContext context)
        {
            var items = new[]
            {
                new DocumentAccessLevel { Id = 1, Name = "Public", Description = "Publicly accessible", DisplayOrder = 1, IsActive = true },
                new DocumentAccessLevel { Id = 2, Name = "Internal", Description = "Internal access only", DisplayOrder = 2, IsActive = true },
                new DocumentAccessLevel { Id = 3, Name = "Restricted", Description = "Restricted access", DisplayOrder = 3, IsActive = true },
                new DocumentAccessLevel { Id = 4, Name = "Confidential", Description = "Confidential access", DisplayOrder = 4, IsActive = true }
            };

            await context.DocumentAccessLevels.AddRangeAsync(items);
        }

        private async Task SeedBeneficiaryTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new BeneficiaryType { Id = 1, Name = "Owner", Description = "Property owner", DisplayOrder = 1, IsActive = true },
                new BeneficiaryType { Id = 2, Name = "Agent", Description = "Property agent", DisplayOrder = 2, IsActive = true },
                new BeneficiaryType { Id = 3, Name = "Manager", Description = "Property manager", DisplayOrder = 3, IsActive = true },
                new BeneficiaryType { Id = 4, Name = "Other", Description = "Other beneficiary type", DisplayOrder = 4, IsActive = true }
            };

            await context.BeneficiaryTypes.AddRangeAsync(items);
        }

        private async Task SeedBeneficiaryStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new BeneficiaryStatusType { Id = 1, Name = "Active", Description = "Active beneficiary", DisplayOrder = 1, IsActive = true },
                new BeneficiaryStatusType { Id = 2, Name = "Inactive", Description = "Inactive beneficiary", DisplayOrder = 2, IsActive = true },
                new BeneficiaryStatusType { Id = 3, Name = "Suspended", Description = "Suspended beneficiary", DisplayOrder = 3, IsActive = true },
                new BeneficiaryStatusType { Id = 4, Name = "Terminated", Description = "Terminated beneficiary", DisplayOrder = 4, IsActive = true }
            };

            await context.BeneficiaryStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedTenantStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new TenantStatusType { Id = 1, Name = "Pending", Description = "Pending tenant", DisplayOrder = 1, IsActive = true },
                new TenantStatusType { Id = 2, Name = "Active", Description = "Active tenant", DisplayOrder = 2, IsActive = true },
                new TenantStatusType { Id = 3, Name = "Inactive", Description = "Inactive tenant", DisplayOrder = 3, IsActive = true },
                new TenantStatusType { Id = 4, Name = "Evicted", Description = "Evicted tenant", DisplayOrder = 4, IsActive = true },
                new TenantStatusType { Id = 5, Name = "Moved Out", Description = "Moved out tenant", DisplayOrder = 5, IsActive = true }
            };

            await context.TenantStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedInspectionTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new InspectionType { Id = 1, Name = "Move In", Description = "Move-in inspection", DisplayOrder = 1, IsActive = true },
                new InspectionType { Id = 2, Name = "Move Out", Description = "Move-out inspection", DisplayOrder = 2, IsActive = true },
                new InspectionType { Id = 3, Name = "Routine", Description = "Routine inspection", DisplayOrder = 3, IsActive = true },
                new InspectionType { Id = 4, Name = "Annual", Description = "Annual inspection", DisplayOrder = 4, IsActive = true },
                new InspectionType { Id = 5, Name = "Pre Purchase", Description = "Pre-purchase inspection", DisplayOrder = 5, IsActive = true },
                new InspectionType { Id = 6, Name = "Post Renovation", Description = "Post-renovation inspection", DisplayOrder = 6, IsActive = true },
                new InspectionType { Id = 7, Name = "Emergency Damage", Description = "Emergency damage inspection", DisplayOrder = 7, IsActive = true }
            };

            await context.InspectionTypes.AddRangeAsync(items);
        }

        private async Task SeedInspectionStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new InspectionStatusType { Id = 1, Name = "Scheduled", Description = "Inspection scheduled", DisplayOrder = 1, IsActive = true },
                new InspectionStatusType { Id = 2, Name = "In Progress", Description = "Inspection in progress", DisplayOrder = 2, IsActive = true },
                new InspectionStatusType { Id = 3, Name = "Completed", Description = "Inspection completed", DisplayOrder = 3, IsActive = true },
                new InspectionStatusType { Id = 4, Name = "Cancelled", Description = "Inspection cancelled", DisplayOrder = 4, IsActive = true },
                new InspectionStatusType { Id = 5, Name = "Requires Review", Description = "Inspection requires review", DisplayOrder = 5, IsActive = true }
            };

            await context.InspectionStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedInspectionAreas(ApplicationDbContext context)
        {
            var items = new[]
            {
                new InspectionArea { Id = 1, Name = "Exterior", Description = "Property exterior", DisplayOrder = 1, IsActive = true },
                new InspectionArea { Id = 2, Name = "Interior", Description = "Property interior", DisplayOrder = 2, IsActive = true },
                new InspectionArea { Id = 3, Name = "Kitchen", Description = "Kitchen area", DisplayOrder = 3, IsActive = true },
                new InspectionArea { Id = 4, Name = "Bathroom", Description = "Bathroom area", DisplayOrder = 4, IsActive = true },
                new InspectionArea { Id = 5, Name = "Bedroom", Description = "Bedroom area", DisplayOrder = 5, IsActive = true },
                new InspectionArea { Id = 6, Name = "Living Room", Description = "Living room area", DisplayOrder = 6, IsActive = true },
                new InspectionArea { Id = 7, Name = "Dining Room", Description = "Dining room area", DisplayOrder = 7, IsActive = true },
                new InspectionArea { Id = 8, Name = "Garage", Description = "Garage area", DisplayOrder = 8, IsActive = true },
                new InspectionArea { Id = 9, Name = "Garden", Description = "Garden area", DisplayOrder = 9, IsActive = true },
                new InspectionArea { Id = 10, Name = "Roof", Description = "Roof area", DisplayOrder = 10, IsActive = true },
                new InspectionArea { Id = 11, Name = "Foundation", Description = "Foundation", DisplayOrder = 11, IsActive = true },
                new InspectionArea { Id = 12, Name = "Plumbing", Description = "Plumbing system", DisplayOrder = 12, IsActive = true },
                new InspectionArea { Id = 13, Name = "Electrical", Description = "Electrical system", DisplayOrder = 13, IsActive = true },
                new InspectionArea { Id = 14, Name = "HVAC", Description = "HVAC system", DisplayOrder = 14, IsActive = true },
                new InspectionArea { Id = 15, Name = "Windows", Description = "Windows", DisplayOrder = 15, IsActive = true },
                new InspectionArea { Id = 16, Name = "Doors", Description = "Doors", DisplayOrder = 16, IsActive = true },
                new InspectionArea { Id = 17, Name = "Floors", Description = "Floors", DisplayOrder = 17, IsActive = true },
                new InspectionArea { Id = 18, Name = "Walls", Description = "Walls", DisplayOrder = 18, IsActive = true },
                new InspectionArea { Id = 19, Name = "Ceilings", Description = "Ceilings", DisplayOrder = 19, IsActive = true },
                new InspectionArea { Id = 20, Name = "Stairs", Description = "Stairs", DisplayOrder = 20, IsActive = true },
                new InspectionArea { Id = 21, Name = "Other", Description = "Other areas", DisplayOrder = 21, IsActive = true }
            };

            await context.InspectionAreas.AddRangeAsync(items);
        }

        private async Task SeedConditionLevels(ApplicationDbContext context)
        {
            var items = new[]
            {
                new ConditionLevel { Id = 1, Name = "Excellent", Description = "Excellent condition", DisplayOrder = 1, ScoreValue = 5, IsActive = true },
                new ConditionLevel { Id = 2, Name = "Good", Description = "Good condition", DisplayOrder = 2, ScoreValue = 4, IsActive = true },
                new ConditionLevel { Id = 3, Name = "Fair", Description = "Fair condition", DisplayOrder = 3, ScoreValue = 3, IsActive = true },
                new ConditionLevel { Id = 4, Name = "Poor", Description = "Poor condition", DisplayOrder = 4, ScoreValue = 2, IsActive = true },
                new ConditionLevel { Id = 5, Name = "Critical", Description = "Critical condition", DisplayOrder = 5, ScoreValue = 1, IsActive = true }
            };

            await context.ConditionLevels.AddRangeAsync(items);
        }

        private async Task SeedMaintenanceCategories(ApplicationDbContext context)
        {
            var items = new[]
            {
                new MaintenanceCategory { Id = 1, Name = "Plumbing", Description = "Plumbing issues", DisplayOrder = 1, IsActive = true },
                new MaintenanceCategory { Id = 2, Name = "Electrical", Description = "Electrical issues", DisplayOrder = 2, IsActive = true },
                new MaintenanceCategory { Id = 3, Name = "HVAC", Description = "HVAC issues", DisplayOrder = 3, IsActive = true },
                new MaintenanceCategory { Id = 4, Name = "Appliances", Description = "Appliance issues", DisplayOrder = 4, IsActive = true },
                new MaintenanceCategory { Id = 5, Name = "Structural", Description = "Structural issues", DisplayOrder = 5, IsActive = true },
                new MaintenanceCategory { Id = 6, Name = "Painting", Description = "Painting work", DisplayOrder = 6, IsActive = true },
                new MaintenanceCategory { Id = 7, Name = "Cleaning", Description = "Cleaning services", DisplayOrder = 7, IsActive = true },
                new MaintenanceCategory { Id = 8, Name = "Landscaping", Description = "Landscaping work", DisplayOrder = 8, IsActive = true },
                new MaintenanceCategory { Id = 9, Name = "Security", Description = "Security issues", DisplayOrder = 9, IsActive = true },
                new MaintenanceCategory { Id = 10, Name = "Other", Description = "Other maintenance", DisplayOrder = 10, IsActive = true }
            };

            await context.MaintenanceCategories.AddRangeAsync(items);
        }

        private async Task SeedMaintenancePriorities(ApplicationDbContext context)
        {
            var items = new[]
            {
                new MaintenancePriority { Id = 1, Name = "Low", Description = "Low priority", DisplayOrder = 1, ResponseTimeHours = 168, IsActive = true },
                new MaintenancePriority { Id = 2, Name = "Medium", Description = "Medium priority", DisplayOrder = 2, ResponseTimeHours = 72, IsActive = true },
                new MaintenancePriority { Id = 3, Name = "High", Description = "High priority", DisplayOrder = 3, ResponseTimeHours = 24, IsActive = true },
                new MaintenancePriority { Id = 4, Name = "Urgent", Description = "Urgent priority", DisplayOrder = 4, ResponseTimeHours = 4, IsActive = true },
                new MaintenancePriority { Id = 5, Name = "Emergency", Description = "Emergency priority", DisplayOrder = 5, ResponseTimeHours = 1, IsActive = true }
            };

            await context.MaintenancePriorities.AddRangeAsync(items);
        }

        private async Task SeedMaintenanceStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new MaintenanceStatusType { Id = 1, Name = "Open", Description = "Ticket open", DisplayOrder = 1, IsActive = true },
                new MaintenanceStatusType { Id = 2, Name = "Assigned", Description = "Ticket assigned", DisplayOrder = 2, IsActive = true },
                new MaintenanceStatusType { Id = 3, Name = "In Progress", Description = "Work in progress", DisplayOrder = 3, IsActive = true },
                new MaintenanceStatusType { Id = 4, Name = "On Hold", Description = "Work on hold", DisplayOrder = 4, IsActive = true },
                new MaintenanceStatusType { Id = 5, Name = "Completed", Description = "Work completed", DisplayOrder = 5, IsActive = true },
                new MaintenanceStatusType { Id = 6, Name = "Cancelled", Description = "Ticket cancelled", DisplayOrder = 6, IsActive = true },
                new MaintenanceStatusType { Id = 7, Name = "Requires Approval", Description = "Requires approval", DisplayOrder = 7, IsActive = true }
            };

            await context.MaintenanceStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedMaintenanceImageTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new MaintenanceImageType { Id = 1, Name = "Before", Description = "Before repair", DisplayOrder = 1, IsActive = true },
                new MaintenanceImageType { Id = 2, Name = "During", Description = "During repair", DisplayOrder = 2, IsActive = true },
                new MaintenanceImageType { Id = 3, Name = "After", Description = "After repair", DisplayOrder = 3, IsActive = true },
                new MaintenanceImageType { Id = 4, Name = "Receipt", Description = "Receipt/Invoice", DisplayOrder = 4, IsActive = true },
                new MaintenanceImageType { Id = 5, Name = "Other", Description = "Other image", DisplayOrder = 5, IsActive = true }
            };

            await context.MaintenanceImageTypes.AddRangeAsync(items);
        }

        private async Task SeedExpenseCategories(ApplicationDbContext context)
        {
            var items = new[]
            {
                new ExpenseCategory { Id = 1, Name = "Materials", Description = "Material costs", DisplayOrder = 1, IsActive = true },
                new ExpenseCategory { Id = 2, Name = "Labor", Description = "Labor costs", DisplayOrder = 2, IsActive = true },
                new ExpenseCategory { Id = 3, Name = "Equipment", Description = "Equipment costs", DisplayOrder = 3, IsActive = true },
                new ExpenseCategory { Id = 4, Name = "Transportation", Description = "Transportation costs", DisplayOrder = 4, IsActive = true },
                new ExpenseCategory { Id = 5, Name = "Other", Description = "Other expenses", DisplayOrder = 5, IsActive = true }
            };

            await context.ExpenseCategories.AddRangeAsync(items);
        }

        private async Task SeedPaymentTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentType { Id = 1, Name = "Rent", Description = "Rent payment", DisplayOrder = 1, IsActive = true },
                new PaymentType { Id = 2, Name = "Deposit", Description = "Security deposit", DisplayOrder = 2, IsActive = true },
                new PaymentType { Id = 3, Name = "Utility Bill", Description = "Utility bill payment", DisplayOrder = 3, IsActive = true },
                new PaymentType { Id = 4, Name = "Maintenance Fee", Description = "Maintenance fee", DisplayOrder = 4, IsActive = true },
                new PaymentType { Id = 5, Name = "Late Fee", Description = "Late payment fee", DisplayOrder = 5, IsActive = true },
                new PaymentType { Id = 6, Name = "Other", Description = "Other payment type", DisplayOrder = 6, IsActive = true }
            };

            await context.PaymentTypes.AddRangeAsync(items);
        }

        private async Task SeedPaymentStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentStatusType { Id = 1, Name = "Pending", Description = "Payment pending", DisplayOrder = 1, IsActive = true },
                new PaymentStatusType { Id = 2, Name = "Paid", Description = "Payment received", DisplayOrder = 2, IsActive = true },
                new PaymentStatusType { Id = 3, Name = "Partially Paid", Description = "Partially paid", DisplayOrder = 3, IsActive = true },
                new PaymentStatusType { Id = 4, Name = "Overdue", Description = "Payment overdue", DisplayOrder = 4, IsActive = true },
                new PaymentStatusType { Id = 5, Name = "Cancelled", Description = "Payment cancelled", DisplayOrder = 5, IsActive = true },
                new PaymentStatusType { Id = 6, Name = "Refunded", Description = "Payment refunded", DisplayOrder = 6, IsActive = true }
            };

            await context.PaymentStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedPaymentMethods(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentMethod { Id = 1, Name = "Bank Transfer", Description = "Bank transfer payment", DisplayOrder = 1, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 0m, IsActive = true },
                new PaymentMethod { Id = 2, Name = "Credit Card", Description = "Credit card payment", DisplayOrder = 2, ProcessingFeePercentage = 2.9m, ProcessingFeeFixed = 0.30m, IsActive = true },
                new PaymentMethod { Id = 3, Name = "Debit Card", Description = "Debit card payment", DisplayOrder = 3, ProcessingFeePercentage = 1.5m, ProcessingFeeFixed = 0.25m, IsActive = true },
                new PaymentMethod { Id = 4, Name = "Cash", Description = "Cash payment", DisplayOrder = 4, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 0m, IsActive = true },
                new PaymentMethod { Id = 5, Name = "Cheque", Description = "Cheque payment", DisplayOrder = 5, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 0m, IsActive = true },
                new PaymentMethod { Id = 6, Name = "Online Payment", Description = "Online payment gateway", DisplayOrder = 6, ProcessingFeePercentage = 2.5m, ProcessingFeeFixed = 0m, IsActive = true },
                new PaymentMethod { Id = 7, Name = "Other", Description = "Other payment method", DisplayOrder = 7, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 0m, IsActive = true }
            };

            await context.PaymentMethods.AddRangeAsync(items);
        }

        private async Task SeedAllocationTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new AllocationType { Id = 1, Name = "Rent", Description = "Rent allocation", DisplayOrder = 1, IsActive = true },
                new AllocationType { Id = 2, Name = "Commission", Description = "Commission allocation", DisplayOrder = 2, IsActive = true },
                new AllocationType { Id = 3, Name = "Management Fee", Description = "Management fee allocation", DisplayOrder = 3, IsActive = true },
                new AllocationType { Id = 4, Name = "Maintenance", Description = "Maintenance allocation", DisplayOrder = 4, IsActive = true },
                new AllocationType { Id = 5, Name = "Other", Description = "Other allocation", DisplayOrder = 5, IsActive = true }
            };

            await context.AllocationTypes.AddRangeAsync(items);
        }

        private async Task SeedBeneficiaryPaymentStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new BeneficiaryPaymentStatusType { Id = 1, Name = "Pending", Description = "Payment pending", DisplayOrder = 1, IsActive = true },
                new BeneficiaryPaymentStatusType { Id = 2, Name = "Processed", Description = "Payment processed", DisplayOrder = 2, IsActive = true },
                new BeneficiaryPaymentStatusType { Id = 3, Name = "Paid", Description = "Payment paid", DisplayOrder = 3, IsActive = true },
                new BeneficiaryPaymentStatusType { Id = 4, Name = "Failed", Description = "Payment failed", DisplayOrder = 4, IsActive = true },
                new BeneficiaryPaymentStatusType { Id = 5, Name = "Cancelled", Description = "Payment cancelled", DisplayOrder = 5, IsActive = true }
            };

            await context.BeneficiaryPaymentStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedPaymentFrequencies(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentFrequency { Id = 1, Name = "Monthly", Description = "Monthly payment", DaysInterval = 30, DisplayOrder = 1, IsActive = true },
                new PaymentFrequency { Id = 2, Name = "Quarterly", Description = "Quarterly payment", DaysInterval = 90, DisplayOrder = 2, IsActive = true },
                new PaymentFrequency { Id = 3, Name = "Semi-Annual", Description = "Semi-annual payment", DaysInterval = 180, DisplayOrder = 3, IsActive = true },
                new PaymentFrequency { Id = 4, Name = "Annual", Description = "Annual payment", DaysInterval = 365, DisplayOrder = 4, IsActive = true },
                new PaymentFrequency { Id = 5, Name = "One Time", Description = "One-time payment", DaysInterval = null, DisplayOrder = 5, IsActive = true }
            };

            await context.PaymentFrequencies.AddRangeAsync(items);
        }

        private async Task SeedPaymentRuleTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentRuleType { Id = 1, Name = "Late Fee", Description = "Late fee rules", DisplayOrder = 1, IsActive = true },
                new PaymentRuleType { Id = 2, Name = "Reminder", Description = "Payment reminder rules", DisplayOrder = 2, IsActive = true },
                new PaymentRuleType { Id = 3, Name = "Allocation", Description = "Payment allocation rules", DisplayOrder = 3, IsActive = true },
                new PaymentRuleType { Id = 4, Name = "General", Description = "General payment rules", DisplayOrder = 4, IsActive = true }
            };

            await context.PaymentRuleTypes.AddRangeAsync(items);
        }

        #endregion

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
                new CompanyStatusType { Id = 1, Name = "Active", Description = "Company is active", DisplayOrder = 1, IsActive = true },
                new CompanyStatusType { Id = 2, Name = "Inactive", Description = "Company is inactive", DisplayOrder = 2, IsActive = true },
                new CompanyStatusType { Id = 3, Name = "Suspended", Description = "Company is suspended", DisplayOrder = 3, IsActive = true },
                new CompanyStatusType { Id = 4, Name = "Trial", Description = "Company is in trial period", DisplayOrder = 4, IsActive = true }
            };

            await context.CompanyStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedSubscriptionPlans(ApplicationDbContext context)
        {
            var items = new[]
            {
                new SubscriptionPlan
                {
                    Id = 1,
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
                    Id = 2,
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
                    Id = 3,
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
                new BranchStatusType { Id = 1, Name = "Active", Description = "Branch is active", DisplayOrder = 1, IsActive = true },
                new BranchStatusType { Id = 2, Name = "Inactive", Description = "Branch is inactive", DisplayOrder = 2, IsActive = true },
                new BranchStatusType { Id = 3, Name = "Temporary Closed", Description = "Branch is temporarily closed", DisplayOrder = 3, IsActive = true }
            };

            await context.BranchStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedUserStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new UserStatusType { Id = 1, Name = "Active", Description = "User is active", DisplayOrder = 1, IsActive = true },
                new UserStatusType { Id = 2, Name = "Inactive", Description = "User is inactive", DisplayOrder = 2, IsActive = true },
                new UserStatusType { Id = 3, Name = "Suspended", Description = "User is suspended", DisplayOrder = 3, IsActive = true },
                new UserStatusType { Id = 4, Name = "Locked", Description = "User account is locked", DisplayOrder = 4, IsActive = true }
            };

            await context.UserStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedTwoFactorMethods(ApplicationDbContext context)
        {
            var items = new[]
            {
                new TwoFactorMethod { Id = 1, Name = "SMS", Description = "SMS-based 2FA", DisplayOrder = 1, IsActive = true },
                new TwoFactorMethod { Id = 2, Name = "Email", Description = "Email-based 2FA", DisplayOrder = 2, IsActive = true },
                new TwoFactorMethod { Id = 3, Name = "Authenticator App", Description = "Authenticator app 2FA", DisplayOrder = 3, IsActive = true }
            };

            await context.TwoFactorMethods.AddRangeAsync(items);
        }

        private async Task SeedPermissionCategories(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PermissionCategory { Id = 1, Name = "Properties", Description = "Property management permissions", Icon = "fa-building", DisplayOrder = 1, IsActive = true },
                new PermissionCategory { Id = 2, Name = "Tenants", Description = "Tenant management permissions", Icon = "fa-users", DisplayOrder = 2, IsActive = true },
                new PermissionCategory { Id = 3, Name = "Payments", Description = "Payment management permissions", Icon = "fa-credit-card", DisplayOrder = 3, IsActive = true },
                new PermissionCategory { Id = 4, Name = "Maintenance", Description = "Maintenance management permissions", Icon = "fa-tools", DisplayOrder = 4, IsActive = true },
                new PermissionCategory { Id = 5, Name = "Reports", Description = "Reporting permissions", Icon = "fa-chart-bar", DisplayOrder = 5, IsActive = true },
                new PermissionCategory { Id = 6, Name = "Administration", Description = "Administration permissions", Icon = "fa-cog", DisplayOrder = 6, IsActive = true }
            };

            await context.PermissionCategories.AddRangeAsync(items);
        }

        private async Task SeedRoleTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new RoleType { Id = 1, Name = "System", Description = "System-level roles", DisplayOrder = 1, IsActive = true },
                new RoleType { Id = 2, Name = "Company", Description = "Company-level roles", DisplayOrder = 2, IsActive = true },
                new RoleType { Id = 3, Name = "Branch", Description = "Branch-level roles", DisplayOrder = 3, IsActive = true }
            };

            await context.RoleTypes.AddRangeAsync(items);
        }

        private async Task SeedNotificationTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new NotificationType { Id = 1, Name = "Payment Due", Description = "Payment due notification", Icon = "fa-dollar-sign", DisplayOrder = 1, IsActive = true },
                new NotificationType { Id = 2, Name = "Payment Received", Description = "Payment received notification", Icon = "fa-check-circle", DisplayOrder = 2, IsActive = true },
                new NotificationType { Id = 3, Name = "Maintenance Request", Description = "Maintenance request notification", Icon = "fa-wrench", DisplayOrder = 3, IsActive = true },
                new NotificationType { Id = 4, Name = "Lease Expiry", Description = "Lease expiry notification", Icon = "fa-calendar-alt", DisplayOrder = 4, IsActive = true },
                new NotificationType { Id = 5, Name = "System Alert", Description = "System alert notification", Icon = "fa-exclamation-triangle", DisplayOrder = 5, IsActive = true }
            };

            await context.NotificationTypes.AddRangeAsync(items);
        }

        private async Task SeedNotificationChannels(ApplicationDbContext context)
        {
            var items = new[]
            {
                new NotificationChannel { Id = 1, Name = "Email", Description = "Email notifications", DisplayOrder = 1, IsActive = true },
                new NotificationChannel { Id = 2, Name = "SMS", Description = "SMS notifications", DisplayOrder = 2, IsActive = true },
                new NotificationChannel { Id = 3, Name = "Push", Description = "Push notifications", DisplayOrder = 3, IsActive = true },
                new NotificationChannel { Id = 4, Name = "In-App", Description = "In-app notifications", DisplayOrder = 4, IsActive = true }
            };

            await context.NotificationChannels.AddRangeAsync(items);
        }

        private async Task SeedThemeTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new ThemeType
                {
                    Id = 1,
                    Name = "Light",
                    Description = "Light theme",
                    CssVariables = "{ \"primary\": \"#007bff\", \"secondary\": \"#6c757d\", \"background\": \"#ffffff\" }",
                    IsDarkTheme = false,
                    DisplayOrder = 1,
                    IsActive = true
                },
                new ThemeType
                {
                    Id = 2,
                    Name = "Dark",
                    Description = "Dark theme",
                    CssVariables = "{ \"primary\": \"#0099ff\", \"secondary\": \"#6c757d\", \"background\": \"#1a1a1a\" }",
                    IsDarkTheme = true,
                    DisplayOrder = 2,
                    IsActive = true
                },
                new ThemeType
                {
                    Id = 3,
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

        #endregion

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

        #endregion
    }
}