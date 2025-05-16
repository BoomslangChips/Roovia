using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
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
}