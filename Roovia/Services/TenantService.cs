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
using Roovia.Services.General;

namespace Roovia.Services
{
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
            _contextFactory = contextFactory;
            _logger = logger;
            _cdnService = cdnService;
        }

        public async Task<ResponseModel> CreateTenant(PropertyTenant tenant, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify the property exists and belongs to the company
                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == tenant.PropertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found or does not belong to the company.";
                    return response;
                }

                // Set audit fields
                tenant.CompanyId = companyId;
                tenant.CreatedOn = DateTime.Now;

                // Add the tenant
                await context.PropertyTenants.AddAsync(tenant);

                // Update property to indicate it has a tenant
                property.HasTenant = true;
                property.CurrentTenantId = tenant.Id;
                property.UpdatedDate = DateTime.Now;

                await context.SaveChangesAsync();

                // Create CDN folder structure for tenant
                var cdnFolderPath = $"company-{companyId}/tenants/{tenant.Id}";
                await _cdnService.CreateFolderAsync("tenants", cdnFolderPath, "documents");
                await _cdnService.CreateFolderAsync("tenants", cdnFolderPath, "receipts");

                // Reload with related data
                var createdTenant = await context.PropertyTenants
                    .Include(t => t.EmailAddresses)
                    .Include(t => t.ContactNumbers)
                    .Include(t => t.Property)
                    .Include(t => t.Status)
                    .Include(t => t.LeaseDocument)
                    .FirstOrDefaultAsync(t => t.Id == tenant.Id);

                response.Response = createdTenant;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant created successfully.";

                _logger.LogInformation("Tenant created with ID: {TenantId} for property {PropertyId}", tenant.Id, tenant.PropertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tenant");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the tenant: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UploadLeaseDocument(int tenantId, IFormFile file, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

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

                    response.Response = new { DocumentUrl = cdnUrl, FileId = fileMetadata.Id };
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Lease document uploaded successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Failed to save document metadata.";
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
                    .Include(t => t.LeaseDocument)
                    .Include(t => t.Payments)
                    .Include(t => t.MaintenanceRequests)
                    .Include(t => t.PaymentSchedules)
                    .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsRemoved)
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

        public async Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenant = await context.PropertyTenants
                    .Include(t => t.EmailAddresses)
                    .Include(t => t.ContactNumbers)
                    .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId && !t.IsRemoved);

                if (tenant == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Tenant not found.";
                    return response;
                }

                // Update tenant fields
                tenant.FirstName = updatedTenant.FirstName;
                tenant.LastName = updatedTenant.LastName;
                tenant.IdNumber = updatedTenant.IdNumber;
                tenant.IsEmailNotificationsEnabled = updatedTenant.IsEmailNotificationsEnabled;
                tenant.IsSmsNotificationsEnabled = updatedTenant.IsSmsNotificationsEnabled;
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

                await context.SaveChangesAsync();

                // Reload with related data
                var updatedResult = await context.PropertyTenants
                    .Include(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Include(t => t.Property)
                    .Include(t => t.Status)
                    .Include(t => t.LeaseDocument)
                    .FirstOrDefaultAsync(t => t.Id == id);

                response.Response = updatedResult;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant updated successfully.";

                _logger.LogInformation("Tenant updated: {TenantId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant {TenantId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the tenant: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteTenant(int id, int companyId, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenant = await context.PropertyTenants
                    .Include(t => t.Property)
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

                // Update property to indicate no tenant
                if (tenant.Property != null && tenant.Property.CurrentTenantId == tenant.Id)
                {
                    tenant.Property.HasTenant = false;
                    tenant.Property.CurrentTenantId = null;
                    tenant.Property.UpdatedDate = DateTime.Now;
                }

                await context.SaveChangesAsync();

                response.Response = tenant;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant deleted successfully.";

                _logger.LogInformation("Tenant soft deleted: {TenantId} by {UserId}", id, user?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the tenant: " + ex.Message;
            }

            return response;
        }

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
                    .Include(t => t.LeaseDocument)
                    .Where(t => t.CompanyId == companyId && !t.IsRemoved)
                    .OrderBy(t => t.LastName)
                    .ThenBy(t => t.FirstName)
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

        public async Task<ResponseModel> GetTenantsByProperty(int propertyId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenants = await context.PropertyTenants
                    .Include(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Include(t => t.Status)
                    .Include(t => t.LeaseDocument)
                    .Where(t => t.PropertyId == propertyId && t.CompanyId == companyId && !t.IsRemoved)
                    .OrderBy(t => t.LeaseStartDate)
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

        public async Task<ResponseModel> GetCurrentTenant(int propertyId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenant = await context.PropertyTenants
                    .Include(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Include(t => t.Status)
                    .Include(t => t.LeaseDocument)
                    .Where(t => t.PropertyId == propertyId &&
                                t.CompanyId == companyId &&
                                !t.IsRemoved &&
                                t.StatusId == 1) // Assuming 1 is Active status
                    .OrderByDescending(t => t.LeaseStartDate)
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

        public async Task<ResponseModel> AddEmailAddress(int tenantId, Email email)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenant = await context.PropertyTenants
                    .Include(t => t.EmailAddresses)
                    .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsRemoved);

                if (tenant == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Tenant not found.";
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
                email.RelatedEntityType = "PropertyTenant";
                email.RelatedEntityId = tenantId;
                email.PropertyTenantId = tenantId;
                email.CreatedOn = DateTime.Now;
                email.CreatedBy = tenant.UpdatedBy;

                tenant.EmailAddresses.Add(email);
                await context.SaveChangesAsync();

                response.Response = email;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding email address for tenant {TenantId}", tenantId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the email address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddContactNumber(int tenantId, ContactNumber contactNumber)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenant = await context.PropertyTenants
                    .Include(t => t.ContactNumbers)
                    .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsRemoved);

                if (tenant == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Tenant not found.";
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
                contactNumber.RelatedEntityType = "PropertyTenant";
                contactNumber.RelatedEntityId = tenantId;
                contactNumber.PropertyTenantId = tenantId;
                contactNumber.CreatedOn = DateTime.Now;
                contactNumber.CreatedBy = tenant.UpdatedBy;

                tenant.ContactNumbers.Add(contactNumber);
                await context.SaveChangesAsync();

                response.Response = contactNumber;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact number for tenant {TenantId}", tenantId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the contact number: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateTenantLease(int tenantId, DateTime leaseStartDate, DateTime leaseEndDate, decimal rentAmount, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

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
                }

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Lease information updated successfully.";

                _logger.LogInformation("Lease updated for tenant {TenantId} by {UserId}", tenantId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lease for tenant {TenantId}", tenantId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the lease: " + ex.Message;
            }

            return response;
        }

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
                    .Include(t => t.LeaseDocument)
                    .Where(t => t.CompanyId == companyId &&
                                !t.IsRemoved &&
                                t.Balance < 0 &&
                                t.StatusId == 1) // Active tenants only
                    .OrderBy(t => t.Balance)
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

        public async Task<ResponseModel> GetTenantStatistics(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tenants = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId && !t.IsRemoved)
                    .ToListAsync();

                var statistics = new
                {
                    TotalTenants = tenants.Count,
                    ActiveTenants = tenants.Count(t => t.StatusId == 1),
                    InactiveTenants = tenants.Count(t => t.StatusId != 1),
                    TenantsInArrears = tenants.Count(t => t.Balance < 0),
                    TotalArrears = Math.Abs(tenants.Where(t => t.Balance < 0).Sum(t => t.Balance)),
                    TotalMonthlyRent = tenants.Where(t => t.StatusId == 1).Sum(t => t.RentAmount),
                    TotalDepositsHeld = tenants.Where(t => t.StatusId == 1).Sum(t => t.DepositBalance ?? 0),
                    TenantsByStatus = tenants.GroupBy(t => t.StatusId)
                        .Select(g => new { StatusId = g.Key, Count = g.Count() })
                        .ToList()
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

        public async Task<ResponseModel> GetTenantDocuments(int tenantId, int companyId)
        {
            ResponseModel response = new();

            try
            {
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

        public async Task<ResponseModel> UploadTenantDocument(int tenantId, IFormFile file, string category, string userId)
        {
            ResponseModel response = new();

            try
            {
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

                response.Response = new { DocumentUrl = cdnUrl };
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

        public async Task<ResponseModel> LinkMoveInInspection(int tenantId, int inspectionId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

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

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Move-in inspection linked successfully.";
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