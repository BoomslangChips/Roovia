using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Roovia.Interfaces
{
    public interface ITenant
    {
        // CRUD Operations
        Task<ResponseModel> CreateTenant(PropertyTenant tenant, int companyId);
        Task<ResponseModel> GetTenantById(int id, int companyId);
        Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant, int companyId);
        Task<ResponseModel> DeleteTenant(int id, int companyId, ApplicationUser user);

        // Listing Methods
        Task<ResponseModel> GetAllTenants(int companyId);
        Task<ResponseModel> GetTenantsByProperty(int propertyId, int companyId);
        Task<ResponseModel> GetCurrentTenant(int propertyId, int companyId);
        Task<ResponseModel> GetTenantsInArrears(int companyId);

        // Contact Management
        Task<ResponseModel> AddEmailAddress(int tenantId, Email email);
        Task<ResponseModel> AddContactNumber(int tenantId, ContactNumber contactNumber);

        // Lease Management
        Task<ResponseModel> UpdateTenantLease(int tenantId, DateTime leaseStartDate, DateTime leaseEndDate, decimal rentAmount, string userId);
        Task<ResponseModel> LinkMoveInInspection(int tenantId, int inspectionId, string userId);

        // Document Management
        Task<ResponseModel> UploadLeaseDocument(int tenantId, IFormFile file, string userId);
        Task<ResponseModel> GetTenantDocuments(int tenantId, int companyId);
        Task<ResponseModel> UploadTenantDocument(int tenantId, IFormFile file, string category, string userId);

        // Statistics
        Task<ResponseModel> GetTenantStatistics(int companyId);
    }
}