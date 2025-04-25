using Roovia.Models.Helper;

namespace Roovia.Interfaces
{
    public interface IUser
    {
        Task<ResponseModel> CreateUser(object user);
        Task<ResponseModel> GetUserById(int id);
        Task<ResponseModel> UpdateUser(int id, object updatedUser);
        Task<ResponseModel> DeleteUser(int id);
        Task<ResponseModel> GetAllUsers();
    }
}
