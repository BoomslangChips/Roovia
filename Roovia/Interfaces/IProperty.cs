using Roovia.Models.Helper;
using Roovia.Models.Properties;

namespace Roovia.Interfaces
{
    public interface IProperty
    {
        Task<ResponseModel> CreateProperty(Property property);
        Task<ResponseModel> GetPropertyById(int id, int companyId);
        Task<ResponseModel> UpdateProperty(int id, Property updatedProperty, int companyId);
        Task<ResponseModel> DeleteProperty(int id, int companyId);
        Task<ResponseModel> GetAllProperties(int companyId);
        Task<ResponseModel> GetPropertiesByOwner(int ownerId);
    }
}
