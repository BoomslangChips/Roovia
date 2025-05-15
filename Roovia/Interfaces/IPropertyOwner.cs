using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface IPropertyOwner
    {
        Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner);
        Task<ResponseModel> GetPropertyOwnerById(int companyId, int id);
        Task<ResponseModel> UpdatePropertyOwner(int id, PropertyOwner updatedPropertyOwner);
        Task<ResponseModel> DeletePropertyOwner(int id, ApplicationUser user);
        Task<ResponseModel> GetAllPropertyOwners(int companyId);
        Task<ResponseModel> AddEmailAddress(int ownerId, Email email);
        Task<ResponseModel> AddContactNumber(int ownerId, ContactNumber contactNumber);
        Task<ResponseModel> GetPropertyOwnersByPage(int companyId, int pageNumber, int pageSize);
    }
}