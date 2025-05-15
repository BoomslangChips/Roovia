#region Property Management

using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

public interface IProperty
{
    // CRUD Operations
    Task<ResponseModel> CreateProperty(Property property);
    Task<ResponseModel> GetPropertyById(int id, int companyId);
    Task<ResponseModel> UpdateProperty(int id, Property updatedProperty, int companyId);
    Task<ResponseModel> DeleteProperty(int id, int companyId, ApplicationUser user);

    // Listing Methods
    Task<ResponseModel> GetAllProperties(int companyId);
    Task<ResponseModel> GetPropertiesByOwner(int ownerId);
    Task<ResponseModel> GetPropertiesByBranch(int branchId);
    Task<ResponseModel> GetPropertiesWithTenants(int companyId);
    Task<ResponseModel> GetVacantProperties(int companyId);

    // CDN Operations
    Task<ResponseModel> UploadPropertyMainImage(int propertyId, IFormFile file, string userId);
    Task<ResponseModel> GetPropertyDocumentFolders(int propertyId, int companyId);
    Task<ResponseModel> GetPropertyCdnFiles(int propertyId, int companyId, string category = "images");

    // Status Management
    Task<ResponseModel> UpdatePropertyStatus(int propertyId, int statusId, string userId);

    // Statistics
    Task<ResponseModel> GetPropertyStatistics(int companyId);
}

#endregion

#region Property Owner Management

public interface IPropertyOwner
{
    // CRUD Operations
    Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner);
    Task<ResponseModel> GetPropertyOwnerById(int companyId, int id);
    Task<ResponseModel> UpdatePropertyOwner(int id, PropertyOwner updatedOwner);
    Task<ResponseModel> DeletePropertyOwner(int id, ApplicationUser user);

    // Listing Methods
    Task<ResponseModel> GetAllPropertyOwners(int companyId);
    Task<ResponseModel> GetPropertyOwnersByPage(int companyId, int pageNumber, int pageSize);

    // Contact Management
    Task<ResponseModel> AddEmailAddress(int ownerId, Email email);
    Task<ResponseModel> AddContactNumber(int ownerId, ContactNumber contactNumber);

    // Address and Banking
    Task<ResponseModel> UpdateOwnerAddress(int ownerId, Address newAddress, string userId);
    Task<ResponseModel> UpdateOwnerBankAccount(int ownerId, BankAccount bankAccount, string userId);

    // Search Methods
    Task<ResponseModel> GetOwnerByVatNumber(string vatNumber, int companyId);
    Task<ResponseModel> GetOwnerByIdNumber(string idNumber, int companyId);
    Task<ResponseModel> SearchOwners(int companyId, string searchTerm);

    // Statistics and Export
    Task<ResponseModel> GetOwnerStatistics(int companyId);
    Task<ResponseModel> ExportOwnersToExcel(int companyId);
}

#endregion

#region Tenant Management

public interface ITenant
{
    // CRUD Operations
    Task<ResponseModel> CreateTenant(PropertyTenant tenant, int companyId);
    Task<ResponseModel> GetTenantById(int id, int companyId);
    Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant, int companyId);
    Task<ResponseModel> DeleteTenant(int id, int companyId, ApplicationUser user);

    // Listing Methods
    Task<ResponseModel> GetAllTenants(int companyId);
    Task<ResponseModel> GetTenantsByProperty(int propertyId, int companyId);
    Task<ResponseModel> GetCurrentTenant(int propertyId, int companyId);
    Task<ResponseModel> GetTenantsInArrears(int companyId);

    // Contact Management
    Task<ResponseModel> AddEmailAddress(int tenantId, Email email);
    Task<ResponseModel> AddContactNumber(int tenantId, ContactNumber contactNumber);

    // Lease Management
    Task<ResponseModel> UpdateTenantLease(int tenantId, DateTime leaseStartDate, DateTime leaseEndDate, decimal rentAmount, string userId);
    Task<ResponseModel> LinkMoveInInspection(int tenantId, int inspectionId, string userId);

    // Document Management
    Task<ResponseModel> UploadLeaseDocument(int tenantId, IFormFile file, string userId);
    Task<ResponseModel> GetTenantDocuments(int tenantId, int companyId);
    Task<ResponseModel> UploadTenantDocument(int tenantId, IFormFile file, string category, string userId);

    // Statistics
    Task<ResponseModel> GetTenantStatistics(int companyId);
}

