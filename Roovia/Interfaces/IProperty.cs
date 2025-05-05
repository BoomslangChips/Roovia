using Roovia.Models.Helper;
using Roovia.Models.Properties;

namespace Roovia.Interfaces
{
    public interface IProperty
    {
        Task<ResponseModel> CreateProperty(Property property);
        Task<ResponseModel> GetPropertyById(int id);
        Task<ResponseModel> UpdateProperty(int id, Property updatedProperty);
        Task<ResponseModel> DeleteProperty(int id);
        Task<ResponseModel> GetAllProperties();
        Task<ResponseModel> GetPropertiesByOwner(int ownerId);
    }
}
