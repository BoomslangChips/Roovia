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
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        public async Task<ResponseModel> CreateTenant(PropertyTenant tenant, int companyId)
        {
            ResponseModel response = new();
            string sql = @"  
                INSERT INTO [PropertyTenants]   
                (  
                    PropertyId,  
                    CompanyId,
                    FirstName,   
                    LastName,   
                    IdNumber,  
                    EmailAddress,   
                    IsEmailNotificationsEnabled,  
                    MobileNumber,   
                    IsSmsNotificationsEnabled,  
                    BankAccount_AccountType,   
                    BankAccount_AccountNumber,   
                    BankAccount_BankName,   
                    BankAccount_BranchCode,  
                    Address_Street,  
                    Address_UnitNumber,  
                    Address_ComplexName,  
                    Address_BuildingName,  
                    Address_Floor,  
                    Address_City,  
                    Address_Suburb,  
                    Address_Province,  
                    Address_PostalCode,  
                    Address_Country,  
                    Address_GateCode,  
                    Address_IsResidential,  
                    Address_Latitude,  
                    Address_Longitude,  
                    Address_DeliveryInstructions,  
                    DebitDayOfMonth,  
                    CreatedOn,   
                    CreatedBy  
                )   
                VALUES   
                (  
                    @PropertyId,  
                    @CompanyId,
                    @FirstName,   
                    @LastName,   
                    @IdNumber,  
                    @EmailAddress,   
                    @IsEmailNotificationsEnabled,  
                    @MobileNumber,   
                    @IsSmsNotificationsEnabled,  
                    @AccountType,   
                    @AccountNumber,   
                    @BankName,   
                    @BranchCode,  
                    @Street,  
                    @UnitNumber,  
                    @ComplexName,  
                    @BuildingName,  
                    @Floor,  
                    @City,  
                    @Suburb,  
                    @Province,  
                    @PostalCode,  
                    @Country,  
                    @GateCode,  
                    @IsResidential,  
                    @Latitude,  
                    @Longitude,  
                    @DeliveryInstructions,  
                    @DebitDayOfMonth,  
                    @CreatedOn,   
                    @CreatedBy  
                );";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new
                    {
                        tenant.PropertyId,
                        CompanyId = companyId,
                        tenant.FirstName,
                        tenant.LastName,
                        tenant.IdNumber,
                        tenant.EmailAddress,
                        tenant.IsEmailNotificationsEnabled,
                        tenant.MobileNumber,
                        tenant.IsSmsNotificationsEnabled,
                        AccountType = tenant.BankAccount.AccountType,
                        AccountNumber = tenant.BankAccount.AccountNumber,
                        BankName = tenant.BankAccount.BankName,
                        BranchCode = tenant.BankAccount.BranchCode,
                        Street = tenant.Address.Street,
                        UnitNumber = tenant.Address.UnitNumber,
                        ComplexName = tenant.Address.ComplexName,
                        BuildingName = tenant.Address.BuildingName,
                        Floor = tenant.Address.Floor,
                        City = tenant.Address.City,
                        Suburb = tenant.Address.Suburb,
                        Province = tenant.Address.Province,
                        PostalCode = tenant.Address.PostalCode,
                        Country = tenant.Address.Country,
                        GateCode = tenant.Address.GateCode,
                        IsResidential = tenant.Address.IsResidential,
                        Latitude = tenant.Address.Latitude,
                        Longitude = tenant.Address.Longitude,
                        DeliveryInstructions = tenant.Address.DeliveryInstructions,
                        tenant.DebitDayOfMonth,
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

        public async Task<ResponseModel> GetTenantById(int id, int companyId)
        {
            ResponseModel response = new();
            string sql = @"SELECT * FROM [PropertyTenants] WHERE Id = @Id AND CompanyId = @CompanyId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<PropertyTenant>(sql, new { Id = id, CompanyId = companyId });
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

        public async Task<ResponseModel> UpdateTenant(int id, PropertyTenant updatedTenant, int companyId)
        {
            ResponseModel response = new();
            string sql = @"  
                UPDATE [PropertyTenants]   
                SET   
                    PropertyId = @PropertyId,  
                    FirstName = @FirstName,   
                    LastName = @LastName,   
                    IdNumber = @IdNumber,  
                    EmailAddress = @EmailAddress,   
                    IsEmailNotificationsEnabled = @IsEmailNotificationsEnabled,  
                    MobileNumber = @MobileNumber,   
                    IsSmsNotificationsEnabled = @IsSmsNotificationsEnabled,  
                    BankAccount_AccountType = @AccountType,   
                    BankAccount_AccountNumber = @AccountNumber,   
                    BankAccount_BankName = @BankName,   
                    BankAccount_BranchCode = @BranchCode,  
                    Address_Street = @Street,  
                    Address_UnitNumber = @UnitNumber,  
                    Address_ComplexName = @ComplexName,  
                    Address_BuildingName = @BuildingName,  
                    Address_Floor = @Floor,  
                    Address_City = @City,  
                    Address_Suburb = @Suburb,  
                    Address_Province = @Province,  
                    Address_PostalCode = @PostalCode,  
                    Address_Country = @Country,  
                    Address_GateCode = @GateCode,  
                    Address_IsResidential = @IsResidential,  
                    Address_Latitude = @Latitude,  
                    Address_Longitude = @Longitude,  
                    Address_DeliveryInstructions = @DeliveryInstructions,  
                    DebitDayOfMonth = @DebitDayOfMonth,  
                    UpdatedDate = @UpdatedDate,   
                    UpdatedBy = @UpdatedBy  
                WHERE Id = @Id AND CompanyId = @CompanyId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new
                    {
                        Id = id,
                        CompanyId = companyId,
                        updatedTenant.PropertyId,
                        updatedTenant.FirstName,
                        updatedTenant.LastName,
                        updatedTenant.IdNumber,
                        updatedTenant.EmailAddress,
                        updatedTenant.IsEmailNotificationsEnabled,
                        updatedTenant.MobileNumber,
                        updatedTenant.IsSmsNotificationsEnabled,
                        AccountType = updatedTenant.BankAccount.AccountType,
                        AccountNumber = updatedTenant.BankAccount.AccountNumber,
                        BankName = updatedTenant.BankAccount.BankName,
                        BranchCode = updatedTenant.BankAccount.BranchCode,
                        Street = updatedTenant.Address.Street,
                        UnitNumber = updatedTenant.Address.UnitNumber,
                        ComplexName = updatedTenant.Address.ComplexName,
                        BuildingName = updatedTenant.Address.BuildingName,
                        Floor = updatedTenant.Address.Floor,
                        City = updatedTenant.Address.City,
                        Suburb = updatedTenant.Address.Suburb,
                        Province = updatedTenant.Address.Province,
                        PostalCode = updatedTenant.Address.PostalCode,
                        Country = updatedTenant.Address.Country,
                        GateCode = updatedTenant.Address.GateCode,
                        IsResidential = updatedTenant.Address.IsResidential,
                        Latitude = updatedTenant.Address.Latitude,
                        Longitude = updatedTenant.Address.Longitude,
                        DeliveryInstructions = updatedTenant.Address.DeliveryInstructions,
                        updatedTenant.DebitDayOfMonth,
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

        public async Task<ResponseModel> DeleteTenant(int id, int companyId)
        {
            ResponseModel response = new();
            string sql = "DELETE FROM [PropertyTenants] WHERE Id = @Id AND CompanyId = @CompanyId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id, CompanyId = companyId });

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

        public async Task<ResponseModel> GetAllTenants(int companyId)
        {
            ResponseModel response = new();
            string sql = @"
                SELECT 
                    Id,
                    PropertyId,
                    CompanyId,
                    FirstName,
                    LastName,
                    IdNumber,
                    EmailAddress,
                    IsEmailNotificationsEnabled,
                    MobileNumber,
                    IsSmsNotificationsEnabled,
                    BankAccount_AccountType AS AccountType,
                    BankAccount_AccountNumber AS AccountNumber,
                    BankAccount_BankName AS BankName,
                    BankAccount_BranchCode AS BranchCode,
                    Address_Street AS Street,
                    Address_UnitNumber AS UnitNumber,
                    Address_ComplexName AS ComplexName,
                    Address_BuildingName AS BuildingName,
                    Address_Floor AS Floor,
                    Address_City AS City,
                    Address_Suburb AS Suburb,
                    Address_Province AS Province,
                    Address_PostalCode AS PostalCode,
                    Address_Country AS Country,
                    Address_GateCode AS GateCode,
                    Address_IsResidential AS IsResidential,
                    Address_Latitude AS Latitude,
                    Address_Longitude AS Longitude,
                    Address_DeliveryInstructions AS DeliveryInstructions,
                    DebitDayOfMonth,
                    CreatedOn,
                    CreatedBy,
                    UpdatedDate,
                    UpdatedBy
                FROM [PropertyTenants]
                WHERE CompanyId = @CompanyId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryAsync<PropertyTenant, BankAccount, Address, PropertyTenant>(
                        sql,
                        (tenant, bankAccount, address) =>
                        {
                            tenant.BankAccount = bankAccount;
                            tenant.Address = address;

                            if (tenant.CreatedOn == DateTime.MinValue)
                            {
                                tenant.CreatedOn = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
                            }

                            return tenant;
                        },
                        new { CompanyId = companyId },
                        splitOn: "AccountType,Street"
                    );

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

        public async Task<ResponseModel> GetTenantWithPropertyId(int propertyId, int companyId)
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM [PropertyTenants] WHERE PropertyId = @PropertyId AND CompanyId = @CompanyId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<PropertyTenant>(sql, new { PropertyId = propertyId, CompanyId = companyId });
                    response.Response = result;
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
