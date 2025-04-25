using Roovia.Models.Helper;
using Roovia.Models.Tenant;

namespace Roovia.Interfaces
{
    public interface ITenant
    {
        Task<ResponseModel> CreateTenant(PropertyTenant tenant);
        Task<ResponseModel> GetTenantById(int id);
        Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant);
        Task<ResponseModel> DeleteTenant(int id);
        Task<ResponseModel> GetAllTenants();
    }
}
