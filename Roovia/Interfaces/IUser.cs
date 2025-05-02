using Roovia.Models.Helper;
using Roovia.Models.Users;

namespace Roovia.Interfaces
{
    public interface IUser
    {
        // User methods (Identity-related)
        Task<ResponseModel> GetUserById(int id);
        Task<ResponseModel> UpdateUser(int id, ApplicationUser updatedUser);
        Task<ResponseModel> DeleteUser(int id);
        Task<ResponseModel> GetAllUsers();
        Task<ResponseModel> UpdateUserCompanyId(string userId, int companyId);
        Task<ResponseModel> UpdateUserRole(string userId, UserRole role);
        Task<ResponseModel> UpdateUserBranch(string userId, int branchId);
        Task<ResponseModel> GetUsersByBranch(int branchId);

        // Company methods
        Task<ResponseModel> CreateCompany(Company company);
        Task<ResponseModel> GetCompanyById(int id);
        Task<ResponseModel> UpdateCompany(int id, Company updatedCompany);
        Task<ResponseModel> DeleteCompany(int id);
        Task<ResponseModel> GetAllCompanies();

        // Branch methods
        Task<ResponseModel> CreateBranch(Branch branch);
        Task<ResponseModel> GetBranchById(int id);
        Task<ResponseModel> GetBranchesByCompany(int companyId);
        Task<ResponseModel> UpdateBranch(int id, Branch updatedBranch);
        Task<ResponseModel> DeleteBranch(int id);

        // Contact methods
        Task<ResponseModel> AddEmailAddress(Email email);
        Task<ResponseModel> UpdateEmailAddress(int id, Email email);
        Task<ResponseModel> DeleteEmailAddress(int id);
        Task<ResponseModel> AddContactNumber(ContactNumber contactNumber);
        Task<ResponseModel> UpdateContactNumber(int id, ContactNumber contactNumber);
        Task<ResponseModel> DeleteContactNumber(int id);
    }
}