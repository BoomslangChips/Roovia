using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessModels;
using Roovia.Models.BusinessMappingModels;
using Roovia.Models.Helper;
using Roovia.Models.Users;

namespace Roovia.Services
{
    public interface IPropertyService
    {
        // Property CRUD
        Task<ResponseModel> CreateProperty(Property property);
        Task<ResponseModel> GetPropertyById(int id, int companyId);
        Task<ResponseModel> UpdateProperty(int id, Property property);
        Task<ResponseModel> DeleteProperty(int id, int companyId, ApplicationUser user);
        Task<ResponseModel> GetAllProperties(int companyId);
        Task<ResponseModel> GetPropertiesByOwner(int ownerId);
        
        // Image Management
        Task<ResponseModel> UploadPropertyImage(int propertyId, IFormFile file, int imageTypeId, string description = null, ApplicationUser user = null);
        Task<ResponseModel> DeletePropertyImage(int imageId, int companyId, ApplicationUser user);
        Task<ResponseModel> SetMainImage(int propertyId, int imageId, int companyId);
        Task<ResponseModel> GetPropertyImages(int propertyId);
        
        // Document Management
        Task<ResponseModel> UploadPropertyDocument(int propertyId, IFormFile file, int documentTypeId, int categoryId, string description = null, ApplicationUser user = null);
        Task<ResponseModel> DeletePropertyDocument(int documentId, int companyId, ApplicationUser user);
        Task<ResponseModel> GetPropertyDocuments(int propertyId, int? categoryId = null);
        
        // Beneficiary Management
        Task<ResponseModel> AddBeneficiary(PropertyBeneficiary beneficiary);
        Task<ResponseModel> UpdateBeneficiary(int id, PropertyBeneficiary beneficiary);
        Task<ResponseModel> DeleteBeneficiary(int id, int companyId, ApplicationUser user);
        Task<ResponseModel> GetPropertyBeneficiaries(int propertyId);
        
        // Tenant Management
        Task<ResponseModel> AddTenant(PropertyTenant tenant);
        Task<ResponseModel> UpdateTenant(int id, PropertyTenant tenant);
        Task<ResponseModel> DeleteTenant(int id, int companyId, ApplicationUser user);
        Task<ResponseModel> GetPropertyTenants(int propertyId);
        Task<ResponseModel> GetActiveTenant(int propertyId);
    }

    public class PropertyService : IPropertyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICdnService _cdnService;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(
            ApplicationDbContext context,
            ICdnService cdnService,
            ILogger<PropertyService> logger)
        {
            _context = context;
            _cdnService = cdnService;
            _logger = logger;
        }

