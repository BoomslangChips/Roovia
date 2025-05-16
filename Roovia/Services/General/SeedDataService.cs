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
                // General Mapping Types
                await SeedBankNameTypes(context);
                await SeedContactNumberTypes(context);
                await SeedMediaTypes(context);
                await SeedEntityTypes(context);
                
                // Property Related Mapping Types
                await SeedPropertyTypes(context);
                await SeedPropertyStatusTypes(context);
                await SeedCommissionTypes(context);
                await SeedPropertyImageTypes(context);
                await SeedPropertyOwnerTypes(context);
                await SeedPropertyOwnerStatusTypes(context);
                
                // Document Related Mapping Types
                await SeedDocumentTypes(context);
                await SeedDocumentCategories(context);
                await SeedDocumentAccessLevels(context);
                await SeedDocumentStatuses(context);
                await SeedDocumentRequirementTypes(context);
                
                // Beneficiary and Tenant Related Mapping Types
                await SeedBeneficiaryTypes(context);
                await SeedBeneficiaryStatusTypes(context);
                await SeedTenantTypes(context);
                await SeedTenantStatusTypes(context);
                
                // Inspection Related Mapping Types
                await SeedInspectionTypes(context);
                await SeedInspectionStatusTypes(context);
                await SeedInspectionAreas(context);
                await SeedConditionLevels(context);
                
                // Maintenance Related Mapping Types
                await SeedMaintenanceCategories(context);
                await SeedMaintenancePriorities(context);
                await SeedMaintenanceStatusTypes(context);
                await SeedMaintenanceImageTypes(context);
                await SeedExpenseCategories(context);
                
                // Payment Related Mapping Types
                await SeedPaymentTypes(context);
                await SeedPaymentStatusTypes(context);
                await SeedPaymentMethods(context);
                await SeedAllocationTypes(context);
                await SeedBeneficiaryPaymentStatusTypes(context);
                await SeedPaymentFrequencies(context);
                await SeedPaymentRuleTypes(context);
                
                // Communication Related Mapping Types
                await SeedCommunicationChannels(context);
                await SeedCommunicationDirections(context);
                await SeedNotificationEventTypes(context);
                
                // Note and Reminder Related Mapping Types
                await SeedNoteTypes(context);
                await SeedReminderTypes(context);
                await SeedReminderStatuses(context);
                await SeedRecurrenceFrequencies(context);

                await context.SaveChangesAsync();
                _logger.LogInformation("Business mapping data seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding business mapping data");
                throw;
            }
        }

        #region General Mapping Types

        private async Task SeedBankNameTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new BankNameType { Name = "ABSA Bank", Description = "ABSA Bank South Africa", DefaultBranchCode = "632005", DisplayOrder = 1, IsActive = true },
                new BankNameType { Name = "Standard Bank", Description = "Standard Bank South Africa", DefaultBranchCode = "051001", DisplayOrder = 2, IsActive = true },
                new BankNameType { Name = "FNB", Description = "First National Bank South Africa", DefaultBranchCode = "250655", DisplayOrder = 3, IsActive = true },
                new BankNameType { Name = "Nedbank", Description = "Nedbank South Africa", DefaultBranchCode = "198765", DisplayOrder = 4, IsActive = true },
                new BankNameType { Name = "Capitec", Description = "Capitec Bank South Africa", DefaultBranchCode = "470010", DisplayOrder = 5, IsActive = true },
                new BankNameType { Name = "Investec", Description = "Investec Bank South Africa", DefaultBranchCode = "580105", DisplayOrder = 6, IsActive = true },
                new BankNameType { Name = "African Bank", Description = "African Bank South Africa", DefaultBranchCode = "430000", DisplayOrder = 7, IsActive = true },
                new BankNameType { Name = "Bidvest Bank", Description = "Bidvest Bank South Africa", DefaultBranchCode = "462005", DisplayOrder = 8, IsActive = true },
                new BankNameType { Name = "Discovery Bank", Description = "Discovery Bank South Africa", DefaultBranchCode = "679000", DisplayOrder = 9, IsActive = true },
                new BankNameType { Name = "TymeBank", Description = "TymeBank South Africa", DefaultBranchCode = "678910", DisplayOrder = 10, IsActive = true }
            };

            await context.BankNameTypes.AddRangeAsync(items);
        }

        private async Task SeedContactNumberTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new ContactNumberType { Name = "Mobile", Description = "Mobile phone number", DisplayOrder = 1, IsActive = true },
                new ContactNumberType { Name = "Work", Description = "Work phone number", DisplayOrder = 2, IsActive = true },
                new ContactNumberType { Name = "Home", Description = "Home phone number", DisplayOrder = 3, IsActive = true },
                new ContactNumberType { Name = "WhatsApp", Description = "WhatsApp phone number", DisplayOrder = 4, IsActive = true },
                new ContactNumberType { Name = "Fax", Description = "Fax number", DisplayOrder = 5, IsActive = true },
                new ContactNumberType { Name = "Other", Description = "Other phone number", DisplayOrder = 6, IsActive = true }
            };

            await context.ContactNumberTypes.AddRangeAsync(items);
        }

        private async Task SeedMediaTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new MediaType { Name = "Image", Description = "Image file", DisplayOrder = 1, IsActive = true },
                new MediaType { Name = "Document", Description = "Document file", DisplayOrder = 2, IsActive = true },
                new MediaType { Name = "Video", Description = "Video file", DisplayOrder = 3, IsActive = true },
                new MediaType { Name = "Audio", Description = "Audio file", DisplayOrder = 4, IsActive = true },
                new MediaType { Name = "Other", Description = "Other media type", DisplayOrder = 5, IsActive = true }
            };

            await context.MediaTypes.AddRangeAsync(items);
        }

        private async Task SeedEntityTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new EntityType { Name = "Property", Description = "Property entity", SystemName = "Property", DisplayOrder = 1, IsActive = true },
                new EntityType { Name = "Owner", Description = "Property owner entity", SystemName = "PropertyOwner", DisplayOrder = 2, IsActive = true },
                new EntityType { Name = "Tenant", Description = "Property tenant entity", SystemName = "PropertyTenant", DisplayOrder = 3, IsActive = true },
                new EntityType { Name = "Beneficiary", Description = "Property beneficiary entity", SystemName = "PropertyBeneficiary", DisplayOrder = 4, IsActive = true },
                new EntityType { Name = "Inspection", Description = "Property inspection entity", SystemName = "PropertyInspection", DisplayOrder = 5, IsActive = true },
                new EntityType { Name = "Maintenance", Description = "Maintenance ticket entity", SystemName = "MaintenanceTicket", DisplayOrder = 6, IsActive = true },
                new EntityType { Name = "Payment", Description = "Property payment entity", SystemName = "PropertyPayment", DisplayOrder = 7, IsActive = true },
                new EntityType { Name = "Company", Description = "Company entity", SystemName = "Company", DisplayOrder = 8, IsActive = true },
                new EntityType { Name = "Branch", Description = "Branch entity", SystemName = "Branch", DisplayOrder = 9, IsActive = true },
                new EntityType { Name = "User", Description = "User entity", SystemName = "ApplicationUser", DisplayOrder = 10, IsActive = true },
                new EntityType { Name = "Vendor", Description = "Vendor entity", SystemName = "Vendor", DisplayOrder = 11, IsActive = true }
            };

            await context.EntityTypes.AddRangeAsync(items);
        }

        #endregion

        #region Property Related Mapping Types

        private async Task SeedPropertyTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PropertyType { Name = "House", Description = "Standalone house", DisplayOrder = 1, IsActive = true },
                new PropertyType { Name = "Apartment", Description = "Apartment in multi-unit building", DisplayOrder = 2, IsActive = true },
                new PropertyType { Name = "Townhouse", Description = "Townhouse in complex", DisplayOrder = 3, IsActive = true },
                new PropertyType { Name = "Condo", Description = "Condominium unit", DisplayOrder = 4, IsActive = true },
                new PropertyType { Name = "Duplex", Description = "Duplex unit", DisplayOrder = 5, IsActive = true },
                new PropertyType { Name = "Commercial", Description = "Commercial property", DisplayOrder = 6, IsActive = true },
                new PropertyType { Name = "Retail", Description = "Retail property", DisplayOrder = 7, IsActive = true },
                new PropertyType { Name = "Office", Description = "Office property", DisplayOrder = 8, IsActive = true },
                new PropertyType { Name = "Industrial", Description = "Industrial property", DisplayOrder = 9, IsActive = true },
                new PropertyType { Name = "Warehouse", Description = "Warehouse property", DisplayOrder = 10, IsActive = true },
                new PropertyType { Name = "Residential Land", Description = "Residential land", DisplayOrder = 11, IsActive = true },
                new PropertyType { Name = "Commercial Land", Description = "Commercial land", DisplayOrder = 12, IsActive = true },
                new PropertyType { Name = "Farm", Description = "Farm property", DisplayOrder = 13, IsActive = true },
                new PropertyType { Name = "Vacation Home", Description = "Vacation/holiday home", DisplayOrder = 14, IsActive = true },
                new PropertyType { Name = "Other", Description = "Other property type", DisplayOrder = 15, IsActive = true }
            };

            await context.PropertyTypes.AddRangeAsync(items);
        }

        private async Task SeedPropertyStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PropertyStatusType { Name = "Active", Description = "Property is active", DisplayOrder = 1, IsActive = true },
                new PropertyStatusType { Name = "Inactive", Description = "Property is inactive", DisplayOrder = 2, IsActive = true },
                new PropertyStatusType { Name = "Under Maintenance", Description = "Property is under maintenance", DisplayOrder = 3, IsActive = true },
                new PropertyStatusType { Name = "Vacant", Description = "Property is vacant", DisplayOrder = 4, IsActive = true },
                new PropertyStatusType { Name = "Occupied", Description = "Property is occupied", DisplayOrder = 5, IsActive = true },
                new PropertyStatusType { Name = "For Sale", Description = "Property is for sale", DisplayOrder = 6, IsActive = true },
                new PropertyStatusType { Name = "Sold", Description = "Property is sold", DisplayOrder = 7, IsActive = true },
                new PropertyStatusType { Name = "Being Renovated", Description = "Property is being renovated", DisplayOrder = 8, IsActive = true },
                new PropertyStatusType { Name = "Pending Inspection", Description = "Property pending inspection", DisplayOrder = 9, IsActive = true },
                new PropertyStatusType { Name = "Foreclosed", Description = "Property in foreclosure", DisplayOrder = 10, IsActive = true }
            };

            await context.PropertyStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedCommissionTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new CommissionType { Name = "Percentage", Description = "Commission based on percentage", DisplayOrder = 1, IsActive = true },
                new CommissionType { Name = "Fixed Amount", Description = "Fixed commission amount", DisplayOrder = 2, IsActive = true },
                new CommissionType { Name = "Tiered Percentage", Description = "Tiered percentage based on property value", DisplayOrder = 3, IsActive = true },
                new CommissionType { Name = "Split Commission", Description = "Split commission between parties", DisplayOrder = 4, IsActive = true },
                new CommissionType { Name = "No Commission", Description = "No commission charged", DisplayOrder = 5, IsActive = true }
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
                new PropertyImageType { Name = "Document", Description = "Document image", DisplayOrder = 11, IsActive = true },
                new PropertyImageType { Name = "Aerial", Description = "Aerial/drone image", DisplayOrder = 12, IsActive = true },
                new PropertyImageType { Name = "Virtual Tour", Description = "Virtual tour image", DisplayOrder = 13, IsActive = true },
                new PropertyImageType { Name = "Street View", Description = "Street view image", DisplayOrder = 14, IsActive = true }
            };

            await context.PropertyImageTypes.AddRangeAsync(items);
        }

        private async Task SeedPropertyOwnerTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PropertyOwnerType { Name = "Individual", Description = "Individual owner", DisplayOrder = 1, IsActive = true },
                new PropertyOwnerType { Name = "Company", Description = "Company owner", DisplayOrder = 2, IsActive = true },
                new PropertyOwnerType { Name = "Trust", Description = "Trust owner", DisplayOrder = 3, IsActive = true },
                new PropertyOwnerType { Name = "Partnership", Description = "Partnership owner", DisplayOrder = 4, IsActive = true },
                new PropertyOwnerType { Name = "Government", Description = "Government owner", DisplayOrder = 5, IsActive = true },
                new PropertyOwnerType { Name = "Non-Profit", Description = "Non-profit organization owner", DisplayOrder = 6, IsActive = true },
                new PropertyOwnerType { Name = "Estate", Description = "Estate owner", DisplayOrder = 7, IsActive = true },
                new PropertyOwnerType { Name = "Bank", Description = "Bank-owned property", DisplayOrder = 8, IsActive = true },
                new PropertyOwnerType { Name = "Other", Description = "Other owner type", DisplayOrder = 9, IsActive = true }
            };

            await context.PropertyOwnerTypes.AddRangeAsync(items);
        }

        private async Task SeedPropertyOwnerStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PropertyOwnerStatusType { Name = "Active", Description = "Active property owner", DisplayOrder = 1, IsActive = true },
                new PropertyOwnerStatusType { Name = "Inactive", Description = "Inactive property owner", DisplayOrder = 2, IsActive = true },
                new PropertyOwnerStatusType { Name = "Pending Verification", Description = "Owner pending verification", DisplayOrder = 3, IsActive = true },
                new PropertyOwnerStatusType { Name = "Deceased", Description = "Deceased owner", DisplayOrder = 4, IsActive = true },
                new PropertyOwnerStatusType { Name = "Transferred", Description = "Ownership transferred", DisplayOrder = 5, IsActive = true },
                new PropertyOwnerStatusType { Name = "Disputed", Description = "Disputed ownership", DisplayOrder = 6, IsActive = true },
                new PropertyOwnerStatusType { Name = "Under Legal Review", Description = "Ownership under legal review", DisplayOrder = 7, IsActive = true }
            };

            await context.PropertyOwnerStatusTypes.AddRangeAsync(items);
        }

        #endregion

        #region Document Related Mapping Types

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
                new DocumentType { Name = "Identification", Description = "Identification document", DisplayOrder = 11, IsActive = true },
                new DocumentType { Name = "Bank Statement", Description = "Bank statement document", DisplayOrder = 12, IsActive = true },
                new DocumentType { Name = "Utility Bill", Description = "Utility bill document", DisplayOrder = 13, IsActive = true },
                new DocumentType { Name = "Receipt", Description = "Receipt document", DisplayOrder = 14, IsActive = true },
                new DocumentType { Name = "Invoice", Description = "Invoice document", DisplayOrder = 15, IsActive = true },
                new DocumentType { Name = "Contract", Description = "Contract document", DisplayOrder = 16, IsActive = true },
                new DocumentType { Name = "Addendum", Description = "Addendum document", DisplayOrder = 17, IsActive = true },
                new DocumentType { Name = "Other", Description = "Other document type", DisplayOrder = 18, IsActive = true }
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
                new DocumentCategory { Name = "Tenant", Description = "Tenant-related documents", DisplayOrder = 8, IsActive = true },
                new DocumentCategory { Name = "Owner", Description = "Owner-related documents", DisplayOrder = 9, IsActive = true },
                new DocumentCategory { Name = "Property", Description = "Property-related documents", DisplayOrder = 10, IsActive = true },
                new DocumentCategory { Name = "Tax", Description = "Tax-related documents", DisplayOrder = 11, IsActive = true },
                new DocumentCategory { Name = "Utility", Description = "Utility-related documents", DisplayOrder = 12, IsActive = true },
                new DocumentCategory { Name = "Communication", Description = "Communication records", DisplayOrder = 13, IsActive = true },
                new DocumentCategory { Name = "General", Description = "General documents", DisplayOrder = 14, IsActive = true }
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
                new DocumentAccessLevel { Name = "Confidential", Description = "Confidential access", DisplayOrder = 4, IsActive = true },
                new DocumentAccessLevel { Name = "Private", Description = "Private access (owner only)", DisplayOrder = 5, IsActive = true },
                new DocumentAccessLevel { Name = "Management", Description = "Management access only", DisplayOrder = 6, IsActive = true },
                new DocumentAccessLevel { Name = "Legal", Description = "Legal team access only", DisplayOrder = 7, IsActive = true }
            };

            await context.DocumentAccessLevels.AddRangeAsync(items);
        }

        private async Task SeedDocumentStatuses(ApplicationDbContext context)
        {
            var items = new[]
            {
                new DocumentStatus { Name = "Draft", Description = "Document in draft state", DisplayOrder = 1, IsActive = true },
                new DocumentStatus { Name = "Pending Review", Description = "Document pending review", DisplayOrder = 2, IsActive = true },
                new DocumentStatus { Name = "Approved", Description = "Document approved", DisplayOrder = 3, IsActive = true },
                new DocumentStatus { Name = "Rejected", Description = "Document rejected", DisplayOrder = 4, IsActive = true },
                new DocumentStatus { Name = "Expired", Description = "Document expired", DisplayOrder = 5, IsActive = true },
                new DocumentStatus { Name = "Active", Description = "Document is active", DisplayOrder = 6, IsActive = true },
                new DocumentStatus { Name = "Archived", Description = "Document archived", DisplayOrder = 7, IsActive = true },
                new DocumentStatus { Name = "Requires Update", Description = "Document requires update", DisplayOrder = 8, IsActive = true },
                new DocumentStatus { Name = "Signed", Description = "Document signed", DisplayOrder = 9, IsActive = true },
                new DocumentStatus { Name = "Pending Signature", Description = "Document pending signature", DisplayOrder = 10, IsActive = true }
            };

            await context.DocumentStatuses.AddRangeAsync(items);
        }

        private async Task SeedDocumentRequirementTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new DocumentRequirementType { Name = "Required", Description = "Document is required", DisplayOrder = 1, IsActive = true },
                new DocumentRequirementType { Name = "Optional", Description = "Document is optional", DisplayOrder = 2, IsActive = true },
                new DocumentRequirementType { Name = "Conditional", Description = "Document is conditionally required", DisplayOrder = 3, IsActive = true },
                new DocumentRequirementType { Name = "Recommended", Description = "Document is recommended but not required", DisplayOrder = 4, IsActive = true },
                new DocumentRequirementType { Name = "Regulatory", Description = "Document is required by regulations", DisplayOrder = 5, IsActive = true }
            };

            await context.DocumentRequirementTypes.AddRangeAsync(items);
        }

        #endregion

        #region Beneficiary and Tenant Related Mapping Types

        private async Task SeedBeneficiaryTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new BeneficiaryType { Name = "Owner", Description = "Property owner", DisplayOrder = 1, IsActive = true },
                new BeneficiaryType { Name = "Agent", Description = "Property agent", DisplayOrder = 2, IsActive = true },
                new BeneficiaryType { Name = "Manager", Description = "Property manager", DisplayOrder = 3, IsActive = true },
                new BeneficiaryType { Name = "Broker", Description = "Property broker", DisplayOrder = 4, IsActive = true },
                new BeneficiaryType { Name = "Tax Authority", Description = "Tax authority", DisplayOrder = 5, IsActive = true },
                new BeneficiaryType { Name = "Insurance", Description = "Insurance company", DisplayOrder = 6, IsActive = true },
                new BeneficiaryType { Name = "Bank", Description = "Banking institution", DisplayOrder = 7, IsActive = true },
                new BeneficiaryType { Name = "Maintenance", Description = "Maintenance service", DisplayOrder = 8, IsActive = true },
                new BeneficiaryType { Name = "Utility", Description = "Utility company", DisplayOrder = 9, IsActive = true },
                new BeneficiaryType { Name = "Body Corporate", Description = "Body corporate", DisplayOrder = 10, IsActive = true },
                new BeneficiaryType { Name = "HOA", Description = "Homeowners association", DisplayOrder = 11, IsActive = true },
                new BeneficiaryType { Name = "Other", Description = "Other beneficiary type", DisplayOrder = 12, IsActive = true }
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
                new BeneficiaryStatusType { Name = "Terminated", Description = "Terminated beneficiary", DisplayOrder = 4, IsActive = true },
                new BeneficiaryStatusType { Name = "Pending Verification", Description = "Pending verification", DisplayOrder = 5, IsActive = true },
                new BeneficiaryStatusType { Name = "On Hold", Description = "Payments on hold", DisplayOrder = 6, IsActive = true },
                new BeneficiaryStatusType { Name = "Archived", Description = "Archived beneficiary", DisplayOrder = 7, IsActive = true },
                new BeneficiaryStatusType { Name = "Disputed", Description = "Disputed beneficiary", DisplayOrder = 8, IsActive = true }
            };

            await context.BeneficiaryStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedTenantTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new TenantType { Name = "Individual", Description = "Individual tenant", DisplayOrder = 1, IsActive = true },
                new TenantType { Name = "Company", Description = "Company tenant", DisplayOrder = 2, IsActive = true },
                new TenantType { Name = "Partnership", Description = "Partnership tenant", DisplayOrder = 3, IsActive = true },
                new TenantType { Name = "Non-Profit", Description = "Non-profit organization tenant", DisplayOrder = 4, IsActive = true },
                new TenantType { Name = "Government", Description = "Government tenant", DisplayOrder = 5, IsActive = true },
                new TenantType { Name = "Educational", Description = "Educational institution tenant", DisplayOrder = 6, IsActive = true },
                new TenantType { Name = "Medical", Description = "Medical institution tenant", DisplayOrder = 7, IsActive = true },
                new TenantType { Name = "Mixed Use", Description = "Mixed use tenant", DisplayOrder = 8, IsActive = true },
                new TenantType { Name = "Other", Description = "Other tenant type", DisplayOrder = 9, IsActive = true }
            };

            await context.TenantTypes.AddRangeAsync(items);
        }

        private async Task SeedTenantStatusTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new TenantStatusType { Name = "Pending", Description = "Pending tenant", DisplayOrder = 1, IsActive = true },
                new TenantStatusType { Name = "Active", Description = "Active tenant", DisplayOrder = 2, IsActive = true },
                new TenantStatusType { Name = "Inactive", Description = "Inactive tenant", DisplayOrder = 3, IsActive = true },
                new TenantStatusType { Name = "Evicted", Description = "Evicted tenant", DisplayOrder = 4, IsActive = true },
                new TenantStatusType { Name = "Moved Out", Description = "Moved out tenant", DisplayOrder = 5, IsActive = true },
                new TenantStatusType { Name = "In Arrears", Description = "Tenant in payment arrears", DisplayOrder = 6, IsActive = true },
                new TenantStatusType { Name = "Lease Renewal", Description = "Tenant in lease renewal", DisplayOrder = 7, IsActive = true },
                new TenantStatusType { Name = "Lease Ending", Description = "Tenant with ending lease", DisplayOrder = 8, IsActive = true },
                new TenantStatusType { Name = "Notice Given", Description = "Tenant has given notice", DisplayOrder = 9, IsActive = true },
                new TenantStatusType { Name = "In Dispute", Description = "Tenant in dispute", DisplayOrder = 10, IsActive = true },
                new TenantStatusType { Name = "Legal Action", Description = "Legal action in progress", DisplayOrder = 11, IsActive = true }
            };

            await context.TenantStatusTypes.AddRangeAsync(items);
        }

        #endregion

        #region Inspection Related Mapping Types

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
                new InspectionType { Name = "Emergency Damage", Description = "Emergency damage inspection", DisplayOrder = 7, IsActive = true },
                new InspectionType { Name = "Maintenance Follow-up", Description = "Maintenance follow-up inspection", DisplayOrder = 8, IsActive = true },
                new InspectionType { Name = "Insurance", Description = "Insurance-related inspection", DisplayOrder = 9, IsActive = true },
                new InspectionType { Name = "Compliance", Description = "Compliance inspection", DisplayOrder = 10, IsActive = true },
                new InspectionType { Name = "Safety", Description = "Safety inspection", DisplayOrder = 11, IsActive = true },
                new InspectionType { Name = "Other", Description = "Other inspection type", DisplayOrder = 12, IsActive = true }
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
                new InspectionStatusType { Name = "Requires Review", Description = "Inspection requires review", DisplayOrder = 5, IsActive = true },
                new InspectionStatusType { Name = "Pending Report", Description = "Inspection pending report", DisplayOrder = 6, IsActive = true },
                new InspectionStatusType { Name = "Report Completed", Description = "Inspection report completed", DisplayOrder = 7, IsActive = true },
                new InspectionStatusType { Name = "Rescheduled", Description = "Inspection rescheduled", DisplayOrder = 8, IsActive = true },
                new InspectionStatusType { Name = "Tenant No-Show", Description = "Tenant no-show for inspection", DisplayOrder = 9, IsActive = true },
                new InspectionStatusType { Name = "Inspector No-Show", Description = "Inspector no-show", DisplayOrder = 10, IsActive = true },
                new InspectionStatusType { Name = "Follow-up Required", Description = "Follow-up inspection required", DisplayOrder = 11, IsActive = true }
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
                new InspectionArea { Name = "Hallway", Description = "Hallway", DisplayOrder = 21, IsActive = true },
                new InspectionArea { Name = "Attic", Description = "Attic space", DisplayOrder = 22, IsActive = true },
                new InspectionArea { Name = "Basement", Description = "Basement area", DisplayOrder = 23, IsActive = true },
                new InspectionArea { Name = "Security System", Description = "Security system", DisplayOrder = 24, IsActive = true },
                new InspectionArea { Name = "Fire Safety", Description = "Fire safety systems", DisplayOrder = 25, IsActive = true },
                new InspectionArea { Name = "Pool", Description = "Swimming pool area", DisplayOrder = 26, IsActive = true },
                new InspectionArea { Name = "Patio/Deck", Description = "Patio or deck area", DisplayOrder = 27, IsActive = true },
                new InspectionArea { Name = "Other", Description = "Other areas", DisplayOrder = 28, IsActive = true }
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
                new ConditionLevel { Name = "Critical", Description = "Critical condition", DisplayOrder = 5, ScoreValue = 1, IsActive = true },
                new ConditionLevel { Name = "Not Applicable", Description = "Not applicable", DisplayOrder = 6, ScoreValue = 0, IsActive = true },
                new ConditionLevel { Name = "Not Inspected", Description = "Not inspected", DisplayOrder = 7, ScoreValue = null, IsActive = true },
                new ConditionLevel { Name = "Like New", Description = "Like new condition", DisplayOrder = 8, ScoreValue = 5, IsActive = true },
                new ConditionLevel { Name = "Needs Attention", Description = "Needs attention soon", DisplayOrder = 9, ScoreValue = 3, IsActive = true },
                new ConditionLevel { Name = "Requires Immediate Action", Description = "Requires immediate action", DisplayOrder = 10, ScoreValue = 1, IsActive = true }
            };

            await context.ConditionLevels.AddRangeAsync(items);
        }

        #endregion

        #region Maintenance Related Mapping Types

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
                new MaintenanceCategory { Name = "Roofing", Description = "Roofing issues", DisplayOrder = 10, IsActive = true },
                new MaintenanceCategory { Name = "Flooring", Description = "Flooring issues", DisplayOrder = 11, IsActive = true },
                new MaintenanceCategory { Name = "Windows/Doors", Description = "Window/door issues", DisplayOrder = 12, IsActive = true },
                new MaintenanceCategory { Name = "Pest Control", Description = "Pest control issues", DisplayOrder = 13, IsActive = true },
                new MaintenanceCategory { Name = "Mold/Moisture", Description = "Mold/moisture issues", DisplayOrder = 14, IsActive = true },
                new MaintenanceCategory { Name = "Carpentry", Description = "Carpentry work", DisplayOrder = 15, IsActive = true },
                new MaintenanceCategory { Name = "Safety Equipment", Description = "Safety equipment issues", DisplayOrder = 16, IsActive = true },
                new MaintenanceCategory { Name = "Pool Maintenance", Description = "Pool maintenance", DisplayOrder = 17, IsActive = true },
                new MaintenanceCategory { Name = "Elevator", Description = "Elevator issues", DisplayOrder = 18, IsActive = true },
                new MaintenanceCategory { Name = "Common Area", Description = "Common area maintenance", DisplayOrder = 19, IsActive = true },
                new MaintenanceCategory { Name = "Other", Description = "Other maintenance", DisplayOrder = 20, IsActive = true }
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
                new MaintenancePriority { Name = "Emergency", Description = "Emergency priority", DisplayOrder = 5, ResponseTimeHours = 1, IsActive = true },
                new MaintenancePriority { Name = "Routine", Description = "Routine maintenance", DisplayOrder = 6, ResponseTimeHours = 336, IsActive = true },
                new MaintenancePriority { Name = "Preventative", Description = "Preventative maintenance", DisplayOrder = 7, ResponseTimeHours = 504, IsActive = true },
                new MaintenancePriority { Name = "Deferred", Description = "Deferred maintenance", DisplayOrder = 8, ResponseTimeHours = 672, IsActive = true }
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
                new MaintenanceStatusType { Name = "Requires Approval", Description = "Requires approval", DisplayOrder = 7, IsActive = true },
                new MaintenanceStatusType { Name = "Scheduled", Description = "Work scheduled", DisplayOrder = 8, IsActive = true },
                new MaintenanceStatusType { Name = "Waiting For Parts", Description = "Waiting for parts", DisplayOrder = 9, IsActive = true },
                new MaintenanceStatusType { Name = "Pending Tenant Access", Description = "Pending tenant access", DisplayOrder = 10, IsActive = true },
                new MaintenanceStatusType { Name = "Verification Needed", Description = "Completion verification needed", DisplayOrder = 11, IsActive = true },
                new MaintenanceStatusType { Name = "Follow-up Required", Description = "Follow-up required", DisplayOrder = 12, IsActive = true },
                new MaintenanceStatusType { Name = "Re-opened", Description = "Ticket re-opened", DisplayOrder = 13, IsActive = true }
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
                new MaintenanceImageType { Name = "Issue Detail", Description = "Detailed image of issue", DisplayOrder = 5, IsActive = true },
                new MaintenanceImageType { Name = "Part/Material", Description = "Parts or materials used", DisplayOrder = 6, IsActive = true },
                new MaintenanceImageType { Name = "Area Overview", Description = "Overview of affected area", DisplayOrder = 7, IsActive = true },
                new MaintenanceImageType { Name = "Damage", Description = "Damage documentation", DisplayOrder = 8, IsActive = true },
                new MaintenanceImageType { Name = "Diagnostic", Description = "Diagnostic information", DisplayOrder = 9, IsActive = true },
                new MaintenanceImageType { Name = "Other", Description = "Other image", DisplayOrder = 10, IsActive = true }
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
                new ExpenseCategory { Name = "Permits", Description = "Permit fees", DisplayOrder = 5, IsActive = true },
                new ExpenseCategory { Name = "Disposal", Description = "Waste disposal costs", DisplayOrder = 6, IsActive = true },
                new ExpenseCategory { Name = "Subcontractor", Description = "Subcontractor costs", DisplayOrder = 7, IsActive = true },
                new ExpenseCategory { Name = "Inspection", Description = "Inspection costs", DisplayOrder = 8, IsActive = true },
                new ExpenseCategory { Name = "Emergency Service", Description = "Emergency service premium", DisplayOrder = 9, IsActive = true },
                new ExpenseCategory { Name = "Replacement Parts", Description = "Replacement part costs", DisplayOrder = 10, IsActive = true },
                new ExpenseCategory { Name = "Tools", Description = "Tool costs", DisplayOrder = 11, IsActive = true },
                new ExpenseCategory { Name = "Insurance", Description = "Insurance costs", DisplayOrder = 12, IsActive = true },
                new ExpenseCategory { Name = "Tax", Description = "Taxes", DisplayOrder = 13, IsActive = true },
                new ExpenseCategory { Name = "Other", Description = "Other expenses", DisplayOrder = 14, IsActive = true }
            };

            await context.ExpenseCategories.AddRangeAsync(items);
        }

        #endregion

        #region Payment Related Mapping Types

        private async Task SeedPaymentTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PaymentType { Name = "Rent", Description = "Rent payment", DisplayOrder = 1, IsActive = true },
                new PaymentType { Name = "Deposit", Description = "Security deposit", DisplayOrder = 2, IsActive = true },
                new PaymentType { Name = "Utility Bill", Description = "Utility bill payment", DisplayOrder = 3, IsActive = true },
                new PaymentType { Name = "Maintenance Fee", Description = "Maintenance fee", DisplayOrder = 4, IsActive = true },
                new PaymentType { Name = "Late Fee", Description = "Late payment fee", DisplayOrder = 5, IsActive = true },
                new PaymentType { Name = "Pet Fee", Description = "Pet fee", DisplayOrder = 6, IsActive = true },
                new PaymentType { Name = "Damage Fee", Description = "Property damage fee", DisplayOrder = 7, IsActive = true },
                new PaymentType { Name = "Parking Fee", Description = "Parking fee", DisplayOrder = 8, IsActive = true },
                new PaymentType { Name = "Administration Fee", Description = "Administration fee", DisplayOrder = 9, IsActive = true },
                new PaymentType { Name = "Application Fee", Description = "Application fee", DisplayOrder = 10, IsActive = true },
                new PaymentType { Name = "Early Termination Fee", Description = "Early lease termination fee", DisplayOrder = 11, IsActive = true },
                new PaymentType { Name = "First Month Rent", Description = "First month rent", DisplayOrder = 12, IsActive = true },
                new PaymentType { Name = "Last Month Rent", Description = "Last month rent", DisplayOrder = 13, IsActive = true },
                new PaymentType { Name = "Transfer Fee", Description = "Property transfer fee", DisplayOrder = 14, IsActive = true },
                new PaymentType { Name = "Renewal Fee", Description = "Lease renewal fee", DisplayOrder = 15, IsActive = true },
                new PaymentType { Name = "Insurance Payment", Description = "Insurance payment", DisplayOrder = 16, IsActive = true },
                new PaymentType { Name = "HOA Fee", Description = "HOA/Body corporate fee", DisplayOrder = 17, IsActive = true },
                new PaymentType { Name = "Other", Description = "Other payment type", DisplayOrder = 18, IsActive = true }
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
                new PaymentStatusType { Name = "Refunded", Description = "Payment refunded", DisplayOrder = 6, IsActive = true },
                new PaymentStatusType { Name = "Disputed", Description = "Payment disputed", DisplayOrder = 7, IsActive = true },
                new PaymentStatusType { Name = "Processing", Description = "Payment processing", DisplayOrder = 8, IsActive = true },
                new PaymentStatusType { Name = "Failed", Description = "Payment failed", DisplayOrder = 9, IsActive = true },
                new PaymentStatusType { Name = "Scheduled", Description = "Payment scheduled", DisplayOrder = 10, IsActive = true },
                new PaymentStatusType { Name = "Waived", Description = "Payment waived", DisplayOrder = 11, IsActive = true },
                new PaymentStatusType { Name = "Allocated", Description = "Payment allocated", DisplayOrder = 12, IsActive = true },
                new PaymentStatusType { Name = "Unallocated", Description = "Payment unallocated", DisplayOrder = 13, IsActive = true }
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
                new PaymentMethod { Name = "Direct Debit", Description = "Direct debit payment", DisplayOrder = 7, ProcessingFeePercentage = 0.5m, ProcessingFeeFixed = 0m, IsActive = true },
                new PaymentMethod { Name = "Mobile Payment", Description = "Mobile payment app", DisplayOrder = 8, ProcessingFeePercentage = 1.8m, ProcessingFeeFixed = 0.15m, IsActive = true },
                new PaymentMethod { Name = "PayFast", Description = "PayFast payment", DisplayOrder = 9, ProcessingFeePercentage = 3.5m, ProcessingFeeFixed = 0.50m, IsActive = true },
                new PaymentMethod { Name = "PayPal", Description = "PayPal payment", DisplayOrder = 10, ProcessingFeePercentage = 3.9m, ProcessingFeeFixed = 0.30m, IsActive = true },
                new PaymentMethod { Name = "Money Order", Description = "Money order payment", DisplayOrder = 11, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 1.00m, IsActive = true },
                new PaymentMethod { Name = "SnapScan", Description = "SnapScan payment", DisplayOrder = 12, ProcessingFeePercentage = 3.0m, ProcessingFeeFixed = 0.25m, IsActive = true },
                new PaymentMethod { Name = "Other", Description = "Other payment method", DisplayOrder = 13, ProcessingFeePercentage = 0m, ProcessingFeeFixed = 0m, IsActive = true }
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
                new AllocationType { Name = "Insurance", Description = "Insurance allocation", DisplayOrder = 5, IsActive = true },
                new AllocationType { Name = "Property Tax", Description = "Property tax allocation", DisplayOrder = 6, IsActive = true },
                new AllocationType { Name = "Utilities", Description = "Utilities allocation", DisplayOrder = 7, IsActive = true },
                new AllocationType { Name = "Repairs", Description = "Repairs allocation", DisplayOrder = 8, IsActive = true },
                new AllocationType { Name = "Legal Fees", Description = "Legal fees allocation", DisplayOrder = 9, IsActive = true },
                new AllocationType { Name = "Inspection Fees", Description = "Inspection fees allocation", DisplayOrder = 10, IsActive = true },
                new AllocationType { Name = "Marketing", Description = "Marketing allocation", DisplayOrder = 11, IsActive = true },
                new AllocationType { Name = "Cleaning", Description = "Cleaning allocation", DisplayOrder = 12, IsActive = true },
                new AllocationType { Name = "Security", Description = "Security allocation", DisplayOrder = 13, IsActive = true },
                new AllocationType { Name = "Other", Description = "Other allocation", DisplayOrder = 14, IsActive = true }
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
                new BeneficiaryPaymentStatusType { Name = "Cancelled", Description = "Payment cancelled", DisplayOrder = 5, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "On Hold", Description = "Payment on hold", DisplayOrder = 6, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "Scheduled", Description = "Payment scheduled", DisplayOrder = 7, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "Rejected", Description = "Payment rejected", DisplayOrder = 8, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "Disputed", Description = "Payment disputed", DisplayOrder = 9, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "Partially Paid", Description = "Payment partially paid", DisplayOrder = 10, IsActive = true },
                new BeneficiaryPaymentStatusType { Name = "Refunded", Description = "Payment refunded", DisplayOrder = 11, IsActive = true }
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
                new PaymentFrequency { Name = "One Time", Description = "One-time payment", DaysInterval = null, DisplayOrder = 5, IsActive = true },
                new PaymentFrequency { Name = "Weekly", Description = "Weekly payment", DaysInterval = 7, DisplayOrder = 6, IsActive = true },
                new PaymentFrequency { Name = "Bi-Weekly", Description = "Bi-weekly payment", DaysInterval = 14, DisplayOrder = 7, IsActive = true },
                new PaymentFrequency { Name = "Daily", Description = "Daily payment", DaysInterval = 1, DisplayOrder = 8, IsActive = true },
                new PaymentFrequency { Name = "Custom", Description = "Custom payment schedule", DaysInterval = null, DisplayOrder = 9, IsActive = true }
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
                new PaymentRuleType { Name = "Grace Period", Description = "Payment grace period rules", DisplayOrder = 4, IsActive = true },
                new PaymentRuleType { Name = "Auto-Payment", Description = "Automatic payment rules", DisplayOrder = 5, IsActive = true },
                new PaymentRuleType { Name = "Notification", Description = "Payment notification rules", DisplayOrder = 6, IsActive = true },
                new PaymentRuleType { Name = "Discount", Description = "Early payment discount rules", DisplayOrder = 7, IsActive = true },
                new PaymentRuleType { Name = "Interest", Description = "Interest calculation rules", DisplayOrder = 8, IsActive = true },
                new PaymentRuleType { Name = "Escalation", Description = "Payment escalation rules", DisplayOrder = 9, IsActive = true },
                new PaymentRuleType { Name = "General", Description = "General payment rules", DisplayOrder = 10, IsActive = true }
            };

            await context.PaymentRuleTypes.AddRangeAsync(items);
        }

        #endregion

        #region Communication Related Mapping Types

        private async Task SeedCommunicationChannels(ApplicationDbContext context)
        {
            var items = new[]
            {
                new CommunicationChannel { Name = "Email", Description = "Email communication", DisplayOrder = 1, IsActive = true },
                new CommunicationChannel { Name = "SMS", Description = "SMS communication", DisplayOrder = 2, IsActive = true },
                new CommunicationChannel { Name = "Phone Call", Description = "Phone call communication", DisplayOrder = 3, IsActive = true },
                new CommunicationChannel { Name = "In-Person", Description = "In-person communication", DisplayOrder = 4, IsActive = true },
                new CommunicationChannel { Name = "WhatsApp", Description = "WhatsApp communication", DisplayOrder = 5, IsActive = true },
                new CommunicationChannel { Name = "Letter", Description = "Postal letter communication", DisplayOrder = 6, IsActive = true },
                new CommunicationChannel { Name = "Portal Message", Description = "Portal message communication", DisplayOrder = 7, IsActive = true },
                new CommunicationChannel { Name = "Mobile App", Description = "Mobile app notification", DisplayOrder = 8, IsActive = true },
                new CommunicationChannel { Name = "Video Call", Description = "Video call communication", DisplayOrder = 9, IsActive = true },
                new CommunicationChannel { Name = "Social Media", Description = "Social media communication", DisplayOrder = 10, IsActive = true },
                new CommunicationChannel { Name = "Other", Description = "Other communication channel", DisplayOrder = 11, IsActive = true }
            };

            await context.CommunicationChannels.AddRangeAsync(items);
        }

        private async Task SeedCommunicationDirections(ApplicationDbContext context)
        {
            var items = new[]
            {
                new CommunicationDirection { Name = "Inbound", Description = "Inbound communication", DisplayOrder = 1, IsActive = true },
                new CommunicationDirection { Name = "Outbound", Description = "Outbound communication", DisplayOrder = 2, IsActive = true },
                new CommunicationDirection { Name = "Internal", Description = "Internal communication", DisplayOrder = 3, IsActive = true },
                new CommunicationDirection { Name = "Auto-Generated", Description = "Automatically generated communication", DisplayOrder = 4, IsActive = true }
            };

            await context.CommunicationDirections.AddRangeAsync(items);
        }

        private async Task SeedNotificationEventTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new NotificationEventType { Name = "Payment Due", Description = "Payment due notification", Category = "Payment", SystemName = "payment.due", DisplayOrder = 1, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Payment Received", Description = "Payment received notification", Category = "Payment", SystemName = "payment.received", DisplayOrder = 2, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Payment Overdue", Description = "Payment overdue notification", Category = "Payment", SystemName = "payment.overdue", DisplayOrder = 3, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Lease Expiring", Description = "Lease expiring notification", Category = "Lease", SystemName = "lease.expiring", DisplayOrder = 4, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Lease Renewed", Description = "Lease renewed notification", Category = "Lease", SystemName = "lease.renewed", DisplayOrder = 5, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Inspection Scheduled", Description = "Inspection scheduled notification", Category = "Inspection", SystemName = "inspection.scheduled", DisplayOrder = 6, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Inspection Completed", Description = "Inspection completed notification", Category = "Inspection", SystemName = "inspection.completed", DisplayOrder = 7, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Maintenance Request Created", Description = "Maintenance request created notification", Category = "Maintenance", SystemName = "maintenance.created", DisplayOrder = 8, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Maintenance Request Updated", Description = "Maintenance request updated notification", Category = "Maintenance", SystemName = "maintenance.updated", DisplayOrder = 9, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Maintenance Request Completed", Description = "Maintenance request completed notification", Category = "Maintenance", SystemName = "maintenance.completed", DisplayOrder = 10, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Document Uploaded", Description = "Document uploaded notification", Category = "Document", SystemName = "document.uploaded", DisplayOrder = 11, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Document Expiring", Description = "Document expiring notification", Category = "Document", SystemName = "document.expiring", DisplayOrder = 12, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Property Added", Description = "Property added notification", Category = "Property", SystemName = "property.added", DisplayOrder = 13, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Property Status Changed", Description = "Property status changed notification", Category = "Property", SystemName = "property.status.changed", DisplayOrder = 14, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Tenant Added", Description = "Tenant added notification", Category = "Tenant", SystemName = "tenant.added", DisplayOrder = 15, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Tenant Status Changed", Description = "Tenant status changed notification", Category = "Tenant", SystemName = "tenant.status.changed", DisplayOrder = 16, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Owner Added", Description = "Owner added notification", Category = "Owner", SystemName = "owner.added", DisplayOrder = 17, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Owner Status Changed", Description = "Owner status changed notification", Category = "Owner", SystemName = "owner.status.changed", DisplayOrder = 18, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Beneficiary Payment Processed", Description = "Beneficiary payment processed notification", Category = "Payment", SystemName = "beneficiary.payment.processed", DisplayOrder = 19, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "System Alert", Description = "System alert notification", Category = "System", SystemName = "system.alert", DisplayOrder = 20, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Reminder", Description = "Reminder notification", Category = "Reminder", SystemName = "reminder", DisplayOrder = 21, IsActive = true, IsSystemEvent = true },
                new NotificationEventType { Name = "Custom", Description = "Custom notification event", Category = "Custom", SystemName = "custom", DisplayOrder = 22, IsActive = true, IsSystemEvent = false }
            };

            await context.NotificationEventTypes.AddRangeAsync(items);
        }

        #endregion

        #region Note and Reminder Related Mapping Types

        private async Task SeedNoteTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new NoteType { Name = "General", Description = "General note", DisplayOrder = 1, IsActive = true },
                new NoteType { Name = "Follow-up", Description = "Follow-up note", DisplayOrder = 2, IsActive = true },
                new NoteType { Name = "Communication", Description = "Communication record note", DisplayOrder = 3, IsActive = true },
                new NoteType { Name = "Issue", Description = "Issue note", DisplayOrder = 4, IsActive = true },
                new NoteType { Name = "Resolution", Description = "Resolution note", DisplayOrder = 5, IsActive = true },
                new NoteType { Name = "Payment", Description = "Payment-related note", DisplayOrder = 6, IsActive = true },
                new NoteType { Name = "Maintenance", Description = "Maintenance-related note", DisplayOrder = 7, IsActive = true },
                new NoteType { Name = "Inspection", Description = "Inspection-related note", DisplayOrder = 8, IsActive = true },
                new NoteType { Name = "Tenant", Description = "Tenant-related note", DisplayOrder = 9, IsActive = true },
                new NoteType { Name = "Owner", Description = "Owner-related note", DisplayOrder = 10, IsActive = true },
                new NoteType { Name = "Property", Description = "Property-related note", DisplayOrder = 11, IsActive = true },
                new NoteType { Name = "Legal", Description = "Legal-related note", DisplayOrder = 12, IsActive = true },
                new NoteType { Name = "Financial", Description = "Financial-related note", DisplayOrder = 13, IsActive = true },
                new NoteType { Name = "Private", Description = "Private/confidential note", DisplayOrder = 14, IsActive = true },
                new NoteType { Name = "Other", Description = "Other note type", DisplayOrder = 15, IsActive = true }
            };

            await context.NoteTypes.AddRangeAsync(items);
        }

        private async Task SeedReminderTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new ReminderType { Name = "General", Description = "General reminder", DisplayOrder = 1, IsActive = true },
                new ReminderType { Name = "Payment", Description = "Payment reminder", DisplayOrder = 2, IsActive = true },
                new ReminderType { Name = "Lease", Description = "Lease reminder", DisplayOrder = 3, IsActive = true },
                new ReminderType { Name = "Inspection", Description = "Inspection reminder", DisplayOrder = 4, IsActive = true },
                new ReminderType { Name = "Maintenance", Description = "Maintenance reminder", DisplayOrder = 5, IsActive = true },
                new ReminderType { Name = "Document", Description = "Document reminder", DisplayOrder = 6, IsActive = true },
                new ReminderType { Name = "Follow-up", Description = "Follow-up reminder", DisplayOrder = 7, IsActive = true },
                new ReminderType { Name = "Meeting", Description = "Meeting reminder", DisplayOrder = 8, IsActive = true },
                new ReminderType { Name = "Call", Description = "Call reminder", DisplayOrder = 9, IsActive = true },
                new ReminderType { Name = "Email", Description = "Email reminder", DisplayOrder = 10, IsActive = true },
                new ReminderType { Name = "Task", Description = "Task reminder", DisplayOrder = 11, IsActive = true },
                new ReminderType { Name = "Deadline", Description = "Deadline reminder", DisplayOrder = 12, IsActive = true },
                new ReminderType { Name = "Legal", Description = "Legal reminder", DisplayOrder = 13, IsActive = true },
                new ReminderType { Name = "Financial", Description = "Financial reminder", DisplayOrder = 14, IsActive = true },
                new ReminderType { Name = "Other", Description = "Other reminder type", DisplayOrder = 15, IsActive = true }
            };

            await context.ReminderTypes.AddRangeAsync(items);
        }

        private async Task SeedReminderStatuses(ApplicationDbContext context)
        {
            var items = new[]
            {
                new ReminderStatus { Name = "Pending", Description = "Pending reminder", DisplayOrder = 1, IsActive = true },
                new ReminderStatus { Name = "Completed", Description = "Completed reminder", DisplayOrder = 2, IsActive = true },
                new ReminderStatus { Name = "Snoozed", Description = "Snoozed reminder", DisplayOrder = 3, IsActive = true },
                new ReminderStatus { Name = "Cancelled", Description = "Cancelled reminder", DisplayOrder = 4, IsActive = true },
                new ReminderStatus { Name = "Overdue", Description = "Overdue reminder", DisplayOrder = 5, IsActive = true },
                new ReminderStatus { Name = "In Progress", Description = "In progress reminder", DisplayOrder = 6, IsActive = true },
                new ReminderStatus { Name = "Delegated", Description = "Delegated reminder", DisplayOrder = 7, IsActive = true },
                new ReminderStatus { Name = "Recurring", Description = "Recurring reminder", DisplayOrder = 8, IsActive = true },
                new ReminderStatus { Name = "Scheduled", Description = "Scheduled reminder", DisplayOrder = 9, IsActive = true }
            };

            await context.ReminderStatuses.AddRangeAsync(items);
        }

        private async Task SeedRecurrenceFrequencies(ApplicationDbContext context)
        {
            var items = new[]
            {
                new RecurrenceFrequency { Name = "Daily", Description = "Daily recurrence", DaysMultiplier = 1, DisplayOrder = 1, IsActive = true },
                new RecurrenceFrequency { Name = "Weekly", Description = "Weekly recurrence", DaysMultiplier = 7, DisplayOrder = 2, IsActive = true },
                new RecurrenceFrequency { Name = "Bi-Weekly", Description = "Bi-weekly recurrence", DaysMultiplier = 14, DisplayOrder = 3, IsActive = true },
                new RecurrenceFrequency { Name = "Monthly", Description = "Monthly recurrence", DaysMultiplier = 30, DisplayOrder = 4, IsActive = true },
                new RecurrenceFrequency { Name = "Quarterly", Description = "Quarterly recurrence", DaysMultiplier = 90, DisplayOrder = 5, IsActive = true },
                new RecurrenceFrequency { Name = "Semi-Annual", Description = "Semi-annual recurrence", DaysMultiplier = 182, DisplayOrder = 6, IsActive = true },
                new RecurrenceFrequency { Name = "Annual", Description = "Annual recurrence", DaysMultiplier = 365, DisplayOrder = 7, IsActive = true },
                new RecurrenceFrequency { Name = "Custom", Description = "Custom recurrence", DaysMultiplier = null, DisplayOrder = 8, IsActive = true }
            };

            await context.RecurrenceFrequencies.AddRangeAsync(items);
        }

        #endregion

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
                new CompanyStatusType { Name = "Trial", Description = "Company is in trial period", DisplayOrder = 4, IsActive = true },
                new CompanyStatusType { Name = "Pending Approval", Description = "Company pending approval", DisplayOrder = 5, IsActive = true },
                new CompanyStatusType { Name = "Expired", Description = "Company subscription expired", DisplayOrder = 6, IsActive = true },
                new CompanyStatusType { Name = "Archived", Description = "Company archived", DisplayOrder = 7, IsActive = true }
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
                },
                new SubscriptionPlan
                {
                    Name = "Custom",
                    Description = "Custom plan with tailored features",
                    Price = 0m, // Price to be determined
                    BillingCycleDays = 30,
                    MaxUsers = null, // No limit
                    MaxProperties = null, // No limit
                    MaxBranches = null, // No limit
                    HasTrialPeriod = false,
                    TrialPeriodDays = null,
                    DisplayOrder = 4,
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
                new BranchStatusType { Name = "Temporary Closed", Description = "Branch is temporarily closed", DisplayOrder = 3, IsActive = true },
                new BranchStatusType { Name = "Under Construction", Description = "Branch is under construction", DisplayOrder = 4, IsActive = true },
                new BranchStatusType { Name = "Opening Soon", Description = "Branch is opening soon", DisplayOrder = 5, IsActive = true },
                new BranchStatusType { Name = "Permanently Closed", Description = "Branch is permanently closed", DisplayOrder = 6, IsActive = true },
                new BranchStatusType { Name = "Relocated", Description = "Branch has been relocated", DisplayOrder = 7, IsActive = true }
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
                new UserStatusType { Name = "Locked", Description = "User account is locked", DisplayOrder = 4, IsActive = true },
                new UserStatusType { Name = "Pending Verification", Description = "User pending verification", DisplayOrder = 5, IsActive = true },
                new UserStatusType { Name = "Pending Approval", Description = "User pending approval", DisplayOrder = 6, IsActive = true },
                new UserStatusType { Name = "Password Reset", Description = "User in password reset state", DisplayOrder = 7, IsActive = true },
                new UserStatusType { Name = "Archived", Description = "User archived", DisplayOrder = 8, IsActive = true },
                new UserStatusType { Name = "On Leave", Description = "User on leave", DisplayOrder = 9, IsActive = true }
            };

            await context.UserStatusTypes.AddRangeAsync(items);
        }

        private async Task SeedTwoFactorMethods(ApplicationDbContext context)
        {
            var items = new[]
            {
                new TwoFactorMethod { Name = "SMS", Description = "SMS-based 2FA", DisplayOrder = 1, IsActive = true },
                new TwoFactorMethod { Name = "Email", Description = "Email-based 2FA", DisplayOrder = 2, IsActive = true },
                new TwoFactorMethod { Name = "Authenticator App", Description = "Authenticator app 2FA", DisplayOrder = 3, IsActive = true },
                new TwoFactorMethod { Name = "Phone Call", Description = "Phone call 2FA", DisplayOrder = 4, IsActive = true },
                new TwoFactorMethod { Name = "Hardware Token", Description = "Hardware token 2FA", DisplayOrder = 5, IsActive = true },
                new TwoFactorMethod { Name = "Backup Codes", Description = "Backup codes 2FA", DisplayOrder = 6, IsActive = true }
            };

            await context.TwoFactorMethods.AddRangeAsync(items);
        }

        private async Task SeedPermissionCategories(ApplicationDbContext context)
        {
            var items = new[]
            {
                new PermissionCategory { Name = "Properties", Description = "Property management permissions", Icon = "fa-light fa-building", DisplayOrder = 1, IsActive = true },
                new PermissionCategory { Name = "Tenants", Description = "Tenant management permissions", Icon = "fa-light fa-users", DisplayOrder = 2, IsActive = true },
                new PermissionCategory { Name = "Payments", Description = "Payment management permissions", Icon = "fa-light fa-credit-card", DisplayOrder = 3, IsActive = true },
                new PermissionCategory { Name = "Maintenance", Description = "Maintenance management permissions", Icon = "fa-light fa-tools", DisplayOrder = 4, IsActive = true },
                new PermissionCategory { Name = "Reports", Description = "Reporting permissions", Icon = "fa-light fa-chart-bar", DisplayOrder = 5, IsActive = true },
                new PermissionCategory { Name = "Administration", Description = "Administration permissions", Icon = "fa-light fa-cog", DisplayOrder = 6, IsActive = true },
                new PermissionCategory { Name = "Inspections", Description = "Inspection permissions", Icon = "fa-light fa-clipboard-check", DisplayOrder = 7, IsActive = true },
                new PermissionCategory { Name = "Documents", Description = "Document management permissions", Icon = "fa-light fa-file-alt", DisplayOrder = 8, IsActive = true },
                new PermissionCategory { Name = "Communications", Description = "Communication permissions", Icon = "fa-light fa-comments", DisplayOrder = 9, IsActive = true },
                new PermissionCategory { Name = "Owners", Description = "Owner management permissions", Icon = "fa-light fa-user-tie", DisplayOrder = 10, IsActive = true },
                new PermissionCategory { Name = "Beneficiaries", Description = "Beneficiary management permissions", Icon = "fa-light fa-hands-holding", DisplayOrder = 11, IsActive = true },
                new PermissionCategory { Name = "Vendors", Description = "Vendor management permissions", Icon = "fa-light fa-truck", DisplayOrder = 12, IsActive = true },
                new PermissionCategory { Name = "Settings", Description = "System settings permissions", Icon = "fa-light fa-sliders-h", DisplayOrder = 13, IsActive = true }
            };

            await context.PermissionCategories.AddRangeAsync(items);
        }

        private async Task SeedRoleTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new RoleType { Name = "System", Description = "System-level roles", DisplayOrder = 1, IsActive = true },
                new RoleType { Name = "Company", Description = "Company-level roles", DisplayOrder = 2, IsActive = true },
                new RoleType { Name = "Branch", Description = "Branch-level roles", DisplayOrder = 3, IsActive = true },
                new RoleType { Name = "Department", Description = "Department-level roles", DisplayOrder = 4, IsActive = true },
                new RoleType { Name = "Custom", Description = "Custom roles", DisplayOrder = 5, IsActive = true },
                new RoleType { Name = "Temporary", Description = "Temporary roles", DisplayOrder = 6, IsActive = true },
                new RoleType { Name = "Project", Description = "Project-specific roles", DisplayOrder = 7, IsActive = true }
            };

            await context.RoleTypes.AddRangeAsync(items);
        }

        private async Task SeedNotificationTypes(ApplicationDbContext context)
        {
            var items = new[]
            {
                new NotificationType { Name = "Payment Due", Description = "Payment due notification", Icon = "fa-light fa-dollar-sign", DisplayOrder = 1, IsActive = true },
                new NotificationType { Name = "Payment Received", Description = "Payment received notification", Icon = "fa-light fa-check-circle", DisplayOrder = 2, IsActive = true },
                new NotificationType { Name = "Maintenance Request", Description = "Maintenance request notification", Icon = "fa-light fa-wrench", DisplayOrder = 3, IsActive = true },
                new NotificationType { Name = "Lease Expiry", Description = "Lease expiry notification", Icon = "fa-light fa-calendar-alt", DisplayOrder = 4, IsActive = true },
                new NotificationType { Name = "System Alert", Description = "System alert notification", Icon = "fa-light fa-exclamation-triangle", DisplayOrder = 5, IsActive = true },
                new NotificationType { Name = "Document Upload", Description = "Document upload notification", Icon = "fa-light fa-file-upload", DisplayOrder = 6, IsActive = true },
                new NotificationType { Name = "Inspection", Description = "Inspection notification", Icon = "fa-light fa-clipboard-check", DisplayOrder = 7, IsActive = true },
                new NotificationType { Name = "New User", Description = "New user notification", Icon = "fa-light fa-user-plus", DisplayOrder = 8, IsActive = true },
                new NotificationType { Name = "Reminder", Description = "Reminder notification", Icon = "fa-light fa-bell", DisplayOrder = 9, IsActive = true },
                new NotificationType { Name = "Task", Description = "Task notification", Icon = "fa-light fa-tasks", DisplayOrder = 10, IsActive = true },
                new NotificationType { Name = "Message", Description = "Message notification", Icon = "fa-light fa-envelope", DisplayOrder = 11, IsActive = true },
                new NotificationType { Name = "Update", Description = "Update notification", Icon = "fa-light fa-sync", DisplayOrder = 12, IsActive = true },
                new NotificationType { Name = "Information", Description = "Information notification", Icon = "fa-light fa-info-circle", DisplayOrder = 13, IsActive = true },
                new NotificationType { Name = "Warning", Description = "Warning notification", Icon = "fa-light fa-exclamation-circle", DisplayOrder = 14, IsActive = true },
                new NotificationType { Name = "Success", Description = "Success notification", Icon = "fa-light fa-check-circle", DisplayOrder = 15, IsActive = true }
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
                new NotificationChannel { Name = "In-App", Description = "In-app notifications", DisplayOrder = 4, IsActive = true },
                new NotificationChannel { Name = "WhatsApp", Description = "WhatsApp notifications", DisplayOrder = 5, IsActive = true },
                new NotificationChannel { Name = "Web", Description = "Web notifications", DisplayOrder = 6, IsActive = true },
                new NotificationChannel { Name = "Mobile App", Description = "Mobile app notifications", DisplayOrder = 7, IsActive = true },
                new NotificationChannel { Name = "Portal", Description = "Portal notifications", DisplayOrder = 8, IsActive = true },
                new NotificationChannel { Name = "Voice Call", Description = "Voice call notifications", DisplayOrder = 9, IsActive = true }
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
                    CssVariables = "{ \"--color-primary\": \"#007bff\", \"--color-secondary\": \"#6c757d\", \"--color-background\": \"#ffffff\", \"--color-text\": \"#212529\", \"--color-border\": \"#dee2e6\", \"--color-success\": \"#28a745\", \"--color-danger\": \"#dc3545\", \"--color-warning\": \"#ffc107\", \"--color-info\": \"#17a2b8\" }",
                    IsDarkTheme = false,
                    DisplayOrder = 1,
                    IsActive = true
                },
                new ThemeType
                {
                    Name = "Dark",
                    Description = "Dark theme",
                    CssVariables = "{ \"--color-primary\": \"#0099ff\", \"--color-secondary\": \"#6c757d\", \"--color-background\": \"#1a1a1a\", \"--color-text\": \"#f8f9fa\", \"--color-border\": \"#343a40\", \"--color-success\": \"#28a745\", \"--color-danger\": \"#dc3545\", \"--color-warning\": \"#ffc107\", \"--color-info\": \"#17a2b8\" }",
                    IsDarkTheme = true,
                    DisplayOrder = 2,
                    IsActive = true
                },
                new ThemeType
                {
                    Name = "Blue",
                    Description = "Blue theme",
                    CssVariables = "{ \"--color-primary\": \"#004ba0\", \"--color-secondary\": \"#1976d2\", \"--color-background\": \"#f5f5f5\", \"--color-text\": \"#212529\", \"--color-border\": \"#bbdefb\", \"--color-success\": \"#28a745\", \"--color-danger\": \"#dc3545\", \"--color-warning\": \"#ffc107\", \"--color-info\": \"#17a2b8\" }",
                    IsDarkTheme = false,
                    DisplayOrder = 3,
                    IsActive = true
                },
                new ThemeType
                {
                    Name = "Green",
                    Description = "Green theme",
                    CssVariables = "{ \"--color-primary\": \"#2e7d32\", \"--color-secondary\": \"#4caf50\", \"--color-background\": \"#f5f5f5\", \"--color-text\": \"#212529\", \"--color-border\": \"#c8e6c9\", \"--color-success\": \"#28a745\", \"--color-danger\": \"#dc3545\", \"--color-warning\": \"#ffc107\", \"--color-info\": \"#17a2b8\" }",
                    IsDarkTheme = false,
                    DisplayOrder = 4,
                    IsActive = true
                },
                new ThemeType
                {
                    Name = "Purple",
                    Description = "Purple theme",
                    CssVariables = "{ \"--color-primary\": \"#6a1b9a\", \"--color-secondary\": \"#9c27b0\", \"--color-background\": \"#f5f5f5\", \"--color-text\": \"#212529\", \"--color-border\": \"#e1bee7\", \"--color-success\": \"#28a745\", \"--color-danger\": \"#dc3545\", \"--color-warning\": \"#ffc107\", \"--color-info\": \"#17a2b8\" }",
                    IsDarkTheme = false,
                    DisplayOrder = 5,
                    IsActive = true
                },
                new ThemeType
                {
                    Name = "High Contrast",
                    Description = "High contrast theme for accessibility",
                    CssVariables = "{ \"--color-primary\": \"#000000\", \"--color-secondary\": \"#ffffff\", \"--color-background\": \"#ffffff\", \"--color-text\": \"#000000\", \"--color-border\": \"#000000\", \"--color-success\": \"#006400\", \"--color-danger\": \"#8b0000\", \"--color-warning\": \"#ff8c00\", \"--color-info\": \"#0000cd\" }",
                    IsDarkTheme = false,
                    DisplayOrder = 6,
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
                    AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.mp4,.mp3,.zip,.svg",
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
                    new CdnCategory { Name = "contracts", DisplayName = "Contracts", Description = "Contract documents", AllowedFileTypes = ".pdf,.doc,.docx", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "reports", DisplayName = "Reports", Description = "Generated reports", AllowedFileTypes = ".pdf,.xls,.xlsx,.csv", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "tenant", DisplayName = "Tenant", Description = "Tenant-related documents", AllowedFileTypes = ".pdf,.jpg,.jpeg,.png,.doc,.docx", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "owner", DisplayName = "Owner", Description = "Owner-related documents", AllowedFileTypes = ".pdf,.jpg,.jpeg,.png,.doc,.docx", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                    new CdnCategory { Name = "exports", DisplayName = "Exports", Description = "Exported data files", AllowedFileTypes = ".csv,.xls,.xlsx,.pdf", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
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