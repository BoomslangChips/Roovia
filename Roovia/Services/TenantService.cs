using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Roovia.Services
{
    /// <summary>
    /// Service for managing tenant-related operations
    /// </summary>
    public class TenantService : ITenant
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<TenantService> _logger;
        private readonly ICdnService _cdnService;

        public TenantService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<TenantService> logger,
            ICdnService cdnService)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cdnService = cdnService ?? throw new ArgumentNullException(nameof(cdnService));
        }

        /// <summary>
        /// Creates a new tenant for a property
        /// </summary>
        public async Task<ResponseModel> CreateTenant(PropertyTenant tenant, int companyId)
        {
            ResponseModel response = new();

            try
            {
                // Validate required fields
                if (tenant.TenantTypeId == 1) // Individual
                {
                    if (string.IsNullOrEmpty(tenant.FirstName) || string.IsNullOrEmpty(tenant.LastName))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "First name and last name are required for individual tenants.";
                        return response;
                    }
                }
                else // Company/Organization
                {
                    if (string.IsNullOrEmpty(tenant.CompanyName))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Company name is required for organization tenants.";
                        return response;
                    }
                }

                // Validate lease dates
                if (tenant.LeaseEndDate <= tenant.LeaseStartDate)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Lease end date must be after lease start date.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Verify the property exists and belongs to the company
                    var property = await context.Properties
                        .FirstOrDefaultAsync(p => p.Id == tenant.PropertyId && p.CompanyId == companyId && !p.IsRemoved);

                    if (property == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found or does not belong to the company.";
                        return response;
                    }

                    // Validate address if provided
                    if (tenant.Address != null)
                    {
                        var addressValidator = new AddressValidator();
                        var validationResult = addressValidator.Validate(tenant.Address);
                        if (!validationResult.IsValid)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                            return response;
                        }
                    }

                    // Validate bank account if provided
                    if (tenant.BankAccount != null)
                    {
                        var bankValidator = new BankAccountValidator();
                        var validationResult = bankValidator.Validate(tenant.BankAccount);
                        if (!validationResult.IsValid)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                            return response;
                        }
                    }

                    // Set audit fields
                    tenant.CompanyId = companyId;
                    tenant.CreatedOn = DateTime.Now;

                    // Add the tenant
                    await context.PropertyTenants.AddAsync(tenant);

                    // Save to get tenant ID before updating property
                    await context.SaveChangesAsync();

                    // Update property to indicate it has a tenant
                    property.HasTenant = true;
                    property.CurrentTenantId = tenant.Id;
                    property.CurrentLeaseStartDate = tenant.LeaseStartDate;
                    property.LeaseEndDate = tenant.LeaseEndDate;
                    property.UpdatedDate = DateTime.Now;
                    property.UpdatedBy = tenant.CreatedBy;

                    await context.SaveChangesAsync();

                    // Create CDN folder structure for tenant
                    try
                    {
                        var cdnFolderPath = $"company-{companyId}/tenants/{tenant.Id}";
                        await _cdnService.CreateFolderAsync("tenants", cdnFolderPath, "documents");
                        await _cdnService.CreateFolderAsync("tenants", cdnFolderPath, "receipts");
                    }
                    catch (Exception cdnEx)
                    {
                        // Log but continue with transaction
                        _logger.LogWarning(cdnEx, "Error creating CDN folders for tenant {TenantId}. Continuing with creation.", tenant.Id);
                    }

                    await transaction.CommitAsync();

                    // Reload with related data
                    var createdTenant = await context.PropertyTenants
                        .Include(t => t.EmailAddresses)
                        .Include(t => t.ContactNumbers)
                        .Include(t => t.Property)
                        .Include(t => t.Status)
                        .Include(t => t.TenantType)
                        .FirstOrDefaultAsync(t => t.Id == tenant.Id);

                    response.Response = createdTenant;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Tenant created successfully.";

                    _logger.LogInformation("Tenant created with ID: {TenantId} for property {PropertyId}", tenant.Id, tenant.PropertyId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tenant");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the tenant: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Uploads a lease document for a tenant
        /// </summary>
        public async Task<ResponseModel> UploadLeaseDocument(int tenantId, IFormFile file, string userId)
        {
            ResponseModel response = new();

            try
            {
                if (file == null || file.Length == 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "No file was uploaded.";
                    return response;
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "File type not allowed. Please upload a PDF, DOC, DOCX, JPG, JPEG or PNG file.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var tenant = await context.PropertyTenants
                        .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsRemoved);

                    if (tenant == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found.";
                        return response;
                    }

                    // Delete old lease document if exists
                    if (tenant.LeaseDocumentId.HasValue)
                    {
                        var oldDocument = await context.CdnFileMetadata
                            .FirstOrDefaultAsync(f => f.Id == tenant.LeaseDocumentId.Value);

                        if (oldDocument != null)
                        {
                            await _cdnService.DeleteFileAsync(oldDocument.Url);
                        }
                    }

                    // Upload new lease document with base64 backup
                    var cdnPath = $"company-{tenant.CompanyId}/tenants/{tenant.Id}/documents";
                    string cdnUrl;

                    using (var stream = file.OpenReadStream())
                    {
                        cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                            stream,
                            file.FileName,
                            file.ContentType,
                            "tenants",
                            cdnPath
                        );
                    }

                    // Get the file metadata
                    var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);
                    if (fileMetadata != null)
                    {
                        tenant.LeaseDocumentId = fileMetadata.Id;
                        tenant.UpdatedDate = DateTime.Now;
                        tenant.UpdatedBy = userId;

                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        response.Response = new { DocumentUrl = cdnUrl, FileId = fileMetadata.Id };
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Lease document uploaded successfully.";
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Failed to save document metadata.";
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading lease document");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the document: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets a tenant by ID
        /// </summary>
        public async Task<ResponseModel> GetTenantById(int id, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenant = await context.PropertyTenants
                    .Include(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Include(t => t.Property)
                        .ThenInclude(p => p.Owner)
                    .Include(t => t.Status)
                    .Include(t => t.TenantType)
                    .Include(t => t.LeaseDocument)
                    .Include(t => t.Payments.OrderByDescending(p => p.DueDate).Take(5))
                        .ThenInclude(p => p.Status)
                    .Include(t => t.MaintenanceRequests.OrderByDescending(m => m.CreatedOn).Take(5))
                        .ThenInclude(m => m.Status)
                    .Include(t => t.PaymentSchedules.Where(ps => ps.IsActive))
                    .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsRemoved)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (tenant != null)
                {
                    response.Response = tenant;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Tenant retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Tenant not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant {TenantId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the tenant: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Updates an existing tenant
        /// </summary>
        public async Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant, int companyId)
        {
            ResponseModel response = new();

            try
            {
                // Validate required fields
                if (updatedTenant.TenantTypeId == 1) // Individual
                {
                    if (string.IsNullOrEmpty(updatedTenant.FirstName) || string.IsNullOrEmpty(updatedTenant.LastName))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "First name and last name are required for individual tenants.";
                        return response;
                    }
                }
                else // Company/Organization
                {
                    if (string.IsNullOrEmpty(updatedTenant.CompanyName))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Company name is required for organization tenants.";
                        return response;
                    }
                }

                // Validate lease dates
                if (updatedTenant.LeaseEndDate <= updatedTenant.LeaseStartDate)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Lease end date must be after lease start date.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var tenant = await context.PropertyTenants
                        .Include(t => t.Property)
                        .Include(t => t.EmailAddresses)
                        .Include(t => t.ContactNumbers)
                        .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId && !t.IsRemoved);

                    if (tenant == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found.";
                        return response;
                    }

                    // Validate address if provided
                    if (updatedTenant.Address != null)
                    {
                        var addressValidator = new AddressValidator();
                        var validationResult = addressValidator.Validate(updatedTenant.Address);
                        if (!validationResult.IsValid)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                            return response;
                        }
                    }

                    // Validate bank account if provided
                    if (updatedTenant.BankAccount != null)
                    {
                        var bankValidator = new BankAccountValidator();
                        var validationResult = bankValidator.Validate(updatedTenant.BankAccount);
                        if (!validationResult.IsValid)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                            return response;
                        }
                    }

                    // Record if the status is changing
                    bool statusChanged = tenant.StatusId != updatedTenant.StatusId;
                    bool wasActive = tenant.StatusId == 1; // Assuming 1 is Active status
                    bool isNowActive = updatedTenant.StatusId == 1;

                    // Record lease date changes
                    bool leaseDatesChanged = tenant.LeaseStartDate != updatedTenant.LeaseStartDate ||
                                            tenant.LeaseEndDate != updatedTenant.LeaseEndDate;

                    // Update tenant fields
                    tenant.FirstName = updatedTenant.FirstName;
                    tenant.LastName = updatedTenant.LastName;
                    tenant.CompanyName = updatedTenant.CompanyName;
                    tenant.TenantTypeId = updatedTenant.TenantTypeId;
                    tenant.IdNumber = updatedTenant.IdNumber;
                    tenant.RegistrationNumber = updatedTenant.RegistrationNumber;
                    tenant.VatNumber = updatedTenant.VatNumber;
                    tenant.ContactPerson = updatedTenant.ContactPerson;
                    tenant.LeaseStartDate = updatedTenant.LeaseStartDate;
                    tenant.LeaseEndDate = updatedTenant.LeaseEndDate;
                    tenant.RentAmount = updatedTenant.RentAmount;
                    tenant.DepositAmount = updatedTenant.DepositAmount;
                    tenant.DebitDayOfMonth = updatedTenant.DebitDayOfMonth;
                    tenant.StatusId = updatedTenant.StatusId;
                    tenant.Balance = updatedTenant.Balance;
                    tenant.DepositBalance = updatedTenant.DepositBalance;
                    tenant.BankAccount = updatedTenant.BankAccount;
                    tenant.Address = updatedTenant.Address;
                    tenant.EmergencyContactName = updatedTenant.EmergencyContactName;
                    tenant.EmergencyContactPhone = updatedTenant.EmergencyContactPhone;
                    tenant.EmergencyContactRelationship = updatedTenant.EmergencyContactRelationship;
                    tenant.CustomerRefTenant = updatedTenant.CustomerRefTenant;
                    tenant.CustomerRefProperty = updatedTenant.CustomerRefProperty;
                    tenant.ResponsibleAgent = updatedTenant.ResponsibleAgent;
                    tenant.ResponsibleUser = updatedTenant.ResponsibleUser;
                    tenant.Tags = updatedTenant.Tags;
                    tenant.MoveInDate = updatedTenant.MoveInDate;
                    tenant.MoveOutDate = updatedTenant.MoveOutDate;
                    tenant.MoveInInspectionCompleted = updatedTenant.MoveInInspectionCompleted;
                    tenant.MoveInInspectionId = updatedTenant.MoveInInspectionId;
                    tenant.UpdatedDate = DateTime.Now;
                    tenant.UpdatedBy = updatedTenant.UpdatedBy;

                    // Update property if this is the current tenant
                    if (tenant.Property != null && tenant.Property.CurrentTenantId == tenant.Id)
                    {
                        if (leaseDatesChanged)
                        {
                            tenant.Property.CurrentLeaseStartDate = tenant.LeaseStartDate;
                            tenant.Property.LeaseEndDate = tenant.LeaseEndDate;
                        }

                        if (tenant.RentAmount != updatedTenant.RentAmount)
                        {
                            tenant.Property.RentalAmount = tenant.RentAmount;
                        }

                        // Update property tenant status
                        if (statusChanged)
                        {
                            if (!isNowActive && wasActive)
                            {
                                // Tenant is no longer active, update property
                                tenant.Property.HasTenant = false;
                                tenant.Property.CurrentTenantId = null;
                            }
                            else if (isNowActive && !wasActive)
                            {
                                // Tenant is now active, update property
                                tenant.Property.HasTenant = true;
                                tenant.Property.CurrentTenantId = tenant.Id;
                            }
                        }

                        tenant.Property.UpdatedDate = DateTime.Now;
                        tenant.Property.UpdatedBy = updatedTenant.UpdatedBy;
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Reload with related data
                    var updatedResult = await context.PropertyTenants
                        .Include(t => t.EmailAddresses.Where(e => e.IsActive))
                        .Include(t => t.ContactNumbers.Where(c => c.IsActive))
                        .Include(t => t.Property)
                        .Include(t => t.Status)
                        .Include(t => t.TenantType)
                        .Include(t => t.LeaseDocument)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    response.Response = updatedResult;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Tenant updated successfully.";

                    _logger.LogInformation("Tenant updated: {TenantId}", id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant {TenantId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the tenant: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Deletes a tenant (soft delete)
        /// </summary>
        public async Task<ResponseModel> DeleteTenant(int id, int companyId, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var tenant = await context.PropertyTenants
                        .Include(t => t.Property)
                        .Include(t => t.EmailAddresses)
                        .Include(t => t.ContactNumbers)
                        .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId && !t.IsRemoved);

                    if (tenant == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found.";
                        return response;
                    }

                    // Soft delete
                    tenant.IsRemoved = true;
                    tenant.RemovedDate = DateTime.Now;
                    tenant.RemovedBy = user?.Id;
                    tenant.StatusId = 3; // Inactive or Terminated status

                    // Update property to indicate no tenant
                    if (tenant.Property != null && tenant.Property.CurrentTenantId == tenant.Id)
                    {
                        tenant.Property.HasTenant = false;
                        tenant.Property.CurrentTenantId = null;
                        tenant.Property.UpdatedDate = DateTime.Now;
                        tenant.Property.UpdatedBy = user?.Id;
                    }

                    // Deactivate associated emails and contacts
                    foreach (var email in tenant.EmailAddresses)
                    {
                        email.IsActive = false;
                        email.UpdatedDate = DateTime.Now;
                        email.UpdatedBy = user?.Id;
                    }

                    foreach (var contact in tenant.ContactNumbers)
                    {
                        contact.IsActive = false;
                        contact.UpdatedDate = DateTime.Now;
                        contact.UpdatedBy = user?.Id;
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Response = tenant;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Tenant deleted successfully.";

                    _logger.LogInformation("Tenant soft deleted: {TenantId} by {UserId}", id, user?.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the tenant: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets all tenants for a company
        /// </summary>
        public async Task<ResponseModel> GetAllTenants(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenants = await context.PropertyTenants
                    .Include(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Include(t => t.Property)
                    .Include(t => t.Status)
                    .Include(t => t.TenantType)
                    .Where(t => t.CompanyId == companyId && !t.IsRemoved)
                    .OrderBy(t => t.LastName)
                    .ThenBy(t => t.FirstName)
                    .AsNoTracking()
                    .ToListAsync();

                response.Response = tenants;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenants retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} tenants for company {CompanyId}", tenants.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenants for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving tenants: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets tenants for a specific property
        /// </summary>
        public async Task<ResponseModel> GetTenantsByProperty(int propertyId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify property exists
                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == propertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                var tenants = await context.PropertyTenants
                    .Include(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Include(t => t.Status)
                    .Include(t => t.TenantType)
                    .Include(t => t.LeaseDocument)
                    .Where(t => t.PropertyId == propertyId && t.CompanyId == companyId && !t.IsRemoved)
                    .OrderByDescending(t => t.StatusId == 1) // Active tenants first
                    .ThenByDescending(t => t.LeaseStartDate) // Most recent leases next
                    .AsNoTracking()
                    .ToListAsync();

                response.Response = tenants;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenants retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} tenants for property {PropertyId}", tenants.Count, propertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenants for property {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving tenants: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets the current tenant for a property
        /// </summary>
        public async Task<ResponseModel> GetCurrentTenant(int propertyId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify property exists
                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == propertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                var tenant = await context.PropertyTenants
                    .Include(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Include(t => t.Status)
                    .Include(t => t.TenantType)
                    .Include(t => t.LeaseDocument)
                    .Where(t => t.PropertyId == propertyId &&
                                t.CompanyId == companyId &&
                                !t.IsRemoved &&
                                t.StatusId == 1) // Active status
                    .OrderByDescending(t => t.LeaseStartDate)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (tenant != null)
                {
                    response.Response = tenant;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Current tenant retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "No current tenant found for this property.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current tenant for property {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the tenant: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Adds an email address to a tenant
        /// </summary>
        public async Task<ResponseModel> AddEmailAddress(int tenantId, Email email)
        {
            ResponseModel response = new();

            try
            {
                if (string.IsNullOrEmpty(email.EmailAddress))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Email address is required.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var tenant = await context.PropertyTenants
                        .Include(t => t.EmailAddresses)
                        .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsRemoved);

                    if (tenant == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found.";
                        return response;
                    }

                    // Validate email
                    var emailValidator = new EmailValidator();
                    var validationResult = emailValidator.Validate(email);
                    if (!validationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }

                    // If marking as primary, unset other primary emails
                    if (email.IsPrimary)
                    {
                        foreach (var existingEmail in tenant.EmailAddresses.Where(e => e.IsPrimary))
                        {
                            existingEmail.IsPrimary = false;
                            existingEmail.UpdatedDate = DateTime.Now;
                        }
                    }

                    // Set relation properties
                    email.SetRelatedEntity("PropertyTenant", tenantId);
                    email.CreatedOn = DateTime.Now;
                    email.CreatedBy = tenant.UpdatedBy;

                    await context.Emails.AddAsync(email);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Response = email;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Email address added successfully.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding email address for tenant {TenantId}", tenantId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the email address: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Adds a contact number to a tenant
        /// </summary>
        public async Task<ResponseModel> AddContactNumber(int tenantId, ContactNumber contactNumber)
        {
            ResponseModel response = new();

            try
            {
                if (string.IsNullOrEmpty(contactNumber.Number))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Contact number is required.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var tenant = await context.PropertyTenants
                        .Include(t => t.ContactNumbers)
                        .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsRemoved);

                    if (tenant == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found.";
                        return response;
                    }

                    // Validate contact number
                    var contactValidator = new ContactNumberValidator();
                    var validationResult = contactValidator.Validate(contactNumber);
                    if (!validationResult.IsValid)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                        return response;
                    }

                    // If marking as primary, unset other primary numbers
                    if (contactNumber.IsPrimary)
                    {
                        foreach (var existingNumber in tenant.ContactNumbers.Where(c => c.IsPrimary))
                        {
                            existingNumber.IsPrimary = false;
                            existingNumber.UpdatedDate = DateTime.Now;
                        }
                    }

                    // Set relation properties
                    contactNumber.SetRelatedEntity("PropertyTenant", tenantId);
                    contactNumber.CreatedOn = DateTime.Now;
                    contactNumber.CreatedBy = tenant.UpdatedBy;

                    await context.ContactNumbers.AddAsync(contactNumber);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Response = contactNumber;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Contact number added successfully.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact number for tenant {TenantId}", tenantId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the contact number: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Updates the lease details for a tenant
        /// </summary>
        public async Task<ResponseModel> UpdateTenantLease(int tenantId, DateTime leaseStartDate, DateTime leaseEndDate, decimal rentAmount, string userId)
        {
            ResponseModel response = new();

            try
            {
                if (leaseEndDate <= leaseStartDate)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Lease end date must be after lease start date.";
                    return response;
                }

                if (rentAmount <= 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Rent amount must be greater than zero.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var tenant = await context.PropertyTenants
                        .Include(t => t.Property)
                        .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsRemoved);

                    if (tenant == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found.";
                        return response;
                    }

                    // Update lease information
                    tenant.LeaseStartDate = leaseStartDate;
                    tenant.LeaseEndDate = leaseEndDate;
                    tenant.RentAmount = rentAmount;
                    tenant.UpdatedDate = DateTime.Now;
                    tenant.UpdatedBy = userId;

                    // Update property lease dates if this is the current tenant
                    if (tenant.Property != null && tenant.Property.CurrentTenantId == tenant.Id)
                    {
                        tenant.Property.CurrentLeaseStartDate = leaseStartDate;
                        tenant.Property.LeaseEndDate = leaseEndDate;
                        tenant.Property.RentalAmount = rentAmount;
                        tenant.Property.UpdatedDate = DateTime.Now;
                        tenant.Property.UpdatedBy = userId;
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Lease information updated successfully.";

                    _logger.LogInformation("Lease updated for tenant {TenantId} by {UserId}", tenantId, userId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lease for tenant {TenantId}", tenantId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the lease: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets tenants with negative balance (in arrears)
        /// </summary>
        public async Task<ResponseModel> GetTenantsInArrears(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenantsInArrears = await context.PropertyTenants
                    .Include(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Include(t => t.Property)
                    .Include(t => t.Status)
                    .Where(t => t.CompanyId == companyId &&
                                !t.IsRemoved &&
                                t.Balance < 0 &&
                                t.StatusId == 1) // Active tenants only
                    .OrderBy(t => t.Balance) // Most in arrears first
                    .AsNoTracking()
                    .ToListAsync();

                response.Response = tenantsInArrears;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenants in arrears retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} tenants in arrears for company {CompanyId}", tenantsInArrears.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenants in arrears for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving tenants: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets tenant statistics for a company
        /// </summary>
        public async Task<ResponseModel> GetTenantStatistics(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Optimize query for statistics
                var tenants = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId && !t.IsRemoved)
                    .AsNoTracking()
                    .ToListAsync();

                var statistics = new
                {
                    TotalTenants = tenants.Count,
                    ActiveTenants = tenants.Count(t => t.StatusId == 1),
                    InactiveTenants = tenants.Count(t => t.StatusId != 1),
                    TenantsInArrears = tenants.Count(t => t.Balance < 0),
                    TotalArrears = Math.Abs(tenants.Where(t => t.Balance < 0).Sum(t => t.Balance)),
                    TotalMonthlyRent = tenants.Where(t => t.StatusId == 1).Sum(t => t.RentAmount),
                    AverageMonthlyRent = tenants.Count(t => t.StatusId == 1) > 0 ?
                        tenants.Where(t => t.StatusId == 1).Average(t => t.RentAmount) : 0,
                    TotalDepositsHeld = tenants.Where(t => t.StatusId == 1).Sum(t => t.DepositBalance ?? 0),
                    TenantsByStatus = tenants.GroupBy(t => t.StatusId)
                        .Select(g => new { StatusId = g.Key, Count = g.Count() })
                        .ToList(),
                    TenantsByType = tenants.GroupBy(t => t.TenantTypeId)
                        .Select(g => new { TypeId = g.Key, Count = g.Count() })
                        .ToList(),
                    LeaseEndingNext30Days = tenants.Count(t =>
                        t.StatusId == 1 &&
                        t.LeaseEndDate >= DateTime.Today &&
                        t.LeaseEndDate <= DateTime.Today.AddDays(30)),
                    LeaseEndingNext90Days = tenants.Count(t =>
                        t.StatusId == 1 &&
                        t.LeaseEndDate >= DateTime.Today &&
                        t.LeaseEndDate <= DateTime.Today.AddDays(90))
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant statistics for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving statistics: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets tenant documents
        /// </summary>
        public async Task<ResponseModel> GetTenantDocuments(int tenantId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                // Verify tenant exists
                using var context = await _contextFactory.CreateDbContextAsync();
                var tenant = await context.PropertyTenants
                    .FirstOrDefaultAsync(t => t.Id == tenantId && t.CompanyId == companyId && !t.IsRemoved);

                if (tenant == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Tenant not found.";
                    return response;
                }

                var cdnPath = $"company-{companyId}/tenants/{tenantId}/documents";
                var files = await _cdnService.GetFilesAsync("tenants", cdnPath);

                response.Response = files;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant documents retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for tenant {TenantId}", tenantId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving documents: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Uploads a document for a tenant
        /// </summary>
        public async Task<ResponseModel> UploadTenantDocument(int tenantId, IFormFile file, string category, string userId)
        {
            ResponseModel response = new();

            try
            {
                if (file == null || file.Length == 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "No file was uploaded.";
                    return response;
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "File type not allowed. Please upload a PDF, DOC, DOCX, JPG, JPEG or PNG file.";
                    return response;
                }

                // Validate category
                if (string.IsNullOrEmpty(category))
                {
                    category = "documents"; // Default category
                }
                else if (!new[] { "documents", "receipts" }.Contains(category.ToLower()))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Invalid category. Please use 'documents' or 'receipts'.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var tenant = await context.PropertyTenants
                    .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsRemoved);

                if (tenant == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Tenant not found.";
                    return response;
                }

                // Upload document with base64 backup
                var cdnPath = $"company-{tenant.CompanyId}/tenants/{tenant.Id}/{category}";
                string cdnUrl;

                using (var stream = file.OpenReadStream())
                {
                    cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        "tenants",
                        cdnPath
                    );
                }

                // Get file metadata
                var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);

                response.Response = new
                {
                    DocumentUrl = cdnUrl,
                    FileId = fileMetadata?.Id,
                    FileName = fileMetadata?.FileName,
                    ContentType = fileMetadata?.ContentType,
                    FileSize = fileMetadata?.FileSize
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for tenant {TenantId}", tenantId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the document: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Links a move-in inspection to a tenant
        /// </summary>
        public async Task<ResponseModel> LinkMoveInInspection(int tenantId, int inspectionId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var tenant = await context.PropertyTenants
                        .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsRemoved);

                    if (tenant == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found.";
                        return response;
                    }

                    // Verify inspection exists
                    var inspection = await context.PropertyInspections
                        .FirstOrDefaultAsync(i => i.Id == inspectionId && i.PropertyId == tenant.PropertyId);

                    if (inspection == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Inspection not found or not for this property.";
                        return response;
                    }

                    tenant.MoveInInspectionId = inspectionId;
                    tenant.MoveInInspectionCompleted = true;
                    tenant.UpdatedDate = DateTime.Now;
                    tenant.UpdatedBy = userId;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Move-in inspection linked successfully.";

                    _logger.LogInformation("Move-in inspection {InspectionId} linked to tenant {TenantId} by {UserId}", inspectionId, tenantId, userId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking move-in inspection for tenant {TenantId}", tenantId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while linking the inspection: " + ex.Message;
            }

            return response;
        }
    }
}