#endregion

#region Beneficiary Management

public interface IBeneficiary
{
    // CRUD Operations
    Task<ResponseModel> CreateBeneficiary(PropertyBeneficiary beneficiary, int companyId);
    Task<ResponseModel> GetBeneficiaryById(int id, int companyId);
    Task<ResponseModel> UpdateBeneficiary(int id, PropertyBeneficiary updatedBeneficiary, int companyId);
    Task<ResponseModel> DeleteBeneficiary(int id, int companyId, ApplicationUser user);

    // Listing Methods
    Task<ResponseModel> GetBeneficiariesByProperty(int propertyId, int companyId);
    Task<ResponseModel> GetAllBeneficiaries(int companyId);
    Task<ResponseModel> GetBeneficiariesByType(int companyId, int beneficiaryTypeId);

    // Contact Management
    Task<ResponseModel> AddEmailAddress(int beneficiaryId, Email email);
    Task<ResponseModel> AddContactNumber(int beneficiaryId, ContactNumber contactNumber);

    // Status and Updates
    Task<ResponseModel> UpdateBeneficiaryStatus(int beneficiaryId, int statusId, string userId);
    Task<ResponseModel> UpdateBankAccount(int beneficiaryId, BankAccount bankAccount, string userId);
    Task<ResponseModel> UpdateBeneficiaryAddress(int beneficiaryId, Address newAddress, string userId);

    // Payment Related
    Task<ResponseModel> CalculateBeneficiaryAmounts(int propertyId, decimal paymentAmount);
    Task<ResponseModel> GetBeneficiaryPaymentHistory(int beneficiaryId, int companyId);

    // Search
    Task<ResponseModel> SearchBeneficiaries(int companyId, string searchTerm);
}

#endregion

#region Vendor Management

public interface IVendor
{
    // CRUD Operations
    Task<ResponseModel> CreateVendor(Vendor vendor, int companyId);
    Task<ResponseModel> GetVendorById(int id, int companyId);
    Task<ResponseModel> UpdateVendor(int id, Vendor updatedVendor, int companyId);
    Task<ResponseModel> DeleteVendor(int id, int companyId, ApplicationUser user);

    // Listing Methods
    Task<ResponseModel> GetAllVendors(int companyId);
    Task<ResponseModel> GetActiveVendors(int companyId);
    Task<ResponseModel> GetVendorsBySpecialization(int companyId, string specialization);
    Task<ResponseModel> GetVendorsWithExpiredInsurance(int companyId);

    // Contact Management
    Task<ResponseModel> AddEmailAddress(int vendorId, Email email);
    Task<ResponseModel> AddContactNumber(int vendorId, ContactNumber contactNumber);

    // Vendor Management
    Task<ResponseModel> UpdateVendorRating(int vendorId, decimal rating, string userId);
    Task<ResponseModel> SetPreferredVendor(int vendorId, bool isPreferred, string userId);

    // Statistics
    Task<ResponseModel> GetVendorPerformanceStats(int vendorId, int companyId);
}

#endregion

#region Inspection Management

public interface IInspection
{
    // CRUD Operations
    Task<ResponseModel> CreateInspection(PropertyInspection inspection, int companyId);
    Task<ResponseModel> GetInspectionById(int id, int companyId);
    Task<ResponseModel> UpdateInspection(int id, PropertyInspection updatedInspection, int companyId);
    Task<ResponseModel> DeleteInspection(int id, int companyId, ApplicationUser user);

