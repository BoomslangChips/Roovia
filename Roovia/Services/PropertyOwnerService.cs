using Dapper;
using Microsoft.Data.SqlClient;
using Roovia.Interfaces;
using Roovia.Models.Helper;
using Roovia.Models.Properties;
using Roovia.Models.PropertyOwner;

namespace Roovia.Services
{
    public class PropertyOwnerService : IPropertyOwner
    {
        private readonly IConfiguration _configuration;
        private string _connectionString = string.Empty;

        public PropertyOwnerService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<ResponseModel> CreatePropertyOwner(PropertyOwner propertyOwner)
        {
            ResponseModel response = new();

            propertyOwner.CreatedOn = DateTime.Now;
            propertyOwner.UpdatedDate = DateTime.Now;
            string sql = @"
                   INSERT INTO PropertyOwners 
                   (
                       FirstName, 
                       LastName, 
                       IdNumber, 
                       VatNumber, 
                       EmailAddress, 
                       IsEmailNotificationsEnabled, 
                       MobileNumber, 
                       StreetAddress, 
                       City, 
                       Province, 
                       PostalCode, 
                       Country, 
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
                       @IdNumber, 
                       @VatNumber, 
                       @EmailAddress, 
                       @IsEmailNotificationsEnabled, 
                       @MobileNumber, 
                       @StreetAddress, 
                       @City, 
                       @Province, 
                       @PostalCode, 
                       @Country, 
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
                        propertyOwner.FirstName,
                        propertyOwner.LastName,
                        propertyOwner.IdNumber,
                        propertyOwner.VatNumber,
                        propertyOwner.EmailAddress,
                        propertyOwner.IsEmailNotificationsEnabled,
                        propertyOwner.MobileNumber,
                        StreetAddress = propertyOwner.Address.Street,
                        City = propertyOwner.Address.City,
                        Province = propertyOwner.Address.Province,
                        PostalCode = propertyOwner.Address.PostalCode,
                        Country = propertyOwner.Address.Country,
                        BankName = propertyOwner.BankAccount.BankName,
                        AccountNumber = propertyOwner.BankAccount.AccountNumber,
                        AccountType = propertyOwner.BankAccount.AccountType,
                        BranchCode = propertyOwner.BankAccount.BranchCode,
                        propertyOwner.CreatedOn ,
                        propertyOwner.CreatedBy,
                    });

                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property owner created successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyById(int id)
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM PropertyOwners WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<PropertyOwner>(sql, new { Id = id });
                    if (result != null)
                    {
                        response.Response = result;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Property owner retrieved successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property owner not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdatePropertyOwner(int id, PropertyOwner updatedProperty)
        {
            ResponseModel response = new();
            string sql = @"
                    UPDATE PropertyOwners 
                    SET 
                        FirstName = @FirstName, 
                        LastName = @LastName, 
                        IdNumber = @IdNumber, 
                        VatNumber = @VatNumber, 
                        EmailAddress = @EmailAddress, 
                        IsEmailNotificationsEnabled = @IsEmailNotificationsEnabled, 
                        IsSmsNotificationsEnabled = @IsSmsNotificationsEnabled, 
                        MobileNumber = @MobileNumber, 
                        StreetAddress = @StreetAddress, 
                        City = @City, 
                        Province = @Province, 
                        PostalCode = @PostalCode, 
                        Country = @Country, 
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
                        updatedProperty.FirstName,
                        updatedProperty.LastName,
                        updatedProperty.IdNumber,
                        updatedProperty.VatNumber,
                        updatedProperty.EmailAddress,
                        updatedProperty.IsEmailNotificationsEnabled,
                        updatedProperty.IsSmsNotificationsEnabled,
                        updatedProperty.MobileNumber,
                        StreetAddress = updatedProperty.Address.Street,
                        City = updatedProperty.Address.City,
                        Province = updatedProperty.Address.Province,
                        PostalCode = updatedProperty.Address.PostalCode,
                        Country = updatedProperty.Address.Country,
                        BankName = updatedProperty.BankAccount.BankName,
                        AccountNumber = updatedProperty.BankAccount.AccountNumber,
                        AccountType = updatedProperty.BankAccount.AccountType,
                        BranchCode = updatedProperty.BankAccount.BranchCode,
                        UpdatedDate = DateTime.UtcNow,
                        updatedProperty.UpdatedBy
                    });

                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Property owner updated successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property owner not found or no changes made.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteProperty(int id)
        {
            ResponseModel response = new();
            string sql = "DELETE FROM PropertyOwners WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id });

                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Property owner deleted successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property owner not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllProperties()
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM PropertyOwners";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryAsync<PropertyOwner>(sql);
                    response.Response = result.ToList();
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property owners retrieved successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving property owners: " + ex.Message;
            }

            return response;
        }
    }
}
