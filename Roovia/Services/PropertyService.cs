using Dapper;
using Microsoft.Data.SqlClient;
using Roovia.Interfaces;
using Roovia.Models.Helper;
using Roovia.Models.Properties;

namespace Roovia.Services
{
    public class PropertyService : IProperty
    {
        private readonly IConfiguration _configuration;
        private string _connectionString = string.Empty;

        public PropertyService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<ResponseModel> CreateProperty(Property property)
        {
            ResponseModel response = new ResponseModel();
            string sql = @"
                INSERT INTO Properties 
                (
                    OwnerId, 
                    StreetAddress, 
                    City, 
                    Province, 
                    PostalCode, 
                    Country, 
                    RentalAmount, 
                    HasTenant, 
                    LeaseOriginalStartDate, 
                    CurrentLeaseStartDate, 
                    LeaseEndDate, 
                    CurrentTenantId, 
                    CreatedOn, 
                    CreatedBy
                ) 
                VALUES 
                (
                    @OwnerId, 
                    @StreetAddress, 
                    @City, 
                    @Province, 
                    @PostalCode, 
                    @Country, 
                    @RentalAmount, 
                    @HasTenant, 
                    @LeaseOriginalStartDate, 
                    @CurrentLeaseStartDate, 
                    @LeaseEndDate, 
                    @CurrentTenantId, 
                    @CreatedOn, 
                    @CreatedBy
                );";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new
                    {
                        property.OwnerId,
                        StreetAddress = property.Address.Street,
                        City = property.Address.City,
                        Province = property.Address.Province,
                        PostalCode = property.Address.PostalCode,
                        Country = property.Address.Country,
                        property.RentalAmount,
                        property.HasTenant,
                        property.LeaseOriginalStartDate,
                        property.CurrentLeaseStartDate,
                        property.LeaseEndDate,
                        property.CurrentTenantId,
                        property.CreatedOn,
                        property.CreatedBy
                    });
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property created successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the property: " + ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> DeleteProperty(int id)
        {
            ResponseModel response = new();
            string sql = "DELETE FROM Properties WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new { Id = id });

                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Property deleted successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllProperties()
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM Properties";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryAsync<Property>(sql);
                    response.Response = result.ToList();
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Properties retrieved successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyById(int id)
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM Properties WHERE Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<Property>(sql, new { Id = id });
                    if (result != null)
                    {
                        response.Response = result;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Property retrieved successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateProperty(int id, Property updatedProperty)
        {
            ResponseModel response = new();
            string sql = @"
                UPDATE Properties 
                SET 
                    OwnerId = @OwnerId, 
                    StreetAddress = @StreetAddress, 
                    City = @City, 
                    Province = @Province, 
                    PostalCode = @PostalCode, 
                    RentalAmount = @RentalAmount, 
                    HasTenant = @HasTenant, 
                    LeaseOriginalStartDate = @LeaseOriginalStartDate, 
                    CurrentLeaseStartDate = @CurrentLeaseStartDate, 
                    LeaseEndDate = @LeaseEndDate, 
                    CurrentTenantId = @CurrentTenantId, 
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
                        updatedProperty.OwnerId,
                        StreetAddress = updatedProperty.Address.Street,
                        City = updatedProperty.Address.City,
                        Province = updatedProperty.Address.Province,
                        PostalCode = updatedProperty.Address.PostalCode,
                        updatedProperty.RentalAmount,
                        updatedProperty.HasTenant,
                        updatedProperty.LeaseOriginalStartDate,
                        updatedProperty.CurrentLeaseStartDate,
                        updatedProperty.LeaseEndDate,
                        updatedProperty.CurrentTenantId,
                        UpdatedDate = DateTime.UtcNow,
                        updatedProperty.UpdatedBy
                    });

                    if (result > 0)
                    {
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Property updated successfully.";
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found or no changes made.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the property: " + ex.Message;
            }

            return response;
        }
    }
}

