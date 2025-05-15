using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface IProperty
    {
        Task<ResponseModel> CreateProperty(Property property);
        Task<ResponseModel> GetPropertyById(int id, int companyId);
        Task<ResponseModel> UpdateProperty(int id, Property updatedProperty, int companyId);
        Task<ResponseModel> DeleteProperty(int id, int companyId, ApplicationUser user);
        Task<ResponseModel> GetAllProperties(int companyId);
        Task<ResponseModel> GetPropertiesByOwner(int ownerId);
        Task<ResponseModel> GetPropertiesByBranch(int branchId);
        Task<ResponseModel> GetPropertiesWithTenants(int companyId);
        Task<ResponseModel> GetVacantProperties(int companyId);
        Task<ResponseModel> UpdatePropertyStatus(int propertyId, int statusId, string userId);
        Task<ResponseModel> GetPropertyStatistics(int companyId);
    }
}