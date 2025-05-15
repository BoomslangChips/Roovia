using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface ITenant
    {
        Task<ResponseModel> CreateTenant(PropertyTenant tenant, int companyId);
        Task<ResponseModel> GetTenantById(int id, int companyId);
        Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant, int companyId);
        Task<ResponseModel> DeleteTenant(int id, int companyId, ApplicationUser user);
        Task<ResponseModel> GetAllTenants(int companyId);
        Task<ResponseModel> GetTenantsByProperty(int propertyId, int companyId);
        Task<ResponseModel> GetCurrentTenant(int propertyId, int companyId);
        Task<ResponseModel> AddEmailAddress(int tenantId, Email email);
        Task<ResponseModel> AddContactNumber(int tenantId, ContactNumber contactNumber);
        Task<ResponseModel> UpdateTenantLease(int tenantId, DateTime leaseStartDate, DateTime leaseEndDate, decimal rentAmount, string userId);
        Task<ResponseModel> GetTenantsInArrears(int companyId);
        Task<ResponseModel> GetTenantStatistics(int companyId);
    }
}