    // Listing Methods
    Task<ResponseModel> GetInspectionsByProperty(int propertyId, int companyId);
    Task<ResponseModel> GetUpcomingInspections(int companyId, int days = 30);

    // Inspection Items
    Task<ResponseModel> AddInspectionItem(int inspectionId, InspectionItem item);
    Task<ResponseModel> UpdateInspectionItem(int itemId, InspectionItem updatedItem);
    Task<ResponseModel> DeleteInspectionItem(int itemId);

    // Report Generation
    Task<ResponseModel> GenerateInspectionReport(int inspectionId, string userId);

    // Status Management
    Task<ResponseModel> CompleteInspection(int inspectionId, string userId);
}

#endregion

#region Maintenance Management

public interface IMaintenance
{
    // CRUD Operations
    Task<ResponseModel> CreateMaintenanceTicket(MaintenanceTicket ticket, int companyId);
    Task<ResponseModel> GetMaintenanceTicketById(int id, int companyId);
    Task<ResponseModel> UpdateMaintenanceTicket(int id, MaintenanceTicket updatedTicket, int companyId);
    Task<ResponseModel> DeleteMaintenanceTicket(int id, int companyId, ApplicationUser user);

    // Listing Methods
    Task<ResponseModel> GetMaintenanceTicketsByProperty(int propertyId, int companyId);
    Task<ResponseModel> GetOpenTicketsByCompany(int companyId);

    // Ticket Management
    Task<ResponseModel> AssignTicketToVendor(int ticketId, int vendorId, string userId);
    Task<ResponseModel> CompleteMaintenanceTicket(int ticketId, string completionNotes, bool issueResolved, string userId);
    Task<ResponseModel> ApproveMaintenanceTicket(int ticketId, string userId);

    // Comments and Expenses
    Task<ResponseModel> AddComment(int ticketId, MaintenanceComment comment, string userId);
    Task<ResponseModel> AddExpense(int ticketId, MaintenanceExpense expense);

    // Document Management
    Task<ResponseModel> UploadMaintenanceExpenseReceipt(int expenseId, IFormFile file, string userId);
    Task<ResponseModel> GetMaintenanceDocuments(int ticketId, int companyId);
    Task<ResponseModel> UploadMaintenanceImage(int ticketId, IFormFile file, string category, string userId);

    // Statistics
    Task<ResponseModel> GetMaintenanceStatistics(int companyId);
}

#endregion

#region Payment Management

public interface IPayment
{
    // Property Payment Operations
    Task<ResponseModel> CreatePropertyPayment(PropertyPayment payment, int companyId);
    Task<ResponseModel> GetPropertyPaymentById(int id, int companyId);
    Task<ResponseModel> UpdatePropertyPaymentStatus(int paymentId, int statusId, string userId);
    Task<ResponseModel> UploadPaymentReceipt(int paymentId, IFormFile file, string userId);

    // Payment Allocation
    Task<ResponseModel> AllocatePayment(int paymentId);

    // Beneficiary Payments
    Task<ResponseModel> CreateBeneficiaryPayment(BeneficiaryPayment payment);
    Task<ResponseModel> ProcessBeneficiaryPayment(int paymentId, string transactionReference, string userId);
    Task<ResponseModel> UploadBeneficiaryPaymentProof(int beneficiaryPaymentId, IFormFile file, string userId);

    // Payment Schedules
    Task<ResponseModel> CreatePaymentSchedule(PaymentSchedule schedule, int companyId);
    Task<ResponseModel> GenerateScheduledPayments(int companyId);

    // Payment Rules
    Task<ResponseModel> CreatePaymentRule(PaymentRule rule, int companyId);

    // Documents and Statistics
    Task<ResponseModel> GetPaymentDocuments(int paymentId, int companyId);
    Task<ResponseModel> GetPaymentStatistics(int companyId);
}
#endregion