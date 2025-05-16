using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Roovia.Interfaces
{
    public interface IVendor
    {
        // CRUD Operations
        Task<ResponseModel> CreateVendor(Vendor vendor, int companyId);
        Task<ResponseModel> GetVendorById(int id, int companyId);
        Task<ResponseModel> UpdateVendor(int id, Vendor updatedVendor, int companyId);
        Task<ResponseModel> DeleteVendor(int id, int companyId, ApplicationUser user);

        // Listing Methods
        Task<ResponseModel> GetAllVendors(int companyId);
        Task<ResponseModel> GetActiveVendors(int companyId);
        Task<ResponseModel> GetVendorsBySpecialization(int companyId, string specialization);
        Task<ResponseModel> GetVendorsWithExpiredInsurance(int companyId);

        // Contact Management
        Task<ResponseModel> AddEmailAddress(int vendorId, Email email);
        Task<ResponseModel> AddContactNumber(int vendorId, ContactNumber contactNumber);

        // Vendor Management
        Task<ResponseModel> UpdateVendorRating(int vendorId, decimal rating, string userId);
        Task<ResponseModel> SetPreferredVendor(int vendorId, bool isPreferred, string userId);

        // Documents
        Task<ResponseModel> UploadInsuranceDocument(int vendorId, IFormFile file, string userId);
        Task<ResponseModel> GetVendorDocuments(int vendorId, int companyId);

        // Statistics
        Task<ResponseModel> GetVendorPerformanceStats(int vendorId, int companyId);
    }
}