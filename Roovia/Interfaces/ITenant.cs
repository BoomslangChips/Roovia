using Roovia.Models.Helper;
using Roovia.Models.Tenant;
using Roovia.Models.Users;

namespace Roovia.Interfaces
{
    public interface ITenant
    {
        Task<ResponseModel> CreateTenant(PropertyTenant tenant, int companyId);
        Task<ResponseModel> GetTenantById(int id, int companyId);
        Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant, int companyId);
        Task<ResponseModel> DeleteTenant(int id, int companyId, ApplicationUser user);
        Task<ResponseModel> GetAllTenants(int companyId);
        Task<ResponseModel> GetTenantWithPropertyId(int propertyId, int companyId);
    }
}
