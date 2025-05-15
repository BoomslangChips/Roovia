using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using Roovia.Services.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roovia.Services
{
    public class PropertyOwnerService : IPropertyOwner
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<PropertyOwnerService> _logger;

        public PropertyOwnerService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<PropertyOwnerService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }



        public async Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Generate unique customer reference if not provided
                if (string.IsNullOrEmpty(propertyOwner.CustomerRef))
                {
                    propertyOwner.CustomerRef = await GenerateCustomerRef(context, propertyOwner.CompanyId);
                }

                // Set audit fields
                propertyOwner.CreatedOn = DateTime.Now;

                // Add the property owner
                await context.PropertyOwners.AddAsync(propertyOwner);
                await context.SaveChangesAsync();

                // Create CDN folder structure for owner
                var cdnPath = $"company-{propertyOwner.CompanyId}/owners/{propertyOwner.Id}";
                await _cdnService.CreateFolderAsync("owners", cdnPath, "profile");
                await _cdnService.CreateFolderAsync("owners", cdnPath, "documents");
                await _cdnService.CreateFolderAsync("owners", cdnPath, "properties");

                // Reload with related data
                var createdOwner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses)
                    .Include(o => o.ContactNumbers)
                    .Include(o => o.Properties)
                    .FirstOrDefaultAsync(o => o.Id == propertyOwner.Id);

                response.Response = createdOwner;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owner created successfully.";

                _logger.LogInformation("Property owner created with ID: {OwnerId}", propertyOwner.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property owner");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UploadOwnerProfilePicture(int ownerId, IFormFile file)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Delete existing profile picture if any
                if (!string.IsNullOrEmpty(owner.ProfilePictureUrl))
                {
                    await _cdnService.DeleteFileAsync(owner.ProfilePictureUrl);
                }

                // Upload new profile picture with base64 backup
                var cdnPath = $"company-{owner.CompanyId}/owners/{owner.Id}/profile";
                string cdnUrl;

                using (var stream = file.OpenReadStream())
                {
                    cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        "owners",
                        cdnPath
                    );
                }

                // Update owner profile picture URL
                owner.ProfilePictureUrl = cdnUrl;
                owner.UpdatedDate = DateTime.Now;

                await context.SaveChangesAsync();

                response.Response = new { ProfilePictureUrl = cdnUrl };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Profile picture uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading owner profile picture");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the profile picture: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UploadOwnerDocument(int ownerId, IFormFile file, int documentTypeId, string description = null)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Upload document with base64 backup
                var cdnPath = $"company-{owner.CompanyId}/owners/{owner.Id}/documents";
                string cdnUrl;

                using (var stream = file.OpenReadStream())
                {
                    cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        "owners",
                        cdnPath
                    );
                }

                // Create owner document record
                var ownerDocument = new OwnerDocument
                {
                    OwnerId = ownerId,
                    CompanyId = owner.CompanyId,
                    FileName = file.FileName,
                    Description = description,
                    DocumentTypeId = documentTypeId,
                    CdnUrl = cdnUrl,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    HasBase64Backup = true,
                    UploadedOn = DateTime.Now,
                    UploadedBy = owner.UpdatedBy ?? "System",
                    IsActive = true
                };

                context.OwnerDocuments.Add(ownerDocument);
                await context.SaveChangesAsync();

                response.Response = ownerDocument;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading owner document");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the document: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetOwnerDocuments(int ownerId, int? documentTypeId = null)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.OwnerDocuments
                    .Include(d => d.DocumentType)
                    .Where(d => d.OwnerId == ownerId && d.IsActive);

                if (documentTypeId.HasValue)
                {
                    query = query.Where(d => d.DocumentTypeId == documentTypeId.Value);
                }

                var documents = await query
                    .OrderByDescending(d => d.UploadedOn)
                    .ToListAsync();

                response.Response = documents;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Documents retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving owner documents");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving documents: " + ex.Message;
            }

            return response;
        }

        // Helper method to generate unique customer reference
        private async Task<string> GenerateCustomerRef(ApplicationDbContext context, int companyId)
        {
            var company = await context.Companies.FindAsync(companyId);
            var prefix = company?.Name.Substring(0, 3).ToUpper() ?? "OWN";

            var lastRef = await context.PropertyOwners
                .Where(o => o.CompanyId == companyId)
                .OrderByDescending(o => o.Id)
                .Select(o => o.CustomerRef)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastRef) && lastRef.StartsWith(prefix))
            {
                var lastNumber = lastRef.Substring(lastRef.Length - 4);
                if (int.TryParse(lastNumber, out int number))
                {
                    nextNumber = number + 1;
                }
            }

            return $"{prefix}-{nextNumber:D4}";
        }

        // Existing methods would remain but could be enhanced with additional lookups...
        public async Task<ResponseModel> GetPropertyOwnerById(int companyId, int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.Status)
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.MainImage)
                    .Include(o => o.Documents)
                        .ThenInclude(d => d.DocumentType)
                    .Where(o => o.Id == id && o.CompanyId == companyId && !o.IsRemoved)
                    .FirstOrDefaultAsync();

                if (owner != null)
                {
                    response.Response = owner;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property owner retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property owner {OwnerId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property owner: " + ex.Message;
            }

            return response;
        }
        public async Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Set audit fields
                propertyOwner.CreatedOn = DateTime.Now;

                // Add the property owner
                await context.PropertyOwners.AddAsync(propertyOwner);
                await context.SaveChangesAsync();

                // Reload with related data
                var createdOwner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses)
                    .Include(o => o.ContactNumbers)
                    .Include(o => o.Properties)
                    .FirstOrDefaultAsync(o => o.Id == propertyOwner.Id);

                response.Response = createdOwner;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owner created successfully.";

                _logger.LogInformation("Property owner created with ID: {OwnerId}", propertyOwner.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property owner");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyOwnerById(int companyId, int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.Status)
                    .Include(o => o.Properties)
                        .ThenInclude(p => p.MainImage)
                    .Where(o => o.Id == id && o.CompanyId == companyId && !o.IsRemoved)
                    .FirstOrDefaultAsync();

                if (owner != null)
                {
                    response.Response = owner;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property owner retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property owner {OwnerId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdatePropertyOwner(int id, PropertyOwner updatedOwner)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses)
                    .Include(o => o.ContactNumbers)
                    .FirstOrDefaultAsync(o => o.Id == id && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Update owner properties
                owner.FirstName = updatedOwner.FirstName;
                owner.LastName = updatedOwner.LastName;
                owner.IdNumber = updatedOwner.IdNumber;
                owner.VatNumber = updatedOwner.VatNumber;
                owner.Address = updatedOwner.Address;
                owner.BankAccount = updatedOwner.BankAccount;
                owner.IsEmailNotificationsEnabled = updatedOwner.IsEmailNotificationsEnabled;
                owner.IsSmsNotificationsEnabled = updatedOwner.IsSmsNotificationsEnabled;
                owner.CustomerRef = updatedOwner.CustomerRef;
                owner.Tags = updatedOwner.Tags;
                owner.UpdatedDate = DateTime.Now;
                owner.UpdatedBy = updatedOwner.UpdatedBy;

                await context.SaveChangesAsync();

                // Reload with related data
                var updatedResult = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                    .FirstOrDefaultAsync(o => o.Id == id);

                response.Response = updatedResult;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owner updated successfully.";

                _logger.LogInformation("Property owner updated: {OwnerId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property owner {OwnerId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeletePropertyOwner(int id, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.Properties)
                    .FirstOrDefaultAsync(o => o.Id == id && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // Check if owner has active properties
                if (owner.Properties.Any(p => !p.IsRemoved))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete property owner with active properties.";
                    return response;
                }

                // Soft delete
                owner.IsRemoved = true;
                owner.RemovedDate = DateTime.Now;
                owner.RemovedBy = user?.Id;

                await context.SaveChangesAsync();

                response.Response = owner;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owner deleted successfully.";

                _logger.LogInformation("Property owner soft deleted: {OwnerId} by {UserId}", id, user?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property owner {OwnerId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllPropertyOwners(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owners = await context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName)
                    .ToListAsync();

                response.Response = owners;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owners retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} property owners for company {CompanyId}", owners.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property owners for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving property owners: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddEmailAddress(int ownerId, Email email)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.EmailAddresses)
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // If marking as primary, unset other primary emails
                if (email.IsPrimary)
                {
                    foreach (var existingEmail in owner.EmailAddresses.Where(e => e.IsPrimary))
                    {
                        existingEmail.IsPrimary = false;
                        existingEmail.UpdatedDate = DateTime.Now;
                    }
                }

                // Set relation properties
                email.RelatedEntityType = "PropertyOwner";
                email.RelatedEntityId = ownerId;
                email.PropertyOwnerId = ownerId;
                email.CreatedOn = DateTime.Now;
                email.CreatedBy = owner.UpdatedBy;

                owner.EmailAddresses.Add(email);
                await context.SaveChangesAsync();

                response.Response = email;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding email address for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the email address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddContactNumber(int ownerId, ContactNumber contactNumber)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var owner = await context.PropertyOwners
                    .Include(o => o.ContactNumbers)
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property owner not found.";
                    return response;
                }

                // If marking as primary, unset other primary numbers
                if (contactNumber.IsPrimary)
                {
                    foreach (var existingNumber in owner.ContactNumbers.Where(c => c.IsPrimary))
                    {
                        existingNumber.IsPrimary = false;
                        existingNumber.UpdatedDate = DateTime.Now;
                    }
                }

                // Set relation properties
                contactNumber.RelatedEntityType = "PropertyOwner";
                contactNumber.RelatedEntityId = ownerId;
                contactNumber.PropertyOwnerId = ownerId;
                contactNumber.CreatedOn = DateTime.Now;
                contactNumber.CreatedBy = owner.UpdatedBy;

                owner.ContactNumbers.Add(contactNumber);
                await context.SaveChangesAsync();

                response.Response = contactNumber;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact number for property owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the contact number: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyOwnersByPage(int companyId, int pageNumber, int pageSize)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.PropertyOwners
                    .Include(o => o.EmailAddresses.Where(e => e.IsActive))
                    .Include(o => o.ContactNumbers.Where(c => c.IsActive))
                    .Include(o => o.Properties)
                    .Where(o => o.CompanyId == companyId && !o.IsRemoved)
                    .OrderBy(o => o.LastName)
                    .ThenBy(o => o.FirstName);

                var totalCount = await query.CountAsync();
                var owners = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                response.Response = new
                {
                    Data = owners,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property owners retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated property owners for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving property owners: " + ex.Message;
            }

            return response;
        }
    }
}