        // Property CRUD Operations
        public async Task<ResponseModel> CreateProperty(Property property)
        {
            var response = new ResponseModel();

            try
            {
                // Generate unique property code
                property.PropertyCode = await GeneratePropertyCode(property.CompanyId);
                
                // Ensure valid dates
                property.EnsureValidDates();
                
                // Set CDN folder path
                property.CdnFolderPath = property.GetCdnFolderPath();
                
                // Set creation timestamp
                property.CreatedOn = DateTime.Now;

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                // Create CDN folder structure for property
                var cdnFolderPath = $"company-{property.CompanyId}/properties/{property.Id}";
                await _cdnService.CreateFolderAsync("properties", cdnFolderPath, "images");
                await _cdnService.CreateFolderAsync("properties", cdnFolderPath, "documents");

                response.Response = property;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyById(int id, int companyId)
        {
            var response = new ResponseModel();

            try
            {
                var property = await _context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Company)
                    .Include(p => p.Branch)
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.Images).ThenInclude(i => i.ImageType)
                    .Include(p => p.Beneficiaries).ThenInclude(b => b.BenType)
                    .Include(p => p.Documents).ThenInclude(d => d.DocumentType)
                    .Include(p => p.Tenants).ThenInclude(t => t.Status)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                response.Response = property;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateProperty(int id, Property updatedProperty)
        {
            var response = new ResponseModel();

            try
            {
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == updatedProperty.CompanyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                // Update properties
                property.PropertyName = updatedProperty.PropertyName;
                property.RentalAmount = updatedProperty.RentalAmount;
                property.PropertyAccountBalance = updatedProperty.PropertyAccountBalance;
                property.StatusId = updatedProperty.StatusId;
                property.ServiceLevel = updatedProperty.ServiceLevel;
                property.CommissionTypeId = updatedProperty.CommissionTypeId;
                property.CommissionValue = updatedProperty.CommissionValue;
                property.PaymentsEnabled = updatedProperty.PaymentsEnabled;
                property.PaymentsVerify = updatedProperty.PaymentsVerify;
                property.Tags = updatedProperty.Tags;
                property.Address = updatedProperty.Address;
                property.UpdatedDate = DateTime.Now;
                property.UpdatedBy = updatedProperty.UpdatedBy;

                await _context.SaveChangesAsync();

                response.Response = property;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteProperty(int id, int companyId, ApplicationUser user)
        {
            var response = new ResponseModel();

            try
            {
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                // Soft delete
                property.IsRemoved = true;
                property.RemovedDate = DateTime.Now;
                property.RemovedBy = user?.Id;

                await _context.SaveChangesAsync();

                response.Response = property;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllProperties(int companyId)
        {
            var response = new ResponseModel();

            try
            {
                var properties = await _context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.Images)
                    .Where(p => p.CompanyId == companyId && !p.IsRemoved)
                    .OrderBy(p => p.PropertyName)
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertiesByOwner(int ownerId)
        {
            var response = new ResponseModel();

            try
            {
                var properties = await _context.Properties
                    .Include(p => p.Status)
                    .Include(p => p.Images)
                    .Where(p => p.OwnerId == ownerId && !p.IsRemoved)
                    .OrderBy(p => p.PropertyName)
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties by owner");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        // Image Management
        public async Task<ResponseModel> UploadPropertyImage(int propertyId, IFormFile file, int imageTypeId, string description = null, ApplicationUser user = null)
        {
            var response = new ResponseModel();

            try
            {
                var property = await _context.Properties
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == propertyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                // Upload to CDN
                var cdnPath = $"company-{property.CompanyId}/properties/{property.Id}/images";
                string cdnUrl;

                // Create base64 backup for images
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

                // Get display order
                var maxOrder = property.Images.Any() ? property.Images.Max(i => i.DisplayOrder) : 0;

                // Create image record
                var propertyImage = new PropertyImage
                {
                    PropertyId = propertyId,
                    FileName = file.FileName,
                    Description = description,
                    ImageTypeId = imageTypeId,
                    DisplayOrder = maxOrder + 1,
                    CdnUrl = cdnUrl,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    HasBase64Backup = true,
                    UploadedOn = DateTime.Now,
                    UploadedBy = user?.Id ?? "System",
                    IsActive = true
                };

                _context.PropertyImages.Add(propertyImage);

                // If this is the first image, set it as main
                if (!property.Images.Any())
                {
                    property.MainImageUrl = cdnUrl;
                }

                await _context.SaveChangesAsync();

                response.Response = propertyImage;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Image uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading property image");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the image: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeletePropertyImage(int imageId, int companyId, ApplicationUser user)
        {
            var response = new ResponseModel();

            try
            {
                var image = await _context.PropertyImages
                    .Include(i => i.Property)
                    .FirstOrDefaultAsync(i => i.Id == imageId && i.Property.CompanyId == companyId);

                if (image == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Image not found.";
                    return response;
                }

                // Delete from CDN
                await _cdnService.DeleteFileAsync(image.CdnUrl);

                // Remove from database
                _context.PropertyImages.Remove(image);

                // If this was the main image, update to the next available
                if (image.Property.MainImageUrl == image.CdnUrl)
                {
                    var nextImage = await _context.PropertyImages
                        .Where(i => i.PropertyId == image.PropertyId && i.Id != imageId)
                        .OrderBy(i => i.DisplayOrder)
                        .FirstOrDefaultAsync();

                    image.Property.MainImageUrl = nextImage?.CdnUrl;
                }

                await _context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Image deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property image");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the image: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> SetMainImage(int propertyId, int imageId, int companyId)
        {
            var response = new ResponseModel();

            try
            {
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.Id == propertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                var image = await _context.PropertyImages
                    .FirstOrDefaultAsync(i => i.Id == imageId && i.PropertyId == propertyId);

                if (image == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Image not found.";
                    return response;
                }

                property.MainImageUrl = image.CdnUrl;
                await _context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Main image updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting main image");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while setting the main image: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyImages(int propertyId)
        {
            var response = new ResponseModel();

            try
            {
                var images = await _context.PropertyImages
                    .Include(i => i.ImageType)
                    .Where(i => i.PropertyId == propertyId && i.IsActive)
                    .OrderBy(i => i.DisplayOrder)
                    .ToListAsync();

                response.Response = images;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Images retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property images");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving images: " + ex.Message;
            }

            return response;
        }

        // Document Management
        public async Task<ResponseModel> UploadPropertyDocument(int propertyId, IFormFile file, int documentTypeId, int categoryId, string description = null, ApplicationUser user = null)
        {
            var response = new ResponseModel();

            try
            {
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.Id == propertyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                // Upload to CDN
                var cdnPath = $"company-{property.CompanyId}/properties/{property.Id}/documents";
                string cdnUrl;

                using (var stream = file.OpenReadStream())
                {
                    // Documents might be sensitive, so we definitely want base64 backup
                    cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        "properties",
                        cdnPath
                    );
                }

                // Create document record
                var document = new PropertyDocument
                {
                    PropertyId = propertyId,
                    CompanyId = property.CompanyId,
                    FileName = file.FileName,
                    Description = description,
                    DocumentTypeId = documentTypeId,
                    CategoryId = categoryId,
                    CdnUrl = cdnUrl,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    HasBase64Backup = true,
                    AccessLevelId = 2, // Default to Internal
                    Version = 1,
                    IsCurrentVersion = true,
                    UploadedOn = DateTime.Now,
                    UploadedBy = user?.Id ?? "System",
                    IsActive = true
                };

                _context.PropertyDocuments.Add(document);
                await _context.SaveChangesAsync();

                response.Response = document;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading property document");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the document: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeletePropertyDocument(int documentId, int companyId, ApplicationUser user)
        {
            var response = new ResponseModel();

            try
            {
                var document = await _context.PropertyDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.CompanyId == companyId);

                if (document == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Document not found.";
                    return response;
                }

                // Soft delete
                document.IsActive = false;
                document.UpdatedDate = DateTime.Now;
                document.UpdatedBy = user?.Id;

                await _context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property document");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the document: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyDocuments(int propertyId, int? categoryId = null)
        {
            var response = new ResponseModel();

            try
            {
                var query = _context.PropertyDocuments
                    .Include(d => d.DocumentType)
                    .Include(d => d.Category)
                    .Include(d => d.AccessLevel)
                    .Where(d => d.PropertyId == propertyId && d.IsActive);

                if (categoryId.HasValue)
                {
                    query = query.Where(d => d.CategoryId == categoryId.Value);
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
                _logger.LogError(ex, "Error retrieving property documents");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving documents: " + ex.Message;
            }

            return response;
        }

        // Beneficiary Management
        public async Task<ResponseModel> AddBeneficiary(PropertyBeneficiary beneficiary)
        {
            var response = new ResponseModel();

            try
            {
                beneficiary.CreatedOn = DateTime.Now;
                beneficiary.IsActive = true;

                _context.PropertyBeneficiaries.Add(beneficiary);
                await _context.SaveChangesAsync();

                response.Response = beneficiary;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding beneficiary");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the beneficiary: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateBeneficiary(int id, PropertyBeneficiary updatedBeneficiary)
        {
            var response = new ResponseModel();

            try
            {
                var beneficiary = await _context.PropertyBeneficiaries
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
                    return response;
                }

                // Update fields
                beneficiary.Name = updatedBeneficiary.Name;
                beneficiary.EmailAddress = updatedBeneficiary.EmailAddress;
                beneficiary.Mobile = updatedBeneficiary.Mobile;
                beneficiary.Phone = updatedBeneficiary.Phone;
                beneficiary.Address = updatedBeneficiary.Address;
                beneficiary.BenTypeId = updatedBeneficiary.BenTypeId;
                beneficiary.CommissionTypeId = updatedBeneficiary.CommissionTypeId;
                beneficiary.CommissionValue = updatedBeneficiary.CommissionValue;
                beneficiary.BenStatusId = updatedBeneficiary.BenStatusId;
                beneficiary.NotifyEmail = updatedBeneficiary.NotifyEmail;
                beneficiary.NotifySMS = updatedBeneficiary.NotifySMS;
                beneficiary.BankAccount = updatedBeneficiary.BankAccount;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = updatedBeneficiary.UpdatedBy;

                await _context.SaveChangesAsync();

                response.Response = beneficiary;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating beneficiary");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the beneficiary: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteBeneficiary(int id, int companyId, ApplicationUser user)
        {
            var response = new ResponseModel();

            try
            {
                var beneficiary = await _context.PropertyBeneficiaries
                    .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
                    return response;
                }

                // Soft delete
                beneficiary.IsActive = false;
                beneficiary.UpdatedDate = DateTime.Now;
                beneficiary.UpdatedBy = user?.Id;

                await _context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting beneficiary");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the beneficiary: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyBeneficiaries(int propertyId)
        {
            var response = new ResponseModel();

            try
            {
                var beneficiaries = await _context.PropertyBeneficiaries
                    .Include(b => b.BenType)
                    .Include(b => b.CommissionType)
                    .Include(b => b.BenStatus)
                    .Where(b => b.PropertyId == propertyId && b.IsActive)
                    .ToListAsync();

                response.Response = beneficiaries;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiaries retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving beneficiaries");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving beneficiaries: " + ex.Message;
            }

            return response;
        }

        // Tenant Management
        public async Task<ResponseModel> AddTenant(PropertyTenant tenant)
        {
            var response = new ResponseModel();

            try
            {
                // Check if property already has an active tenant
                var existingActiveTenant = await _context.PropertyTenants
                    .FirstOrDefaultAsync(t => t.PropertyId == tenant.PropertyId && 
                                            t.StatusId == 2 && // Active status
                                            !t.IsRemoved);

                if (existingActiveTenant != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property already has an active tenant.";
                    return response;
                }

                tenant.CreatedOn = DateTime.Now;
                tenant.StatusId = 2; // Set as active

                _context.PropertyTenants.Add(tenant);

                // Update property status
                var property = await _context.Properties.FindAsync(tenant.PropertyId);
                if (property != null)
                {
                    property.HasTenant = true;
                    property.CurrentTenantId = Guid.NewGuid(); // You might want to change this based on your ID system
                    property.CurrentLeaseStartDate = tenant.LeaseStartDate;
                    property.LeaseEndDate = tenant.LeaseEndDate;
                }

                await _context.SaveChangesAsync();

                response.Response = tenant;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tenant");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the tenant: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant)
        {
            var response = new ResponseModel();

            try
            {
                var tenant = await _context.PropertyTenants
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tenant == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Tenant not found.";
                    return response;
                }

                // Update tenant information
                tenant.FirstName = updatedTenant.FirstName;
                tenant.LastName = updatedTenant.LastName;
                tenant.EmailAddress = updatedTenant.EmailAddress;
                tenant.MobileNumber = updatedTenant.MobileNumber;
                tenant.LeaseStartDate = updatedTenant.LeaseStartDate;
                tenant.LeaseEndDate = updatedTenant.LeaseEndDate;
                tenant.RentAmount = updatedTenant.RentAmount;
                tenant.DepositAmount = updatedTenant.DepositAmount;
                tenant.DebitDayOfMonth = updatedTenant.DebitDayOfMonth;
                tenant.StatusId = updatedTenant.StatusId;
                tenant.UpdatedDate = DateTime.Now;
                tenant.UpdatedBy = updatedTenant.UpdatedBy;

                await _context.SaveChangesAsync();

                response.Response = tenant;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the tenant: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteTenant(int id, int companyId, ApplicationUser user)
        {
            var response = new ResponseModel();

            try
            {
                var tenant = await _context.PropertyTenants
                    .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);

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

                // Update property if this was the active tenant
                if (tenant.StatusId == 2) // Active status
                {
                    var property = await _context.Properties.FindAsync(tenant.PropertyId);
                    if (property != null)
                    {
                        property.HasTenant = false;
                        property.CurrentTenantId = null;
                    }
                }

                await _context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the tenant: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyTenants(int propertyId)
        {
            var response = new ResponseModel();

            try
            {
                var tenants = await _context.PropertyTenants
                    .Include(t => t.Status)
                    .Where(t => t.PropertyId == propertyId && !t.IsRemoved)
                    .OrderByDescending(t => t.LeaseStartDate)
                    .ToListAsync();

                response.Response = tenants;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenants retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenants");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving tenants: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetActiveTenant(int propertyId)
        {
            var response = new ResponseModel();

            try
            {
                var tenant = await _context.PropertyTenants
                    .Include(t => t.Status)
                    .FirstOrDefaultAsync(t => t.PropertyId == propertyId && 
                                            t.StatusId == 2 && // Active status
                                            !t.IsRemoved);

                if (tenant == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "No active tenant found for this property.";
                    return response;
                }

                response.Response = tenant;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Active tenant retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active tenant");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the active tenant: " + ex.Message;
            }

            return response;
        }

        // Helper Methods
        private async Task<string> GeneratePropertyCode(int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);
            var prefix = company?.Name.Substring(0, 3).ToUpper() ?? "PROP";
            
            var lastPropertyCode = await _context.Properties
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.Id)
                .Select(p => p.PropertyCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastPropertyCode))
            {
                var lastNumber = lastPropertyCode.Substring(lastPropertyCode.Length - 4);
                if (int.TryParse(lastNumber, out int number))
                {
                    nextNumber = number + 1;
                }
            }

            return $"{prefix}-{nextNumber:D4}";
        }
    }
}