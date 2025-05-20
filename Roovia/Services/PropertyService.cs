using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Services
{
    /// <summary>
    /// Service for managing properties
    /// </summary>
    public class PropertyService : IProperty
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<PropertyService> _logger;
        private readonly ICdnService _cdnService;

        public PropertyService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<PropertyService> logger,
            ICdnService cdnService)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cdnService = cdnService ?? throw new ArgumentNullException(nameof(cdnService));
        }

        /// <summary>
        /// Creates a new property
        /// </summary>
        public async Task<ResponseModel> CreateProperty(Property property)
        {
            ResponseModel response = new();

            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(property.PropertyName))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property name is required.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Validate owner exists if an owner ID is provided
                    if (property.OwnerId.HasValue && property.OwnerId > 0)
                    {
                        var owner = await context.PropertyOwners
                            .FirstOrDefaultAsync(o => o.Id == property.OwnerId && !o.IsRemoved);

                        if (owner == null)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = "The specified property owner does not exist.";
                            return response;
                        }
                    }
                    else
                    {
                        // Ensure OwnerId is null, not 0 or other invalid value
                        property.OwnerId = null;
                    }
                    if (property.CommissionTypeId.HasValue)
                    {
                        var commissionType = await context.CommissionTypes
                            .FirstOrDefaultAsync(c => c.Id == property.CommissionTypeId.Value);

                        if (commissionType == null)
                        {
                            // Set to null if the commission type doesn't exist
                            property.CommissionTypeId = null;

                            // Optional: Log a warning or add a note to the response
                            _logger.LogWarning("Commission type with ID {CommissionTypeId} not found. Setting to null.", property.CommissionTypeId);
                        }
                    }
                    // Check if property code already exists
                    if (!string.IsNullOrEmpty(property.PropertyCode))
                    {
                        var existingPropertyWithCode = await context.Properties
                            .FirstOrDefaultAsync(p => p.PropertyCode == property.PropertyCode &&
                                                   p.CompanyId == property.CompanyId &&
                                                   !p.IsRemoved);

                        if (existingPropertyWithCode != null)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = "A property with this code already exists.";
                            return response;
                        }
                    }
                    else
                    {
                        // Generate property code if not provided
                        property.PropertyCode = await GenerateUniquePropertyCode(context, property.CompanyId);
                    }

                    // Validate address
                    if (property.Address != null)
                    {
                        var addressValidator = new AddressValidator();
                        var validationResult = addressValidator.Validate(property.Address);
                        if (!validationResult.IsValid)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                            return response;
                        }
                    }

                    // Ensure dates are valid
                    property.EnsureValidDates();

                    // Set creation date if not already set
                    if (property.CreatedOn == default)
                        property.CreatedOn = DateTime.Now;

                    // Add the property
                    await context.Properties.AddAsync(property);
                    await context.SaveChangesAsync();

                    // Create CDN folder structure for property
                    try
                    {
                        var cdnFolderPath = $"company-{property.CompanyId}/properties/{property.Id}";
                        await _cdnService.CreateFolderAsync("properties", cdnFolderPath, "images");
                        await _cdnService.CreateFolderAsync("properties", cdnFolderPath, "documents");
                        await _cdnService.CreateFolderAsync("properties", cdnFolderPath, "inspections");
                    }
                    catch (Exception cdnEx)
                    {
                        // Log but continue with transaction
                        _logger.LogWarning(cdnEx, "Error creating CDN folders for property {PropertyId}. Continuing with creation.", property.Id);
                    }

                    await transaction.CommitAsync();

                    // Reload with related data - use left joins to handle missing owner
                    var createdProperty = await context.Properties
                        .Include(p => p.Owner)
                        .Include(p => p.Status)
                        .Include(p => p.CommissionType)
                        .Include(p => p.MainImage)
                        .Include(p => p.PropertyType)
                        .FirstOrDefaultAsync(p => p.Id == property.Id);

                    response.Response = createdProperty;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property created successfully.";

                    _logger.LogInformation("Property created with ID: {PropertyId}", property.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the property: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Uploads a main image for a property
        /// </summary>
        public async Task<ResponseModel> UploadPropertyMainImage(int propertyId, IFormFile file, string userId)
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
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "File type not allowed. Please upload a JPG, JPEG, PNG or GIF file.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var property = await context.Properties
                        .FirstOrDefaultAsync(p => p.Id == propertyId && !p.IsRemoved);

                    if (property == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found.";
                        return response;
                    }

                    // Delete old main image if exists
                    if (property.MainImageId.HasValue)
                    {
                        var oldImage = await context.CdnFileMetadata
                            .FirstOrDefaultAsync(f => f.Id == property.MainImageId.Value);

                        if (oldImage != null)
                        {
                            await _cdnService.DeleteFileAsync(oldImage.Url);
                        }
                    }

                    // Upload new image with base64 backup
                    var cdnPath = $"company-{property.CompanyId}/properties/{property.Id}/images";
                    string cdnUrl;

                    using (var stream = file.OpenReadStream())
                    {
                        cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                            stream,
                            file.FileName,
                            file.ContentType,
                            "properties",
                            cdnPath
                        );
                    }

                    // Get the file metadata
                    var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);
                    if (fileMetadata != null)
                    {
                        property.MainImageId = fileMetadata.Id;
                        property.UpdatedDate = DateTime.Now;
                        property.UpdatedBy = userId;

                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        response.Response = new { ImageUrl = cdnUrl, FileId = fileMetadata.Id };
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Main image uploaded successfully.";
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Failed to save image metadata.";
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
                _logger.LogError(ex, "Error uploading property main image");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the image: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets a property by ID
        /// </summary>
        public async Task<ResponseModel> GetPropertyById(int id, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var property = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Company)
                    .Include(p => p.Branch)
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Include(p => p.PropertyType)
                    .Include(p => p.Beneficiaries.Where(b => b.IsActive))
                        .ThenInclude(b => b.BenType)
                    .Include(p => p.Beneficiaries)
                        .ThenInclude(b => b.BenStatus)
                    .Include(p => p.Tenants.Where(t => !t.IsRemoved))
                        .ThenInclude(t => t.Status)
                    .Include(p => p.Tenants)
                        .ThenInclude(t => t.LeaseDocument)
                    .Include(p => p.Inspections.OrderByDescending(i => i.ScheduledDate).Take(5))
                        .ThenInclude(i => i.Status)
                    .Include(p => p.MaintenanceTickets.OrderByDescending(mt => mt.CreatedOn).Take(5))
                        .ThenInclude(mt => mt.Status)
                    .Include(p => p.Payments.OrderByDescending(pm => pm.DueDate).Take(5))
                        .ThenInclude(pm => pm.Status)
                    .Where(p => p.Id == id && p.CompanyId == companyId && !p.IsRemoved)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (property != null)
                {
                    response.Response = property;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property {PropertyId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Updates an existing property
        /// </summary>
        public async Task<ResponseModel> UpdateProperty(int id, Property updatedProperty, int companyId)
        {
            ResponseModel response = new();

            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(updatedProperty.PropertyName))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property name is required.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Get the existing property
                    var property = await context.Properties
                        .Include(p => p.Beneficiaries)
                        .Include(p => p.Tenants)
                        .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsRemoved);

                    if (property == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found.";
                        return response;
                    }

                    // Validate owner exists if an owner ID is provided and has changed
                    if (updatedProperty.OwnerId.HasValue && updatedProperty.OwnerId > 0)
                    {
                        var owner = await context.PropertyOwners
                            .FirstOrDefaultAsync(o => o.Id == updatedProperty.OwnerId && !o.IsRemoved);

                        if (owner == null)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = "The specified property owner does not exist.";
                            return response;
                        }
                    }
                    else
                    {
                        // Ensure OwnerId is null, not 0 or other invalid value
                        property.OwnerId = null;
                    }
                    if (property.CommissionTypeId.HasValue)
                    {
                        var commissionType = await context.CommissionTypes
                            .FirstOrDefaultAsync(c => c.Id == property.CommissionTypeId.Value);

                        if (commissionType == null)
                        {
                            // Set to null if the commission type doesn't exist
                            property.CommissionTypeId = null;

                            // Optional: Log a warning or add a note to the response
                            _logger.LogWarning("Commission type with ID {CommissionTypeId} not found. Setting to null.", property.CommissionTypeId);
                        }
                    }
                    // Check if property code already exists (if changed)
                    if (!string.IsNullOrEmpty(updatedProperty.PropertyCode) &&
                        property.PropertyCode != updatedProperty.PropertyCode)
                    {
                        var existingPropertyWithCode = await context.Properties
                            .FirstOrDefaultAsync(p => p.PropertyCode == updatedProperty.PropertyCode &&
                                                   p.CompanyId == companyId &&
                                                   p.Id != id &&
                                                   !p.IsRemoved);

                        if (existingPropertyWithCode != null)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = "A property with this code already exists.";
                            return response;
                        }
                    }

                    // Validate address
                    if (updatedProperty.Address != null)
                    {
                        var addressValidator = new AddressValidator();
                        var validationResult = addressValidator.Validate(updatedProperty.Address);
                        if (!validationResult.IsValid)
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                            return response;
                        }
                    }

                    // Update the property fields
                    property.PropertyName = updatedProperty.PropertyName;
                    property.PropertyCode = updatedProperty.PropertyCode;
                    property.CustomerRef = updatedProperty.CustomerRef;
                    property.PropertyTypeId = updatedProperty.PropertyTypeId;
                    property.StatusId = updatedProperty.StatusId;
                    property.ServiceLevel = updatedProperty.ServiceLevel;
                    property.Tags = updatedProperty.Tags;
                    property.BranchId = updatedProperty.BranchId;

                    // Update owner ID - can be set to 0 if removing owner
                    property.OwnerId = updatedProperty.OwnerId;

                    // Update financial details
                    property.RentalAmount = updatedProperty.RentalAmount;
                    property.PropertyAccountBalance = updatedProperty.PropertyAccountBalance;

                    // Update commission details
                    property.CommissionTypeId = updatedProperty.CommissionTypeId;
                    property.CommissionValue = updatedProperty.CommissionValue;

                    // Update payment settings
                    property.PaymentsEnabled = updatedProperty.PaymentsEnabled;
                    property.PaymentsVerify = updatedProperty.PaymentsVerify;

                    // Update address
                    if (updatedProperty.Address != null)
                    {
                        property.Address = updatedProperty.Address;
                    }

                    // Update main image if changed
                    if (updatedProperty.MainImageId != property.MainImageId)
                    {
                        property.MainImageId = updatedProperty.MainImageId;
                    }

                    // Update audit fields
                    property.UpdatedDate = DateTime.Now;
                    property.UpdatedBy = updatedProperty.UpdatedBy;

                    // Ensure dates are valid
                    property.EnsureValidDates();

                    // Save the changes
                    context.Properties.Update(property);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Reload with related data
                    var updatedPropertyWithDetails = await context.Properties
                        .Include(p => p.Owner)
                        .Include(p => p.Status)
                        .Include(p => p.CommissionType)
                        .Include(p => p.MainImage)
                        .Include(p => p.PropertyType)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    response.Response = updatedPropertyWithDetails;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property updated successfully.";

                    _logger.LogInformation("Property updated with ID: {PropertyId}", property.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the property: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Deletes a property (soft delete)
        /// </summary>
        public async Task<ResponseModel> DeleteProperty(int id, int companyId, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var property = await context.Properties
                        .Include(p => p.Tenants.Where(t => !t.IsRemoved))
                        .Include(p => p.Beneficiaries.Where(b => b.IsActive))
                        .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsRemoved);

                    if (property == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found.";
                        return response;
                    }

                    // Check if property has active tenants
                    if (property.Tenants.Any(t => t.StatusId == 1)) // Active tenants
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Cannot delete property with active tenants.";
                        return response;
                    }

                    // Soft delete
                    property.IsRemoved = true;
                    property.RemovedDate = DateTime.Now;
                    property.RemovedBy = user?.Id;

                    // Mark beneficiaries as inactive
                    foreach (var beneficiary in property.Beneficiaries)
                    {
                        beneficiary.IsActive = false;
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Response = property;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property deleted successfully.";

                    _logger.LogInformation("Property soft deleted: {PropertyId} by {UserId}", id, user?.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property {PropertyId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the property: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets all properties for a company
        /// </summary>
        public async Task<ResponseModel> GetAllProperties(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Include(p => p.PropertyType)
                    .Where(p => p.CompanyId == companyId && !p.IsRemoved)
                    .OrderBy(p => p.PropertyCode)
                    .AsNoTracking()
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} properties for company {CompanyId}", properties.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets properties for a specific owner
        /// </summary>
        public async Task<ResponseModel> GetPropertiesByOwner(int ownerId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Include(p => p.PropertyType)
                    .Include(p => p.Tenants.Where(t => !t.IsRemoved && t.StatusId == 1))
                    .Where(p => p.OwnerId == ownerId && !p.IsRemoved)
                    .OrderBy(p => p.PropertyCode)
                    .AsNoTracking()
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} properties for owner {OwnerId}", properties.Count, ownerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties for owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets properties for a specific branch
        /// </summary>
        public async Task<ResponseModel> GetPropertiesByBranch(int branchId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Include(p => p.PropertyType)
                    .Where(p => p.BranchId == branchId && !p.IsRemoved)
                    .OrderBy(p => p.PropertyCode)
                    .AsNoTracking()
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} properties for branch {BranchId}", properties.Count, branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties for branch {BranchId}", branchId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets properties with tenants
        /// </summary>
        public async Task<ResponseModel> GetPropertiesWithTenants(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.MainImage)
                    .Include(p => p.PropertyType)
                    .Include(p => p.Tenants.Where(t => !t.IsRemoved && t.StatusId == 1))
                        .ThenInclude(t => t.Status)
                    .Include(p => p.Tenants)
                        .ThenInclude(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(p => p.Tenants)
                        .ThenInclude(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Where(p => p.CompanyId == companyId && !p.IsRemoved && p.HasTenant)
                    .OrderBy(p => p.PropertyCode)
                    .AsNoTracking()
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties with tenants retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} properties with tenants for company {CompanyId}", properties.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties with tenants for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets vacant properties
        /// </summary>
        public async Task<ResponseModel> GetVacantProperties(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.MainImage)
                    .Include(p => p.PropertyType)
                    .Where(p => p.CompanyId == companyId && !p.IsRemoved && !p.HasTenant)
                    .OrderBy(p => p.PropertyCode)
                    .AsNoTracking()
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vacant properties retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} vacant properties for company {CompanyId}", properties.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vacant properties for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Updates the status of a property
        /// </summary>
        public async Task<ResponseModel> UpdatePropertyStatus(int propertyId, int statusId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == propertyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                // Validate status exists
                var status = await context.PropertyStatusTypes
                    .FirstOrDefaultAsync(s => s.Id == statusId && s.IsActive);

                if (status == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "The specified status does not exist or is inactive.";
                    return response;
                }

                property.StatusId = statusId;
                property.UpdatedDate = DateTime.Now;
                property.UpdatedBy = userId;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property status updated successfully.";

                _logger.LogInformation("Property status updated: {PropertyId} to status {StatusId} by {UserId}", propertyId, statusId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property status {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating property status: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets property statistics for a company
        /// </summary>
        public async Task<ResponseModel> GetPropertyStatistics(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Optimize query for statistics
                var properties = await context.Properties
                    .Where(p => p.CompanyId == companyId && !p.IsRemoved)
                    .AsNoTracking()
                    .ToListAsync();

                var statistics = new
                {
                    TotalProperties = properties.Count,
                    OccupiedProperties = properties.Count(p => p.HasTenant),
                    VacantProperties = properties.Count(p => !p.HasTenant),
                    TotalMonthlyRental = properties
                        .Where(p => p.HasTenant)
                        .Sum(p => p.RentalAmount),
                    AverageRental = properties.Count > 0 ?
                        properties
                            .Where(p => p.HasTenant)
                            .Average(p => p.RentalAmount) : 0,
                    PropertiesByStatus = properties
                        .GroupBy(p => p.StatusId)
                        .Select(g => new { StatusId = g.Key, Count = g.Count() })
                        .ToList(),
                    PropertiesByType = properties
                        .GroupBy(p => p.PropertyTypeId)
                        .Select(g => new { TypeId = g.Key, Count = g.Count() })
                        .ToList(),
                    OccupancyRate = properties.Count > 0 ?
                        Math.Round(100.0 * properties.Count(p => p.HasTenant) / properties.Count, 2) : 0
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property statistics for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving statistics: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets document folders for a property
        /// </summary>
        public async Task<ResponseModel> GetPropertyDocumentFolders(int propertyId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                // Verify property exists
                using var context = await _contextFactory.CreateDbContextAsync();
                var property = await context.Properties
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == propertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                var cdnPath = $"company-{companyId}/properties/{propertyId}/documents";
                var folders = await _cdnService.GetFoldersAsync("properties", cdnPath);

                response.Response = folders;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document folders retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document folders for property {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving document folders: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets CDN files for a property
        /// </summary>
        public async Task<ResponseModel> GetPropertyCdnFiles(int propertyId, int companyId, string category = "images")
        {
            ResponseModel response = new();

            try
            {
                // Verify property exists
                using var context = await _contextFactory.CreateDbContextAsync();
                var property = await context.Properties
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == propertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                // Validate category
                if (!new[] { "images", "documents", "inspections" }.Contains(category))
                {
                    category = "images"; // Default to images if invalid category
                }

                var cdnPath = $"company-{companyId}/properties/{propertyId}/{category}";
                var files = await _cdnService.GetFilesAsync("properties", cdnPath);

                response.Response = files;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Property {category} retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving CDN files for property {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving files: " + ex.Message;
            }

            return response;
        }

        // Helper methods
        private async Task<string> GenerateUniquePropertyCode(ApplicationDbContext context, int companyId)
        {
            // Generate unique property code based on company prefix and sequential number
            var company = await context.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId);

            string prefix = "P";
            if (company != null && !string.IsNullOrEmpty(company.Name))
            {
                // Use first letter of company name as prefix
                prefix = company.Name.Substring(0, 1).ToUpper();
            }

            // Get highest existing code number for this prefix
            var highestCode = await context.Properties
                .Where(p => p.CompanyId == companyId && p.PropertyCode.StartsWith(prefix))
                .Select(p => p.PropertyCode)
                .OrderByDescending(p => p)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(highestCode))
            {
                // Extract number from highest code
                var match = System.Text.RegularExpressions.Regex.Match(highestCode, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int highestNumber))
                {
                    nextNumber = highestNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }
    }
}