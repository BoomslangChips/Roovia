using Dapper;
using Microsoft.Data.SqlClient;
using Roovia.Interfaces;
using Roovia.Models.Helper;
using Roovia.Models.Users;

namespace Roovia.Services
{
    public class UserService : IUser
    {
        private readonly IConfiguration _configuration;
        private string _connectionString = string.Empty;

        public UserService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }


        public async Task<ResponseModel> GetUserById(int id)
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM AspNetUsers WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<object>(sql, new { Id = id });
                    if (result != null)
                    {
                        response.Response = result;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "User retrieved successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "User not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the user: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUser(int id, ApplicationUser updatedUser)
        {
            ResponseModel response = new();
            string sql = @"
               UPDATE AspNetUsers 
               SET 
                   UserName = @UserName, 
                   NormalizedUserName = @NormalizedUserName, 
                   Email = @Email, 
                   NormalizedEmail = @NormalizedEmail, 
                   EmailConfirmed = @EmailConfirmed, 
                   PasswordHash = @PasswordHash, 
                   SecurityStamp = @SecurityStamp, 
                   ConcurrencyStamp = @ConcurrencyStamp, 
                   PhoneNumber = @PhoneNumber, 
                   PhoneNumberConfirmed = @PhoneNumberConfirmed, 
                   TwoFactorEnabled = @TwoFactorEnabled, 
                   LockoutEnd = @LockoutEnd, 
                   LockoutEnabled = @LockoutEnabled, 
                   AccessFailedCount = @AccessFailedCount, 
                   CompanyId = @CompanyId, 
                   IsActive = @IsActive, 
                   FirstName = @FirstName, 
                   LastName = @LastName, 
                   UpdatedDate = @UpdatedDate, 
                   UpdatedBy = @UpdatedBy
               WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id, updatedUser });
                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "User updated successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "User not found or no changes made.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user: " + ex.Message;
            }

            return response;
        }



        public async Task<ResponseModel> DeleteUser(int id)
        {
            ResponseModel response = new();
            string sql = "DELETE FROM AspNetUsers WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id });
                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "User deleted successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "User not found.";
                    }
                }
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
            string sql = "SELECT * FROM AspNetUsers";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryAsync<object>(sql);
                    response.Response = result.ToList();
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Users retrieved successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving users: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> CreateCompany(Company company)
        {
            ResponseModel response = new();
            string sql = @"  
                  INSERT INTO AspNetCompanies  
                  (  
                      Id,  
                      Name,  
                      RegistrationNumber,  
                      ContactNumber,  
                      Email,  
                      Street,  
                      City,  
                      Province,  
                      PostalCode,  
                      Country,  
                      Website,  
                      VatNumber,  
                      CreatedOn,  
                      IsActive,  
                      CreatedBy  
                  )  
                  VALUES  
                  (  
                      @Id,  
                      @Name,  
                      @RegistrationNumber,  
                      @ContactNumber,  
                      @Email,  
                      @Address.Street,  
                      @Address.City,  
                      @Address.Province,  
                      @Address.PostalCode,  
                      @Address.Country,  
                      @Website,  
                      @VatNumber,  
                      @CreatedOn,  
                      @IsActive,  
                      @CreatedBy  
                  );";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, company);
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Company created successfully.";
                }
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
            string sql = "SELECT * FROM AspNetCompanies WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<object>(sql, new { Id = id });
                    if (result != null)
                    {
                        response.Response = result;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Company retrieved successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Company not found.";
                    }
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
            string sql = @"
                   UPDATE AspNetCompanies 
                   SET 
                       Name = @Name, 
                       RegistrationNumber = @RegistrationNumber, 
                       ContactNumber = @ContactNumber, 
                       Email = @Email, 
                       Street = @Address.Street, 
                       City = @Address.City, 
                       Province = @Address.Province, 
                       PostalCode = @Address.PostalCode, 
                       Country = @Address.Country, 
                       Website = @Website, 
                       VatNumber = @VatNumber, 
                       UpdatedDate = @UpdatedDate, 
                       UpdatedBy = @UpdatedBy
                   WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id, updatedCompany });
                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Company updated successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Company not found or no changes made.";
                    }
                }
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
            string sql = "DELETE FROM AspNetCompanies WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id });
                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Company deleted successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Company not found.";
                    }
                }
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
            string sql = "SELECT * FROM AspNetCompanies";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryAsync<object>(sql);
                    response.Response = result.ToList();
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Companies retrieved successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving companies: " + ex.Message;
            }

            return response;
        }
        public async Task<ResponseModel> UpdateUserCompanyId(int userId, int companyId)
        {
            ResponseModel response = new();
            string sql = @"
                UPDATE AspNetUsers
                SET CompanyId = @CompanyId
                WHERE Id = @UserId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { UserId = userId, CompanyId = companyId });
                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "User's company ID updated successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "User not found or no changes made.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user's company ID: " + ex.Message;
            }

            return response;
        }


        public async Task<ResponseModel> CreateBranch(Branch branch)
        {
            ResponseModel response = new();
            string sql = @"  
        INSERT INTO AspNetBranches  
        (  
            Id,  
            Name,  
            ContactNumber,  
            Email,  
            Street,  
            City,  
            Province,  
            PostalCode,  
            Country,  
            CompanyId,  
            CreatedOn,  
            IsActive,  
            CreatedBy  
        )  
        VALUES  
        (  
            @Id,  
            @Name,  
            @ContactNumber,  
            @Email,  
            @Address.Street,  
            @Address.City,  
            @Address.Province,  
            @Address.PostalCode,  
            @Address.Country,  
            @CompanyId,  
            @CreatedOn,  
            @IsActive,  
            @CreatedBy  
        );";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, branch);
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Branch created successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetBranchById(Guid id)
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM AspNetBranches WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<Branch>(sql, new { Id = id });
                    if (result != null)
                    {
                        response.Response = result;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Branch retrieved successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Branch not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetBranchesByCompany(Guid companyId)
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM AspNetBranches WHERE CompanyId = @CompanyId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryAsync<Branch>(sql, new { CompanyId = companyId });
                    response.Response = result.ToList();
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Branches retrieved successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving branches: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateBranch(Guid id, Branch updatedBranch)
        {
            ResponseModel response = new();
            string sql = @"
        UPDATE AspNetBranches 
        SET 
            Name = @Name, 
            ContactNumber = @ContactNumber, 
            Email = @Email, 
            Street = @Address.Street, 
            City = @Address.City, 
            Province = @Address.Province, 
            PostalCode = @Address.PostalCode, 
            Country = @Address.Country, 
            CompanyId = @CompanyId,
            UpdatedDate = @UpdatedDate, 
            UpdatedBy = @UpdatedBy
        WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id, updatedBranch });
                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Branch updated successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Branch not found or no changes made.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteBranch(Guid id)
        {
            ResponseModel response = new();
            string sql = "DELETE FROM AspNetBranches WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id });
                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Branch deleted successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Branch not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUserRole(string userId, UserRole role)
        {
            ResponseModel response = new();
            string sql = @"
        UPDATE AspNetUsers
        SET Role = @Role
        WHERE Id = @UserId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { UserId = userId, Role = (int)role });
                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "User role updated successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "User not found or no changes made.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user role: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateUserBranch(string userId, Guid branchId)
        {
            ResponseModel response = new();
            string sql = @"
        UPDATE AspNetUsers
        SET BranchId = @BranchId
        WHERE Id = @UserId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { UserId = userId, BranchId = branchId });
                    if (result > 0)
                    {
                        // Get the Company ID associated with this branch
                        var branch = await conn.QueryFirstOrDefaultAsync<Branch>("SELECT * FROM AspNetBranches WHERE Id = @Id", new { Id = branchId });
                        if (branch != null)
                        {
                            // Also update the Company ID for consistency
                            await conn.ExecuteAsync(
                                "UPDATE AspNetUsers SET CompanyId = @CompanyId WHERE Id = @UserId",
                                new { UserId = userId, CompanyId = branch.CompanyId });
                        }

                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "User branch assignment updated successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "User not found or no changes made.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the user's branch: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetUsersByBranch(Guid branchId)
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM AspNetUsers WHERE BranchId = @BranchId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryAsync<ApplicationUser>(sql, new { BranchId = branchId });
                    response.Response = result.ToList();
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Users retrieved successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving users: " + ex.Message;
            }

            return response;
        }
    }
}
