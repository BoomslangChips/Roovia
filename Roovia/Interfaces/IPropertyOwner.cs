using Roovia.Models.Helper;
using Roovia.Models.Properties;
using Roovia.Models.PropertyOwner;

namespace Roovia.Interfaces
{
    public interface IPropertyOwner
    {
        Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner);

        Task<ResponseModel> GetPropertyById(int id);

        Task<ResponseModel> UpdatePropertyOwner(int id, PropertyOwner updatedPropertyOwner);

        Task <ResponseModel>  DeleteProperty(int id);

        Task<ResponseModel> GetAllProperties();
    }
}
