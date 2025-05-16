using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface IPropertyOwner
    {
        // CRUD Operations
        Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner);

        Task<ResponseModel> GetPropertyOwnerById(int companyId, int id);

        Task<ResponseModel> UpdatePropertyOwner(int id, PropertyOwner updatedOwner);

        Task<ResponseModel> DeletePropertyOwner(int id, ApplicationUser user);

        // Listing Methods
        Task<ResponseModel> GetAllPropertyOwners(int companyId);

        Task<ResponseModel> GetPropertyOwnersByPage(int companyId, int pageNumber, int pageSize);

        // Contact Management
        Task<ResponseModel> AddEmailAddress(int ownerId, Email email);

        Task<ResponseModel> AddContactNumber(int ownerId, ContactNumber contactNumber);

        // Address and Banking
        Task<ResponseModel> UpdateOwnerAddress(int ownerId, Address newAddress, string userId);

        Task<ResponseModel> UpdateOwnerBankAccount(int ownerId, BankAccount bankAccount, string userId);

        // Search Methods
        Task<ResponseModel> GetOwnerByVatNumber(string vatNumber, int companyId);

        Task<ResponseModel> GetOwnerByIdNumber(string idNumber, int companyId);

        Task<ResponseModel> SearchOwners(int companyId, string searchTerm);

        // Statistics and Export
        Task<ResponseModel> GetOwnerStatistics(int companyId);

        Task<ResponseModel> ExportOwnersToExcel(int companyId);
    }
}