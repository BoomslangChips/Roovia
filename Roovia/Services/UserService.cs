using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.Helper;
using Roovia.Models.Users;
using System.Security.Claims;
using System.Text;

namespace Roovia.Services
{
    public class UserService : IUser
    {
        private readonly ApplicationDbContext _context;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly AuthenticationStateProvider _authState;

        public UserService(
            ApplicationDbContext context,
            IDbContextFactory<ApplicationDbContext> contextFactory,
            AuthenticationStateProvider authState)
        {
            _context = context;
            _contextFactory = contextFactory;
            _authState = authState;
        }

        #region User Methods (Identity-related, using scoped DbContext)

        public async Task<ResponseModel> GetUserById(string id)
        {
            ResponseModel response = new();

            try
            {
                var user = await _context.Users
                    .Include(u => u.EmailAddresses.Where(e => e.IsActive))
                    .Include(u => u.ContactNumbers.Where(c => c.IsActive))
                    .Include(u => u.Company)
                    .Include(u => u.Branch)
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
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the user: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUser(string id, ApplicationUser updatedUser)
        {
            ResponseModel response = new();

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                // Update user properties
                user.FirstName = updatedUser.FirstName;
                user.LastName = updatedUser.LastName;
                user.CompanyId = updatedUser.CompanyId;
                user.BranchId = updatedUser.BranchId;
                user.Role = updatedUser.Role; // This is now an integer in the database
                user.IsActive = updatedUser.IsActive;
                user.UserName = updatedUser.UserName;
                user.NormalizedUserName = updatedUser.UserName?.ToUpper();
                user.Email = updatedUser.Email;
                user.NormalizedEmail = updatedUser.Email?.ToUpper();
                user.PhoneNumber = updatedUser.PhoneNumber;
                user.UpdatedDate = DateTime.Now;
                user.UpdatedBy = updatedUser.UpdatedBy;

                // Only update other properties if they are not null
                if (!string.IsNullOrEmpty(updatedUser.PasswordHash))
                    user.PasswordHash = updatedUser.PasswordHash;

                // Save changes
                await _context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User updated successfully.";
            }
            catch (Exception ex)
            {
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
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                // Delete related emails and contact numbers using string ID
                var emails = _context.Emails.Where(e => e.RelatedEntityType == "User" && e.RelatedEntityStringId == id);
                _context.Emails.RemoveRange(emails);

                var contactNumbers = _context.ContactNumbers.Where(c => c.RelatedEntityType == "User" && c.RelatedEntityStringId == id);
                _context.ContactNumbers.RemoveRange(contactNumbers);

                // Remove the user
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User deleted successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the user: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllUsers()
        {
            ResponseModel response = new();

            try
            {
                var users = await _context.Users
                    .Include(u => u.EmailAddresses.Where(e => e.IsActive))
                    .Include(u => u.ContactNumbers.Where(c => c.IsActive))
                    .Include(u => u.Company)
                    .Include(u => u.Branch)
                    .ToListAsync();

                response.Response = users;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Users retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving users: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUserRole(string userId, int roleValue)
        {
            ResponseModel response = new();

            //try
            //{
            //    var user = await _context.Users.FindAsync(userId);
            //    if (user == null)
            //    {
            //        response.ResponseInfo.Success = false;
            //        response.ResponseInfo.Message = "User not found.";
            //        return response;
            //    }

            //    user.Role = roleValue; // Using int directly
            //    user.UpdatedDate = DateTime.Now;

            //    await _context.SaveChangesAsync();

            //    response.ResponseInfo.Success = true;
            //    response.ResponseInfo.Message = "User role updated successfully.";
            //}
            //catch (Exception ex)
            //{
            //    response.ResponseInfo.Success = false;
            //    response.ResponseInfo.Message = "An error occurred while updating the user role: " + ex.Message;
            //}

            return response;
        }

        public async Task<ResponseModel> UpdateUserCompanyId(string userId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                user.CompanyId = companyId;
                user.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User's company ID updated successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user's company ID: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUserBranch(string userId, int branchId)
        {
            ResponseModel response = new();

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                var branch = await _context.Branches
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

                await _context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User branch assignment updated successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user's branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetUsersByBranch(int branchId)
        {
            ResponseModel response = new();

            try
            {
                var users = await _context.Users
                    .Include(u => u.EmailAddresses.Where(e => e.IsActive))
                    .Include(u => u.ContactNumbers.Where(c => c.IsActive))
                    .Where(u => u.BranchId == branchId)
                    .ToListAsync();

                response.Response = users;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Users retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving users: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Company Methods (Using DbContextFactory)

        public async Task<ResponseModel> CreateCompany(Company company)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Set creation date if not already set
                if (!company.CreatedOn.HasValue)
                    company.CreatedOn = DateTime.Now;

                // Add the company
                await context.Companies.AddAsync(company);
                await context.SaveChangesAsync();

                // Process email addresses
                if (company.EmailAddresses != null && company.EmailAddresses.Any())
                {
                    foreach (var email in company.EmailAddresses)
                    {
                        email.SetRelatedEntity("Company", company.Id);
                        email.CreatedOn = DateTime.Now;
                    }
                    await context.Emails.AddRangeAsync(company.EmailAddresses);
                }

                // Process contact numbers
                if (company.ContactNumbers != null && company.ContactNumbers.Any())
                {
                    foreach (var contact in company.ContactNumbers)
                    {
                        contact.SetRelatedEntity("Company", company.Id);
                        contact.CreatedOn = DateTime.Now;
                    }
                    await context.ContactNumbers.AddRangeAsync(company.ContactNumbers);
                }

                await context.SaveChangesAsync();

                response.Response = company;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Company created successfully.";
            }
            catch (Exception ex)
            {
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
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the company: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateCompany(int id, Company updatedCompany)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var company = await context.Companies
                    .Include(c => c.EmailAddresses)
                    .Include(c => c.ContactNumbers)
                    .FirstOrDefaultAsync(c => c.Id == id);

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
                company.Address = updatedCompany.Address;
                company.IsActive = updatedCompany.IsActive;
                company.UpdatedDate = DateTime.Now;
                company.UpdatedBy = updatedCompany.UpdatedBy;

                // Handle email updates
                if (updatedCompany.EmailAddresses != null)
                {
                    // Remove existing emails that aren't in the updated list
                    var emailsToRemove = company.EmailAddresses
                        .Where(e => !updatedCompany.EmailAddresses.Any(ue => ue.Id == e.Id && ue.Id != 0))
                        .ToList();

                    foreach (var email in emailsToRemove)
                    {
                        context.Emails.Remove(email);
                    }

                    // Update existing or add new emails
                    foreach (var updatedEmail in updatedCompany.EmailAddresses)
                    {
                        if (updatedEmail.Id != 0)
                        {
                            // Update existing
                            var existingEmail = company.EmailAddresses.FirstOrDefault(e => e.Id == updatedEmail.Id);
                            if (existingEmail != null)
                            {
                                existingEmail.EmailAddress = updatedEmail.EmailAddress;
                                existingEmail.Description = updatedEmail.Description;
                                existingEmail.IsPrimary = updatedEmail.IsPrimary;
                                existingEmail.IsActive = updatedEmail.IsActive;
                                existingEmail.UpdatedDate = DateTime.Now;
                                existingEmail.UpdatedBy = updatedCompany.UpdatedBy;
                            }
                        }
                        else
                        {
                            // Add new
                            updatedEmail.SetRelatedEntity("Company", company.Id);
                            updatedEmail.CreatedOn = DateTime.Now;
                            await context.Emails.AddAsync(updatedEmail);
                        }
                    }
                }

                // Handle contact number updates
                if (updatedCompany.ContactNumbers != null)
                {
                    // Remove existing contact numbers that aren't in the updated list
                    var numbersToRemove = company.ContactNumbers
                        .Where(c => !updatedCompany.ContactNumbers.Any(uc => uc.Id == c.Id && uc.Id != 0))
                        .ToList();

                    foreach (var number in numbersToRemove)
                    {
                        context.ContactNumbers.Remove(number);
                    }

                    // Update existing or add new contact numbers
                    foreach (var updatedNumber in updatedCompany.ContactNumbers)
                    {
                        if (updatedNumber.Id != 0)
                        {
                            // Update existing
                            var existingNumber = company.ContactNumbers.FirstOrDefault(c => c.Id == updatedNumber.Id);
                            if (existingNumber != null)
                            {
                                existingNumber.Number = updatedNumber.Number;
                                existingNumber.Type = updatedNumber.Type;
                                existingNumber.Description = updatedNumber.Description;
                                existingNumber.IsPrimary = updatedNumber.IsPrimary;
                                existingNumber.IsActive = updatedNumber.IsActive;
                                existingNumber.UpdatedDate = DateTime.Now;
                                existingNumber.UpdatedBy = updatedCompany.UpdatedBy;
                            }
                        }
                        else
                        {
                            // Add new
                            updatedNumber.SetRelatedEntity("Company", company.Id);
                            updatedNumber.CreatedOn = DateTime.Now;
                            await context.ContactNumbers.AddAsync(updatedNumber);
                        }
                    }
                }

                await context.SaveChangesAsync();

                response.Response = company;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Company updated successfully.";
            }
            catch (Exception ex)
            {
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

                // Delete related emails and contact numbers
                var emails = context.Emails.Where(e => e.RelatedEntityType == "Company" && e.RelatedEntityId == id);
                context.Emails.RemoveRange(emails);

                var contactNumbers = context.ContactNumbers.Where(c => c.RelatedEntityType == "Company" && c.RelatedEntityId == id);
                context.ContactNumbers.RemoveRange(contactNumbers);

                // Remove the company
                context.Companies.Remove(company);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Company deleted successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the company: " + ex.Message;
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
                    .ToListAsync();

                response.Response = companies;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Companies retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving companies: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Branch Methods (Using DbContextFactory)

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

                // Set creation date if not already set
                if (branch.CreatedOn == default)
                    branch.CreatedOn = DateTime.Now;

                // Add the branch
                await context.Branches.AddAsync(branch);
                await context.SaveChangesAsync();

                // Process email addresses
                if (branch.EmailAddresses != null && branch.EmailAddresses.Any())
                {
                    foreach (var email in branch.EmailAddresses)
                    {
                        email.SetRelatedEntity("Branch", branch.Id);
                        email.CreatedOn = DateTime.Now;
                    }
                    await context.Emails.AddRangeAsync(branch.EmailAddresses);
                }

                // Process contact numbers
                if (branch.ContactNumbers != null && branch.ContactNumbers.Any())
                {
                    foreach (var contact in branch.ContactNumbers)
                    {
                        contact.SetRelatedEntity("Branch", branch.Id);
                        contact.CreatedOn = DateTime.Now;
                    }
                    await context.ContactNumbers.AddRangeAsync(branch.ContactNumbers);
                }

                // Process logos
                if (branch.Logos != null && branch.Logos.Any())
                {
                    foreach (var logo in branch.Logos)
                    {
                        logo.BranchId = branch.Id;
                        logo.UploadedDate = DateTime.Now;
                    }
                    await context.BranchLogos.AddRangeAsync(branch.Logos);
                }

                await context.SaveChangesAsync();

                response.Response = branch;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Branch created successfully.";
            }
            catch (Exception ex)
            {
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
                    .Include(b => b.Logos)
                    .Include(b => b.Users)
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
                    .Include(b => b.Logos)
                    .Where(b => b.CompanyId == companyId)
                    .ToListAsync();

                response.Response = branches;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Branches retrieved successfully.";
            }
            catch (Exception ex)
            {
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

                var branch = await context.Branches
                    .Include(b => b.EmailAddresses)
                    .Include(b => b.ContactNumbers)
                    .Include(b => b.Logos)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (branch == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Branch not found.";
                    return response;
                }

                // Update branch properties
                branch.Name = updatedBranch.Name;
                branch.Address = updatedBranch.Address;
                branch.CompanyId = updatedBranch.CompanyId;
                branch.IsActive = updatedBranch.IsActive;
                branch.LogoPath = updatedBranch.LogoPath;
                branch.LogoSmallPath = updatedBranch.LogoSmallPath;
                branch.LogoMediumPath = updatedBranch.LogoMediumPath;
                branch.LogoLargePath = updatedBranch.LogoLargePath;
                branch.UpdatedDate = DateTime.Now;
                branch.UpdatedBy = updatedBranch.UpdatedBy;

                // Update BankDetails if provided
                if (updatedBranch.BankDetails != null)
                {
                    branch.BankDetails = updatedBranch.BankDetails;
                }

                // Handle email updates
                if (updatedBranch.EmailAddresses != null)
                {
                    // Process emails to remove
                    var emailsToRemove = branch.EmailAddresses
                        .Where(e => !updatedBranch.EmailAddresses.Any(ue => ue.Id == e.Id && ue.Id != 0))
                        .ToList();

                    foreach (var email in emailsToRemove)
                    {
                        context.Emails.Remove(email);
                    }

                    // Update existing emails
                    foreach (var updatedEmail in updatedBranch.EmailAddresses.Where(e => e.Id != 0))
                    {
                        var existingEmail = branch.EmailAddresses.FirstOrDefault(e => e.Id == updatedEmail.Id);
                        if (existingEmail != null)
                        {
                            existingEmail.EmailAddress = updatedEmail.EmailAddress;
                            existingEmail.Description = updatedEmail.Description;
                            existingEmail.IsPrimary = updatedEmail.IsPrimary;
                            existingEmail.IsActive = updatedEmail.IsActive;
                            existingEmail.UpdatedDate = DateTime.Now;
                            existingEmail.UpdatedBy = updatedBranch.UpdatedBy;
                        }
                    }

                    // Add new emails - IMPORTANT: This is where the fix is focused
                    foreach (var newEmail in updatedBranch.EmailAddresses.Where(e => e.Id == 0))
                    {
                        // Create completely new email object to avoid any entity tracking issues
                        var emailToAdd = new Email
                        {
                            EmailAddress = newEmail.EmailAddress,
                            Description = newEmail.Description,
                            IsPrimary = newEmail.IsPrimary,
                            IsActive = newEmail.IsActive,
                            RelatedEntityType = "Branch",
                            RelatedEntityId = branch.Id,
                            BranchId = branch.Id,
                            CreatedOn = DateTime.Now,
                            CreatedBy = updatedBranch.UpdatedBy
                        };

                        // Add directly to the context rather than to the branch.EmailAddresses collection
                        await context.Emails.AddAsync(emailToAdd);
                    }
                }

                // Handle contact number updates - SAME PATTERN AS EMAILS
                if (updatedBranch.ContactNumbers != null)
                {
                    // Process numbers to remove
                    var numbersToRemove = branch.ContactNumbers
                        .Where(c => !updatedBranch.ContactNumbers.Any(uc => uc.Id == c.Id && uc.Id != 0))
                        .ToList();

                    foreach (var number in numbersToRemove)
                    {
                        context.ContactNumbers.Remove(number);
                    }

                    // Update existing numbers
                    foreach (var updatedNumber in updatedBranch.ContactNumbers.Where(n => n.Id != 0))
                    {
                        var existingNumber = branch.ContactNumbers.FirstOrDefault(c => c.Id == updatedNumber.Id);
                        if (existingNumber != null)
                        {
                            existingNumber.Number = updatedNumber.Number;
                            existingNumber.Type = updatedNumber.Type;
                            existingNumber.Description = updatedNumber.Description;
                            existingNumber.IsPrimary = updatedNumber.IsPrimary;
                            existingNumber.IsActive = updatedNumber.IsActive;
                            existingNumber.UpdatedDate = DateTime.Now;
                            existingNumber.UpdatedBy = updatedBranch.UpdatedBy;
                        }
                    }

                    // Add new numbers - IMPORTANT: Same fix as for emails
                    foreach (var newNumber in updatedBranch.ContactNumbers.Where(n => n.Id == 0))
                    {
                        // Create completely new contact object to avoid any entity tracking issues
                        var contactToAdd = new ContactNumber
                        {
                            Number = newNumber.Number,
                            Type = newNumber.Type,
                            Description = newNumber.Description,
                            IsPrimary = newNumber.IsPrimary,
                            IsActive = newNumber.IsActive,
                            RelatedEntityType = "Branch",
                            RelatedEntityId = branch.Id,
                            BranchId = branch.Id,
                            CreatedOn = DateTime.Now,
                            CreatedBy = updatedBranch.UpdatedBy
                        };

                        // Add directly to the context rather than to the branch.ContactNumbers collection
                        await context.ContactNumbers.AddAsync(contactToAdd);
                    }
                }

                // Handle logo updates
                if (updatedBranch.Logos != null)
                {
                    // Remove logos that aren't in the updated list
                    var logosToRemove = branch.Logos
                        .Where(l => !updatedBranch.Logos.Any(ul => ul.Id == l.Id && ul.Id != 0))
                        .ToList();

                    foreach (var logo in logosToRemove)
                    {
                        context.BranchLogos.Remove(logo);
                    }

                    // Update existing or add new logos
                    foreach (var updatedLogo in updatedBranch.Logos)
                    {
                        if (updatedLogo.Id != 0)
                        {
                            // Update existing
                            var existingLogo = branch.Logos.FirstOrDefault(l => l.Id == updatedLogo.Id);
                            if (existingLogo != null)
                            {
                                existingLogo.FileName = updatedLogo.FileName;
                                existingLogo.FilePath = updatedLogo.FilePath;
                                existingLogo.Size = updatedLogo.Size;
                                existingLogo.ContentType = updatedLogo.ContentType;
                                existingLogo.FileSize = updatedLogo.FileSize;
                            }
                        }
                        else
                        {
                            // Add new - using the same pattern as emails and contact numbers
                            var logoToAdd = new BranchLogo
                            {
                                BranchId = branch.Id,
                                FileName = updatedLogo.FileName,
                                FilePath = updatedLogo.FilePath,
                                Size = updatedLogo.Size,
                                ContentType = updatedLogo.ContentType,
                                FileSize = updatedLogo.FileSize,
                                UploadedDate = DateTime.Now
                            };

                            await context.BranchLogos.AddAsync(logoToAdd);
                        }
                    }
                }

                await context.SaveChangesAsync();

                response.Response = branch;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Branch updated successfully.";
            }
            catch (Exception ex)
            {
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

                // Delete related emails, contact numbers, and logos
                var emails = await context.Emails
                    .Where(e => e.RelatedEntityType == "Branch" && e.RelatedEntityId == id)
                    .ToListAsync();
                context.Emails.RemoveRange(emails);

                var contactNumbers = await context.ContactNumbers
                    .Where(c => c.RelatedEntityType == "Branch" && c.RelatedEntityId == id)
                    .ToListAsync();
                context.ContactNumbers.RemoveRange(contactNumbers);

                var logos = await context.BranchLogos
                    .Where(l => l.BranchId == id)
                    .ToListAsync();
                context.BranchLogos.RemoveRange(logos);

                // Remove the branch
                context.Branches.Remove(branch);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Branch deleted successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the branch: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Contact Methods (Using DbContextFactory)

        public async Task<ResponseModel> AddEmailAddress(Email email)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Validate entity type
                if (string.IsNullOrEmpty(email.RelatedEntityType) ||
                    (email.RelatedEntityType != "User" &&
                     email.RelatedEntityType != "Company" &&
                     email.RelatedEntityType != "Branch"))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Invalid entity type. Must be User, Company, or Branch.";
                    return response;
                }

                // Check if entity exists
                bool entityExists = false;
                switch (email.RelatedEntityType)
                {
                    case "User":
                        entityExists = await context.Users.AnyAsync(u => u.Id == email.RelatedEntityStringId);
                        break;
                    case "Company":
                        entityExists = await context.Companies.AnyAsync(c => c.Id == email.RelatedEntityId);
                        break;
                    case "Branch":
                        entityExists = await context.Branches.AnyAsync(b => b.Id == email.RelatedEntityId);
                        break;
                }

                if (!entityExists)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"{email.RelatedEntityType} not found.";
                    return response;
                }

                // If marking as primary, update any existing primary email
                if (email.IsPrimary)
                {
                    IQueryable<Email> existingPrimaryEmails;

                    if (email.RelatedEntityType == "User")
                    {
                        existingPrimaryEmails = context.Emails
                            .Where(e => e.RelatedEntityType == email.RelatedEntityType &&
                                       e.RelatedEntityStringId == email.RelatedEntityStringId &&
                                       e.IsPrimary);
                    }
                    else
                    {
                        existingPrimaryEmails = context.Emails
                            .Where(e => e.RelatedEntityType == email.RelatedEntityType &&
                                       e.RelatedEntityId == email.RelatedEntityId &&
                                       e.IsPrimary);
                    }

                    var primaryEmails = await existingPrimaryEmails.ToListAsync();
                    foreach (var existingPrimary in primaryEmails)
                    {
                        existingPrimary.IsPrimary = false;
                        existingPrimary.UpdatedDate = DateTime.Now;
                    }
                }

                // Set creation date
                email.CreatedOn = DateTime.Now;

                // Add the email
                await context.Emails.AddAsync(email);
                await context.SaveChangesAsync();

                response.Response = email;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Email address added successfully.";
            }
            catch (Exception ex)
            {
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

                // If marking as primary, update any existing primary email
                if (updatedEmail.IsPrimary && !email.IsPrimary)
                {
                    IQueryable<Email> existingPrimaryEmails;

                    if (email.RelatedEntityType == "User")
                    {
                        existingPrimaryEmails = context.Emails
                            .Where(e => e.RelatedEntityType == email.RelatedEntityType &&
                                       e.RelatedEntityStringId == email.RelatedEntityStringId &&
                                       e.IsPrimary &&
                                       e.Id != id);
                    }
                    else
                    {
                        existingPrimaryEmails = context.Emails
                            .Where(e => e.RelatedEntityType == email.RelatedEntityType &&
                                       e.RelatedEntityId == email.RelatedEntityId &&
                                       e.IsPrimary &&
                                       e.Id != id);
                    }

                    var primaryEmails = await existingPrimaryEmails.ToListAsync();
                    foreach (var existingPrimary in primaryEmails)
                    {
                        existingPrimary.IsPrimary = false;
                        existingPrimary.UpdatedDate = DateTime.Now;
                    }
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

                // Check if this is the only primary email
                if (email.IsPrimary)
                {
                    int primaryEmailCount;

                    if (email.RelatedEntityType == "User")
                    {
                        primaryEmailCount = await context.Emails
                            .CountAsync(e => e.RelatedEntityType == email.RelatedEntityType &&
                                            e.RelatedEntityStringId == email.RelatedEntityStringId &&
                                            e.IsPrimary);
                    }
                    else
                    {
                        primaryEmailCount = await context.Emails
                            .CountAsync(e => e.RelatedEntityType == email.RelatedEntityType &&
                                            e.RelatedEntityId == email.RelatedEntityId &&
                                            e.IsPrimary);
                    }

                    if (primaryEmailCount == 1)
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
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the email address: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddContactNumber(ContactNumber contactNumber)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Validate entity type
                if (string.IsNullOrEmpty(contactNumber.RelatedEntityType) ||
                    (contactNumber.RelatedEntityType != "User" &&
                     contactNumber.RelatedEntityType != "Company" &&
                     contactNumber.RelatedEntityType != "Branch"))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Invalid entity type. Must be User, Company, or Branch.";
                    return response;
                }

                // Check if entity exists
                bool entityExists = false;
                switch (contactNumber.RelatedEntityType)
                {
                    case "User":
                        entityExists = await context.Users.AnyAsync(u => u.Id == contactNumber.RelatedEntityStringId);
                        break;
                    case "Company":
                        entityExists = await context.Companies.AnyAsync(c => c.Id == contactNumber.RelatedEntityId);
                        break;
                    case "Branch":
                        entityExists = await context.Branches.AnyAsync(b => b.Id == contactNumber.RelatedEntityId);
                        break;
                }

                if (!entityExists)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"{contactNumber.RelatedEntityType} not found.";
                    return response;
                }

                // If marking as primary, update any existing primary contact number
                if (contactNumber.IsPrimary)
                {
                    IQueryable<ContactNumber> existingPrimaryNumbers;

                    if (contactNumber.RelatedEntityType == "User")
                    {
                        existingPrimaryNumbers = context.ContactNumbers
                            .Where(c => c.RelatedEntityType == contactNumber.RelatedEntityType &&
                                       c.RelatedEntityStringId == contactNumber.RelatedEntityStringId &&
                                       c.IsPrimary);
                    }
                    else
                    {
                        existingPrimaryNumbers = context.ContactNumbers
                            .Where(c => c.RelatedEntityType == contactNumber.RelatedEntityType &&
                                       c.RelatedEntityId == contactNumber.RelatedEntityId &&
                                       c.IsPrimary);
                    }

                    var primaryNumbers = await existingPrimaryNumbers.ToListAsync();
                    foreach (var existingPrimary in primaryNumbers)
                    {
                        existingPrimary.IsPrimary = false;
                        existingPrimary.UpdatedDate = DateTime.Now;
                    }
                }

                // Set creation date
                contactNumber.CreatedOn = DateTime.Now;

                // Add the contact number
                await context.ContactNumbers.AddAsync(contactNumber);
                await context.SaveChangesAsync();

                response.Response = contactNumber;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Contact number added successfully.";
            }
            catch (Exception ex)
            {
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

                // If marking as primary, update any existing primary contact number
                if (updatedContactNumber.IsPrimary && !contactNumber.IsPrimary)
                {
                    IQueryable<ContactNumber> existingPrimaryNumbers;

                    if (contactNumber.RelatedEntityType == "User")
                    {
                        existingPrimaryNumbers = context.ContactNumbers
                            .Where(c => c.RelatedEntityType == contactNumber.RelatedEntityType &&
                                       c.RelatedEntityStringId == contactNumber.RelatedEntityStringId &&
                                       c.IsPrimary &&
                                       c.Id != id);
                    }
                    else
                    {
                        existingPrimaryNumbers = context.ContactNumbers
                            .Where(c => c.RelatedEntityType == contactNumber.RelatedEntityType &&
                                       c.RelatedEntityId == contactNumber.RelatedEntityId &&
                                       c.IsPrimary &&
                                       c.Id != id);
                    }

                    var primaryNumbers = await existingPrimaryNumbers.ToListAsync();
                    foreach (var existingPrimary in primaryNumbers)
                    {
                        existingPrimary.IsPrimary = false;
                        existingPrimary.UpdatedDate = DateTime.Now;
                    }
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

                // Check if this is the only primary contact number
                if (contactNumber.IsPrimary)
                {
                    int primaryNumberCount;

                    if (contactNumber.RelatedEntityType == "User")
                    {
                        primaryNumberCount = await context.ContactNumbers
                            .CountAsync(c => c.RelatedEntityType == contactNumber.RelatedEntityType &&
                                            c.RelatedEntityStringId == contactNumber.RelatedEntityStringId &&
                                            c.IsPrimary);
                    }
                    else
                    {
                        primaryNumberCount = await context.ContactNumbers
                            .CountAsync(c => c.RelatedEntityType == contactNumber.RelatedEntityType &&
                                            c.RelatedEntityId == contactNumber.RelatedEntityId &&
                                            c.IsPrimary);
                    }

                    if (primaryNumberCount == 1)
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
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the contact number: " + ex.Message;
            }

            return response;
        }

        #endregion
        public async Task<ResponseModel> ResetUserPassword(string userId, bool requireChange = true)
        {
            ResponseModel response = new();

            try
            {
                // Find the user
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                // Generate a random password (12 characters including uppercase, lowercase, numbers, and special chars)
                string newPassword = GenerateRandomPassword(12);

                // Update the user's password hash
                var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
                user.PasswordHash = passwordHasher.HashPassword(user, newPassword);

                // Set security stamp to invalidate existing sessions
                user.SecurityStamp = Guid.NewGuid().ToString();

                // If requireChange is true, set the flag to force password change on next login
                if (requireChange)
                {
                    user.RequireChangePasswordOnLogin = true;
                }

                // Update the last modified date
                user.UpdatedDate = DateTime.Now;

                // Save changes to the database
                await _context.SaveChangesAsync();

                // Return the new password
                response.Response = newPassword;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Password reset successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while resetting the password: " + ex.Message;
            }

            return response;
        }

        // Helper method to generate a random password
        private string GenerateRandomPassword(int length)
        {
            const string upperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";  // Excluding I and O
            const string lowerChars = "abcdefghijkmnopqrstuvwxyz";  // Excluding l
            const string digits = "23456789";  // Excluding 0 and 1
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
        public async Task<ResponseModel> GetAuthenticatedUserInfo()
        {
            ResponseModel response = new();

            try
            {
                var authState = await _authState.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity != null && user.Identity.IsAuthenticated)
                {
                    var userId = user.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var applicationUser = await _context.Users
                            .Include(u => u.EmailAddresses.Where(e => e.IsActive))
                            .Include(u => u.ContactNumbers.Where(c => c.IsActive))
                            .Include(u => u.Company)
                            .Include(u => u.Branch)
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
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving authenticated user info: " + ex.Message;
            }

            return response;
        }
    }
}