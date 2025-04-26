using Roovia.Models.Helper;
using Roovia.Models.Users;

namespace Roovia.Interfaces
{
    public interface IUser
    {
        Task<ResponseModel> GetUserById(int id);
        Task<ResponseModel> UpdateUser(int id, ApplicationUser updatedUser);
        Task<ResponseModel> DeleteUser(int id);
        Task<ResponseModel> GetAllUsers();
        Task<ResponseModel> CreateCompany(Company company);
        Task<ResponseModel> GetCompanyById(int id);
        Task<ResponseModel> UpdateCompany(int id, Company updatedCompany);
        Task<ResponseModel> DeleteCompany(int id);
        Task<ResponseModel> GetAllCompanies();
        Task<ResponseModel> UpdateUserCompanyId(int userId, int companyId);
    }
}
