using Roovia.Models.BusinessHelperModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface IUser
    {
        #region User Methods
        Task<ResponseModel> GetUserById(string id);
        Task<ResponseModel> GetAllUsers();
        Task<ResponseModel> GetUsersByCompany(int companyId);
        Task<ResponseModel> GetUsersByBranch(int branchId);
        Task<ResponseModel> UpdateUser(string id, ApplicationUser updatedUser);
        Task<ResponseModel> DeleteUser(string id);
        Task<ResponseModel> UpdateUserRole(string userId, SystemRole role);

        Task<ResponseModel> AssignUserRole(string userId, SystemRole role);
        Task<ResponseModel> UpdateUserCompanyId(string userId, int companyId);
        Task<ResponseModel> UpdateUserBranchId(string userId, int branchId);
        Task<ResponseModel> GetAuthenticatedUserInfo();
        Task<ResponseModel> ResetUserPassword(string userId, bool requireChange = true);
        Task<ResponseModel> GetUserWithDetails(string userId);
        #endregion

        #region Company Methods
        Task<ResponseModel> CreateCompany(Company company);
        Task<ResponseModel> GetCompanyById(int id);
        Task<ResponseModel> GetAllCompanies();
        Task<ResponseModel> UpdateCompany(int id, Company updatedCompany);
        Task<ResponseModel> DeleteCompany(int id);
        Task<ResponseModel> GetCompanyWithDetails(int companyId);
        #endregion

        #region Branch Methods
        Task<ResponseModel> CreateBranch(Branch branch);
        Task<ResponseModel> GetBranchById(int id);
        Task<ResponseModel> GetBranchesByCompany(int companyId);
        Task<ResponseModel> UpdateBranch(int id, Branch updatedBranch);
        Task<ResponseModel> DeleteBranch(int id);
        Task<ResponseModel> GetBranchWithDetails(int branchId);
        #endregion

        #region Contact Methods
        Task<ResponseModel> AddEmailAddress(Email email);
        Task<ResponseModel> UpdateEmailAddress(int id, Email updatedEmail);
        Task<ResponseModel> DeleteEmailAddress(int id);
        Task<ResponseModel> GetEmailAddresses(string entityType, object entityId);

        Task<ResponseModel> AddContactNumber(ContactNumber contactNumber);
        Task<ResponseModel> UpdateContactNumber(int id, ContactNumber updatedContactNumber);
        Task<ResponseModel> DeleteContactNumber(int id);
        Task<ResponseModel> GetContactNumbers(string entityType, object entityId);
        #endregion
    }
}