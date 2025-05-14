using Roovia.Models.Helper;
using Roovia.Models.Properties;
using Roovia.Models.PropertyOwner;
using Roovia.Models.Users;

namespace Roovia.Interfaces
{
    public interface IPropertyOwner
    {
        Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner);

        Task<ResponseModel> GetPropertyOwnerById(int companyId, int id);

        Task<ResponseModel> UpdatePropertyOwner(int id, PropertyOwner updatedPropertyOwner);

        Task<ResponseModel> DeleteProperty(int id, ApplicationUser user);

        Task<ResponseModel> GetAllPropertyOwners(int companyId);
    }
}
