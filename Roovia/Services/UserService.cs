using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.UserCompanyModels;
using System.Security.Claims;
using System.Text;

namespace Roovia.Services
{
    public class UserService : IUser
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            AuthenticationStateProvider authStateProvider,
            UserManager<ApplicationUser> userManager,
            ILogger<UserService> logger)
        {
            _contextFactory = contextFactory;
            _authStateProvider = authStateProvider;
            _userManager = userManager;
            _logger = logger;
        }

        #region User Methods

        public async Task<ResponseModel> GetUserById(string id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users
                    .Include(u => u.EmailAddresses.Where(e => e.IsActive))
                    .Include(u => u.ContactNumbers.Where(c => c.IsActive))
                    .Include(u => u.Company)
                    .Include(u => u.Branch)
                    .Include(u => u.Status)
                    .Include(u => u.ProfilePicture)
                    .Include(u => u.CustomRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user != null)
                {
                    response.Response = user;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "User retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the user: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllUsers()
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var users = await context.Users
                    .Include(u => u.EmailAddresses.Where(e => e.IsActive))
                    .Include(u => u.ContactNumbers.Where(c => c.IsActive))
                    .Include(u => u.Company)
                    .Include(u => u.Branch)
                    .Include(u => u.Status)
                    .ToListAsync();

                response.Response = users;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Users retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving users: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetUsersByCompany(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var users = await context.Users
                    .Include(u => u.EmailAddresses.Where(e => e.IsActive))
                    .Include(u => u.ContactNumbers.Where(c => c.IsActive))
                    .Include(u => u.Branch)
                    .Include(u => u.Status)
                    .Where(u => u.CompanyId == companyId)
                    .ToListAsync();

                response.Response = users;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Users retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving users: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetUsersByBranch(int branchId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var users = await context.Users
                    .Include(u => u.EmailAddresses.Where(e => e.IsActive))
                    .Include(u => u.ContactNumbers.Where(c => c.IsActive))
                    .Include(u => u.Company)
                    .Include(u => u.Status)
                    .Where(u => u.BranchId == branchId)
                    .ToListAsync();

                response.Response = users;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Users retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for branch {BranchId}", branchId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving users: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUser(string id, ApplicationUser updatedUser)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users.FindAsync(id);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                // Update user properties
                user.FirstName = updatedUser.FirstName;
                user.LastName = updatedUser.LastName;
                user.IdNumber = updatedUser.IdNumber;
                user.CompanyId = updatedUser.CompanyId;
                user.BranchId = updatedUser.BranchId;
                user.ProfilePictureId = updatedUser.ProfilePictureId;
                user.EmployeeNumber = updatedUser.EmployeeNumber;
                user.JobTitle = updatedUser.JobTitle;
                user.Department = updatedUser.Department;
                user.HireDate = updatedUser.HireDate;
                user.Role = updatedUser.Role;
                user.StatusId = updatedUser.StatusId;
                user.IsActive = updatedUser.IsActive;
                user.IsEmailNotificationsEnabled = updatedUser.IsEmailNotificationsEnabled;
                user.IsSmsNotificationsEnabled = updatedUser.IsSmsNotificationsEnabled;
                user.IsPushNotificationsEnabled = updatedUser.IsPushNotificationsEnabled;
                user.UserPreferences = updatedUser.UserPreferences;
                user.UpdatedDate = DateTime.Now;
                user.UpdatedBy = updatedUser.UpdatedBy;

                // Update Identity properties
                if (!string.IsNullOrEmpty(updatedUser.UserName))
                {
                    user.UserName = updatedUser.UserName;
                    user.NormalizedUserName = updatedUser.UserName.ToUpper();
                }

                if (!string.IsNullOrEmpty(updatedUser.Email))
                {
                    user.Email = updatedUser.Email;
                    user.NormalizedEmail = updatedUser.Email.ToUpper();
                }

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteUser(string id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users.FindAsync(id);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                // Soft delete
                user.IsRemoved = true;
                user.RemovedDate = DateTime.Now;
                user.IsActive = false;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the user: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUserRole(string userId, SystemRole role)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                user.Role = role;
                user.UpdatedDate = DateTime.Now;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User role updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role for {UserId}", userId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user role: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AssignUserRole(string userId, SystemRole role)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // First ensure the user exists
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                // Map SystemRole enum to the actual role names in the database
                string roleName = role switch
                {
                    SystemRole.SystemAdministrator => "System Administrator",
                    SystemRole.CompanyAdministrator => "Company Administrator",
                    SystemRole.BranchManager => "Branch Manager",
                    SystemRole.PropertyManager => "Property Manager",
                    SystemRole.FinancialOfficer => "Financial Officer",
                    SystemRole.TenantOfficer => "Tenant Officer",
                    SystemRole.ReportsViewer => "Reports Viewer",
                    _ => role.ToString()
                };

                var customRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive);

                if (customRole == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Role {roleName} not found in Roles table.";
                    return response;
                }

                // Check if the user already has this role assignment
                var existingAssignment = await context.UserRoleAssignments
                    .FirstOrDefaultAsync(ura => ura.UserId == userId && ura.RoleId == customRole.Id);

                if (existingAssignment != null)
                {
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "User already has this role assigned.";
                    return response;
                }

                // Create the new role assignment
                var roleAssignment = new UserRoleAssignment
                {
                    UserId = userId,
                    RoleId = customRole.Id,
                    AssignedDate = DateTime.Now,
                    AssignedBy = userId, // In registration, user assigns their own role
                    ExpiryDate = null, // No expiry for company admin during registration
                    IsActive = true
                };

                await context.UserRoleAssignments.AddAsync(roleAssignment);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Role assigned successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user {UserId}", userId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while assigning the role: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUserCompanyId(string userId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                user.CompanyId = companyId;
                user.UpdatedDate = DateTime.Now;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User's company ID updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user company ID for {UserId}", userId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user's company ID: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUserBranchId(string userId, int branchId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                var branch = await context.Branches
                    .Include(b => b.Company)
                    .FirstOrDefaultAsync(b => b.Id == branchId);

                if (branch == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Branch not found.";
                    return response;
                }

                user.BranchId = branchId;
                user.CompanyId = branch.CompanyId; // Ensure company is also updated
                user.UpdatedDate = DateTime.Now;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User branch assignment updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user branch for {UserId}", userId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user's branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAuthenticatedUserInfo()
        {
            ResponseModel response = new();

            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity != null && user.Identity.IsAuthenticated)
                {
                    var userId = user.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                    if (!string.IsNullOrEmpty(userId))
                    {
                        using var context = await _contextFactory.CreateDbContextAsync();

                        var applicationUser = await context.Users
                            .Include(u => u.EmailAddresses.Where(e => e.IsActive))
                            .Include(u => u.ContactNumbers.Where(c => c.IsActive))
                            .Include(u => u.Company)
                            .Include(u => u.Branch)
                            .Include(u => u.Status)
                            .Include(u => u.ProfilePicture)
                            .FirstOrDefaultAsync(u => u.Id == userId);

                        if (applicationUser != null)
                        {
                            response.Response = applicationUser;
                            response.ResponseInfo.Success = true;
                            response.ResponseInfo.Message = "Authenticated user retrieved successfully.";
                        }
                        else
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = "Authenticated user not found.";
                        }
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "User ID not found in claims.";
                    }
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User is not authenticated.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authenticated user info");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving authenticated user info: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> ResetUserPassword(string userId, bool requireChange = true)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                // Generate a random password
                string newPassword = GenerateRandomPassword(12);

                // Update the user's password hash
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

                if (!result.Succeeded)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Failed to reset password: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return response;
                }

                // If requireChange is true, set the flag to force password change on next login
                if (requireChange)
                {
                    user.RequireChangePasswordOnLogin = true;
                }

                // Update the last modified date
                user.UpdatedDate = DateTime.Now;
                await context.SaveChangesAsync();

                // Return the new password
                response.Response = newPassword;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Password reset successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while resetting the password: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetUserWithDetails(string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users
                    .Include(u => u.EmailAddresses)
                    .Include(u => u.ContactNumbers)
                    .Include(u => u.Company)
                        .ThenInclude(c => c.EmailAddresses)
                    .Include(u => u.Company)
                        .ThenInclude(c => c.ContactNumbers)
                    .Include(u => u.Branch)
                        .ThenInclude(b => b.EmailAddresses)
                    .Include(u => u.Branch)
                        .ThenInclude(b => b.ContactNumbers)
                    .Include(u => u.Status)
                    .Include(u => u.ProfilePicture)
                    .Include(u => u.CustomRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.PermissionOverrides)
                        .ThenInclude(po => po.Permission)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    response.Response = user;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "User with details retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with details {UserId}", userId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the user: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Company Methods

        public async Task<ResponseModel> CreateCompany(Company company)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                if (company.CreatedOn == default)
                    company.CreatedOn = DateTime.Now;

                await context.Companies.AddAsync(company);
                await context.SaveChangesAsync();

                response.Response = company;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Company created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the company: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetCompanyById(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var company = await context.Companies
                    .Include(c => c.EmailAddresses.Where(e => e.IsActive))
                    .Include(c => c.ContactNumbers.Where(c => c.IsActive))
                    .Include(c => c.Branches)
                    .Include(c => c.Users)
                    .Include(c => c.Status)
                    .Include(c => c.MainLogo)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company != null)
                {
                    response.Response = company;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Company retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Company not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company {CompanyId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the company: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllCompanies()
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var companies = await context.Companies
                    .Include(c => c.EmailAddresses.Where(e => e.IsActive))
                    .Include(c => c.ContactNumbers.Where(c => c.IsActive))
                    .Include(c => c.Status)
                    .ToListAsync();

                response.Response = companies;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Companies retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all companies");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving companies: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateCompany(int id, Company updatedCompany)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var company = await context.Companies.FindAsync(id);

                if (company == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Company not found.";
                    return response;
                }

                // Update company properties
                company.Name = updatedCompany.Name;
                company.RegistrationNumber = updatedCompany.RegistrationNumber;
                company.Website = updatedCompany.Website;
                company.VatNumber = updatedCompany.VatNumber;
                company.MainLogoId = updatedCompany.MainLogoId;
                company.Address = updatedCompany.Address;
                company.BankAccount = updatedCompany.BankAccount;
                company.StatusId = updatedCompany.StatusId;
                company.IsActive = updatedCompany.IsActive;
                company.SubscriptionPlanId = updatedCompany.SubscriptionPlanId;
                company.SubscriptionStartDate = updatedCompany.SubscriptionStartDate;
                company.SubscriptionEndDate = updatedCompany.SubscriptionEndDate;
                company.IsTrialPeriod = updatedCompany.IsTrialPeriod;
                company.MaxUsers = updatedCompany.MaxUsers;
                company.MaxProperties = updatedCompany.MaxProperties;
                company.MaxBranches = updatedCompany.MaxBranches;
                company.Settings = updatedCompany.Settings;
                company.Tags = updatedCompany.Tags;
                company.UpdatedDate = DateTime.Now;
                company.UpdatedBy = updatedCompany.UpdatedBy;

                await context.SaveChangesAsync();

                response.Response = company;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Company updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the company: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteCompany(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var company = await context.Companies
                    .Include(c => c.Branches)
                        .ThenInclude(b => b.Users)
                    .Include(c => c.Users)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Company not found.";
                    return response;
                }

                // Check if there are users or branches associated
                if ((company.Users != null && company.Users.Any()) ||
                    (company.Branches != null && company.Branches.Any()))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete company with associated users or branches.";
                    return response;
                }

                // Soft delete
                company.IsRemoved = true;
                company.RemovedDate = DateTime.Now;
                company.IsActive = false;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Company deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company {CompanyId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the company: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetCompanyWithDetails(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var company = await context.Companies
                    .Include(c => c.EmailAddresses)
                    .Include(c => c.ContactNumbers)
                    .Include(c => c.Branches)
                        .ThenInclude(b => b.EmailAddresses)
                    .Include(c => c.Branches)
                        .ThenInclude(b => b.ContactNumbers)
                    .Include(c => c.Branches)
                        .ThenInclude(b => b.Users)
                    .Include(c => c.Users)
                    .Include(c => c.Status)
                    .Include(c => c.MainLogo)
                    .FirstOrDefaultAsync(c => c.Id == companyId);

                if (company != null)
                {
                    response.Response = company;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Company with details retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Company not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company with details {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the company: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Branch Methods

        public async Task<ResponseModel> CreateBranch(Branch branch)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Ensure company exists
                var company = await context.Companies.FindAsync(branch.CompanyId);
                if (company == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Company not found.";
                    return response;
                }

                if (branch.CreatedOn == default)
                    branch.CreatedOn = DateTime.Now;

                await context.Branches.AddAsync(branch);
                await context.SaveChangesAsync();

                response.Response = branch;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Branch created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetBranchById(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var branch = await context.Branches
                    .Include(b => b.Company)
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.Users)
                    .Include(b => b.Status)
                    .Include(b => b.MainLogo)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (branch != null)
                {
                    response.Response = branch;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Branch retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Branch not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch {BranchId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetBranchesByCompany(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var branches = await context.Branches
                    .Include(b => b.EmailAddresses.Where(e => e.IsActive))
                    .Include(b => b.ContactNumbers.Where(c => c.IsActive))
                    .Include(b => b.Status)
                    .Where(b => b.CompanyId == companyId)
                    .ToListAsync();

                response.Response = branches;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Branches retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving branches: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateBranch(int id, Branch updatedBranch)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var branch = await context.Branches.FindAsync(id);

                if (branch == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Branch not found.";
                    return response;
                }

                // Update branch properties
                branch.Name = updatedBranch.Name;
                branch.Code = updatedBranch.Code;
                branch.MainLogoId = updatedBranch.MainLogoId;
                branch.Address = updatedBranch.Address;
                branch.BankAccount = updatedBranch.BankAccount;
                branch.StatusId = updatedBranch.StatusId;
                branch.IsActive = updatedBranch.IsActive;
                branch.IsHeadOffice = updatedBranch.IsHeadOffice;
                branch.Settings = updatedBranch.Settings;
                branch.MaxUsers = updatedBranch.MaxUsers;
                branch.MaxProperties = updatedBranch.MaxProperties;
                branch.Tags = updatedBranch.Tags;
                branch.UpdatedDate = DateTime.Now;
                branch.UpdatedBy = updatedBranch.UpdatedBy;

                await context.SaveChangesAsync();

                response.Response = branch;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Branch updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch {BranchId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteBranch(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var branch = await context.Branches
                    .Include(b => b.Users)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (branch == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Branch not found.";
                    return response;
                }

                // Check if there are users associated
                if (branch.Users != null && branch.Users.Any())
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete branch with associated users.";
                    return response;
                }

                // Soft delete
                branch.IsRemoved = true;
                branch.RemovedDate = DateTime.Now;
                branch.IsActive = false;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Branch deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch {BranchId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetBranchWithDetails(int branchId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var branch = await context.Branches
                    .Include(b => b.Company)
                    .Include(b => b.EmailAddresses)
                    .Include(b => b.ContactNumbers)
                    .Include(b => b.Users)
                        .ThenInclude(u => u.EmailAddresses)
                    .Include(b => b.Users)
                        .ThenInclude(u => u.ContactNumbers)
                    .Include(b => b.Status)
                    .Include(b => b.MainLogo)
                    .FirstOrDefaultAsync(b => b.Id == branchId);

                if (branch != null)
                {
                    response.Response = branch;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Branch with details retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Branch not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch with details {BranchId}", branchId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the branch: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Contact Methods

        public async Task<ResponseModel> AddEmailAddress(Email email)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Validate entity exists
                bool entityExists = await ValidateEntityExists(context, email.RelatedEntityType,
                    email.RelatedEntityId, email.RelatedEntityStringId);

                if (!entityExists)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"{email.RelatedEntityType} not found.";
                    return response;
                }

                // If marking as primary, update existing primary emails
                if (email.IsPrimary)
                {
                    await UnsetPrimaryEmails(context, email.RelatedEntityType,
                        email.RelatedEntityId, email.RelatedEntityStringId);
                }

                email.CreatedOn = DateTime.Now;
                await context.Emails.AddAsync(email);
                await context.SaveChangesAsync();

                response.Response = email;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding email address");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the email address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateEmailAddress(int id, Email updatedEmail)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var email = await context.Emails.FindAsync(id);
                if (email == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Email address not found.";
                    return response;
                }

                // If marking as primary, update existing primary emails
                if (updatedEmail.IsPrimary && !email.IsPrimary)
                {
                    await UnsetPrimaryEmails(context, email.RelatedEntityType,
                        email.RelatedEntityId, email.RelatedEntityStringId, id);
                }

                // Update email properties
                email.EmailAddress = updatedEmail.EmailAddress;
                email.Description = updatedEmail.Description;
                email.IsPrimary = updatedEmail.IsPrimary;
                email.IsActive = updatedEmail.IsActive;
                email.UpdatedDate = DateTime.Now;
                email.UpdatedBy = updatedEmail.UpdatedBy;

                await context.SaveChangesAsync();

                response.Response = email;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email address {EmailId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the email address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteEmailAddress(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var email = await context.Emails.FindAsync(id);
                if (email == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Email address not found.";
                    return response;
                }

                // Don't allow deletion of the only primary email
                if (email.IsPrimary)
                {
                    int primaryCount = await GetPrimaryEmailCount(context, email.RelatedEntityType,
                        email.RelatedEntityId, email.RelatedEntityStringId);

                    if (primaryCount == 1)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Cannot delete the only primary email address.";
                        return response;
                    }
                }

                context.Emails.Remove(email);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email address {EmailId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the email address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetEmailAddresses(string entityType, object entityId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                IQueryable<Email> query = context.Emails.Where(e => e.RelatedEntityType == entityType);

                if (entityId is string stringId)
                {
                    query = query.Where(e => e.RelatedEntityStringId == stringId);
                }
                else if (entityId is int intId)
                {
                    query = query.Where(e => e.RelatedEntityId == intId);
                }

                var emails = await query.OrderByDescending(e => e.IsPrimary).ThenBy(e => e.CreatedOn).ToListAsync();

                response.Response = emails;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email addresses retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email addresses for {EntityType} {EntityId}", entityType, entityId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving email addresses: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddContactNumber(ContactNumber contactNumber)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Validate entity exists
                bool entityExists = await ValidateEntityExists(context, contactNumber.RelatedEntityType,
                    contactNumber.RelatedEntityId, contactNumber.RelatedEntityStringId);

                if (!entityExists)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"{contactNumber.RelatedEntityType} not found.";
                    return response;
                }

                // If marking as primary, update existing primary numbers
                if (contactNumber.IsPrimary)
                {
                    await UnsetPrimaryContactNumbers(context, contactNumber.RelatedEntityType,
                        contactNumber.RelatedEntityId, contactNumber.RelatedEntityStringId);
                }

                contactNumber.CreatedOn = DateTime.Now;
                await context.ContactNumbers.AddAsync(contactNumber);
                await context.SaveChangesAsync();

                response.Response = contactNumber;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact number");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the contact number: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateContactNumber(int id, ContactNumber updatedContactNumber)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var contactNumber = await context.ContactNumbers.FindAsync(id);
                if (contactNumber == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Contact number not found.";
                    return response;
                }

                // If marking as primary, update existing primary numbers
                if (updatedContactNumber.IsPrimary && !contactNumber.IsPrimary)
                {
                    await UnsetPrimaryContactNumbers(context, contactNumber.RelatedEntityType,
                        contactNumber.RelatedEntityId, contactNumber.RelatedEntityStringId, id);
                }

                // Update contact number properties
                contactNumber.Number = updatedContactNumber.Number;
                contactNumber.Type = updatedContactNumber.Type;
                contactNumber.Description = updatedContactNumber.Description;
                contactNumber.IsPrimary = updatedContactNumber.IsPrimary;
                contactNumber.IsActive = updatedContactNumber.IsActive;
                contactNumber.UpdatedDate = DateTime.Now;
                contactNumber.UpdatedBy = updatedContactNumber.UpdatedBy;

                await context.SaveChangesAsync();

                response.Response = contactNumber;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact number {ContactId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the contact number: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteContactNumber(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var contactNumber = await context.ContactNumbers.FindAsync(id);
                if (contactNumber == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Contact number not found.";
                    return response;
                }

                // Don't allow deletion of the only primary contact number
                if (contactNumber.IsPrimary)
                {
                    int primaryCount = await GetPrimaryContactNumberCount(context, contactNumber.RelatedEntityType,
                        contactNumber.RelatedEntityId, contactNumber.RelatedEntityStringId);

                    if (primaryCount == 1)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Cannot delete the only primary contact number.";
                        return response;
                    }
                }

                context.ContactNumbers.Remove(contactNumber);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact number {ContactId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the contact number: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetContactNumbers(string entityType, object entityId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                IQueryable<ContactNumber> query = context.ContactNumbers.Where(c => c.RelatedEntityType == entityType);

                if (entityId is string stringId)
                {
                    query = query.Where(c => c.RelatedEntityStringId == stringId);
                }
                else if (entityId is int intId)
                {
                    query = query.Where(c => c.RelatedEntityId == intId);
                }

                var contactNumbers = await query.OrderByDescending(c => c.IsPrimary).ThenBy(c => c.CreatedOn).ToListAsync();

                response.Response = contactNumbers;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact numbers retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact numbers for {EntityType} {EntityId}", entityType, entityId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving contact numbers: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Helper Methods

        private string GenerateRandomPassword(int length)
        {
            const string upperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string specialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";

            var random = new Random();
            var password = new StringBuilder();

            // Ensure at least one of each character type
            password.Append(upperChars[random.Next(upperChars.Length)]);
            password.Append(lowerChars[random.Next(lowerChars.Length)]);
            password.Append(digits[random.Next(digits.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);

            // Fill the rest of the password
            var allChars = upperChars + lowerChars + digits + specialChars;
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password characters
            return new string(password.ToString().OrderBy(c => random.Next()).ToArray());
        }

        private async Task<bool> ValidateEntityExists(ApplicationDbContext context, string entityType, int? entityId, string? entityStringId)
        {
            return entityType switch
            {
                "User" => await context.Users.AnyAsync(u => u.Id == entityStringId),
                "Company" => await context.Companies.AnyAsync(c => c.Id == entityId),
                "Branch" => await context.Branches.AnyAsync(b => b.Id == entityId),
                "PropertyOwner" => await context.PropertyOwners.AnyAsync(po => po.Id == entityId),
                "PropertyTenant" => await context.PropertyTenants.AnyAsync(pt => pt.Id == entityId),
                "PropertyBeneficiary" => await context.PropertyBeneficiaries.AnyAsync(pb => pb.Id == entityId),
                "Vendor" => await context.Vendors.AnyAsync(v => v.Id == entityId),
                _ => false
            };
        }

        private async Task UnsetPrimaryEmails(ApplicationDbContext context, string entityType, int? entityId, string? entityStringId, int? excludeId = null)
        {
            IQueryable<Email> query = context.Emails
                .Where(e => e.RelatedEntityType == entityType && e.IsPrimary);

            if (excludeId.HasValue)
                query = query.Where(e => e.Id != excludeId.Value);

            if (entityType == "User")
                query = query.Where(e => e.RelatedEntityStringId == entityStringId);
            else
                query = query.Where(e => e.RelatedEntityId == entityId);

            var emails = await query.ToListAsync();
            foreach (var email in emails)
            {
                email.IsPrimary = false;
                email.UpdatedDate = DateTime.Now;
            }
        }

        private async Task UnsetPrimaryContactNumbers(ApplicationDbContext context, string entityType, int? entityId, string? entityStringId, int? excludeId = null)
        {
            IQueryable<ContactNumber> query = context.ContactNumbers
                .Where(c => c.RelatedEntityType == entityType && c.IsPrimary);

            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);

            if (entityType == "User")
                query = query.Where(c => c.RelatedEntityStringId == entityStringId);
            else
                query = query.Where(c => c.RelatedEntityId == entityId);

            var contactNumbers = await query.ToListAsync();
            foreach (var contactNumber in contactNumbers)
            {
                contactNumber.IsPrimary = false;
                contactNumber.UpdatedDate = DateTime.Now;
            }
        }

        private async Task<int> GetPrimaryEmailCount(ApplicationDbContext context, string entityType, int? entityId, string? entityStringId)
        {
            IQueryable<Email> query = context.Emails
                .Where(e => e.RelatedEntityType == entityType && e.IsPrimary);

            if (entityType == "User")
                query = query.Where(e => e.RelatedEntityStringId == entityStringId);
            else
                query = query.Where(e => e.RelatedEntityId == entityId);

            return await query.CountAsync();
        }

        private async Task<int> GetPrimaryContactNumberCount(ApplicationDbContext context, string entityType, int? entityId, string? entityStringId)
        {
            IQueryable<ContactNumber> query = context.ContactNumbers
                .Where(c => c.RelatedEntityType == entityType && c.IsPrimary);

            if (entityType == "User")
                query = query.Where(c => c.RelatedEntityStringId == entityStringId);
            else
                query = query.Where(c => c.RelatedEntityId == entityId);

            return await query.CountAsync();
        }

        #endregion
    }
}