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
    }
}
