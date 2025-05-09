using Roovia.Models.Helper;
using Roovia.Models.Users;

namespace Roovia.Interfaces
{
    public interface IUser
    {
        // User methods (Identity-related)
        Task<ResponseModel> GetUserById(string id);
        Task<ResponseModel> UpdateUser(string id, ApplicationUser updatedUser); // Changed from int to string
        Task<ResponseModel> DeleteUser(string id); // Changed from int to string
        Task<ResponseModel> GetAllUsers();
        Task<ResponseModel> UpdateUserCompanyId(string userId, int companyId);
        Task<ResponseModel> UpdateUserRole(string userId, int roleValue); // Changed SystemRole to int
        Task<ResponseModel> UpdateUserBranch(string userId, int branchId);
        Task<ResponseModel> GetUsersByBranch(int branchId);

        Task<ResponseModel> GetAuthenticatedUserInfo();

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
        Task<ResponseModel> ResetUserPassword(string userId, bool requireChange = true);
    }
}