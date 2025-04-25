using Dapper;
using Microsoft.Data.SqlClient;
using Roovia.Interfaces;
using Roovia.Models.Helper;
using Roovia.Models.Tenant;

namespace Roovia.Services
{
    public class TenantService : ITenant
    {
        private readonly IConfiguration _configuration;
        private string _connectionString = string.Empty;

        public TenantService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<ResponseModel> CreateTenant(PropertyTenant tenant)
        {
            ResponseModel response = new();
            string sql = @"  
                          INSERT INTO Tenants   
                          (  
                              FirstName,   
                              LastName,   
                              EmailAddress,   
                              MobileNumber,   
                              StreetAddress,   
                              City,   
                              Province,   
                              PostalCode,   
                              BankName,   
                              AccountNumber,   
                              AccountType,   
                              BranchCode,  
                              CreatedOn,   
                              CreatedBy  
                          )   
                          VALUES   
                          (  
                              @FirstName,   
                              @LastName,   
                              @EmailAddress,   
                              @MobileNumber,   
                              @StreetAddress,   
                              @City,   
                              @Province,   
                              @PostalCode,   
                              @BankName,   
                              @AccountNumber,   
                              @AccountType,   
                              @BranchCode,  
                              @CreatedOn,   
                              @CreatedBy  
                          );";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new
                    {
                        tenant.FirstName,
                        tenant.LastName,
                        tenant.EmailAddress,
                        tenant.MobileNumber,
                        StreetAddress = tenant.Address.Street,
                        City = tenant.Address.City,
                        Province = tenant.Address.Province,
                        PostalCode = tenant.Address.PostalCode,
                        BankName = tenant.BankAccount.BankName,
                        AccountNumber = tenant.BankAccount.AccountNumber,
                        AccountType = tenant.BankAccount.AccountType,
                        BranchCode = tenant.BankAccount.BranchCode,
                        tenant.CreatedOn,
                        tenant.CreatedBy
                    });

                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Tenant created successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the tenant: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetTenantById(int id)
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM Tenants WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<PropertyTenant>(sql, new { Id = id });
                    if (result != null)
                    {
                        response.Response = result;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Tenant retrieved successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the tenant: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant)
        {
            ResponseModel response = new();
            string sql = @"
                    UPDATE Tenants 
                    SET 
                        FirstName = @FirstName, 
                        LastName = @LastName, 
                        EmailAddress = @EmailAddress, 
                        MobileNumber = @MobileNumber, 
                        StreetAddress = @StreetAddress, 
                        City = @City, 
                        Province = @Province, 
                        PostalCode = @PostalCode, 
                        BankName = @BankName, 
                        AccountNumber = @AccountNumber, 
                        AccountType = @AccountType, 
                        BranchCode = @BranchCode, 
                        UpdatedDate = @UpdatedDate, 
                        UpdatedBy = @UpdatedBy
                    WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new
                    {
                        Id = id,
                        updatedTenant.FirstName,
                        updatedTenant.LastName,
                        updatedTenant.EmailAddress,
                        updatedTenant.MobileNumber,
                        StreetAddress = updatedTenant.Address.Street,
                        City = updatedTenant.Address.City,
                        Province = updatedTenant.Address.Province,
                        PostalCode = updatedTenant.Address.PostalCode,
                        BankName = updatedTenant.BankAccount.BankName,
                        AccountNumber = updatedTenant.BankAccount.AccountNumber,
                        AccountType = updatedTenant.BankAccount.AccountType,
                        BranchCode = updatedTenant.BankAccount.BranchCode,
                        UpdatedDate = DateTime.UtcNow,
                        updatedTenant.UpdatedBy
                    });

                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Tenant updated successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found or no changes made.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the tenant: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteTenant(int id)
        {
            ResponseModel response = new();
            string sql = "DELETE FROM Tenants WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id });

                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Tenant deleted successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Tenant not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the tenant: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllTenants()
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM Tenants";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryAsync<PropertyTenant>(sql);
                    response.Response = result.ToList();
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Tenants retrieved successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving tenants: " + ex.Message;
            }

            return response;
        }
    }
}
