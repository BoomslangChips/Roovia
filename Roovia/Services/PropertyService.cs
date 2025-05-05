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

            // Set creation date if not already set
            if (property.CreatedOn == default)
                property.CreatedOn = DateTime.Now;

            string sql = @"
                INSERT INTO Properties 
                (
                    OwnerId,
                    Street,
                    UnitNumber,
                    ComplexName,
                    BuildingName,
                    Floor,
                    City,
                    Suburb,
                    Province,
                    PostalCode,
                    Country,
                    GateCode,
                    IsResidential,
                    Latitude,
                    Longitude,
                    DeliveryInstructions,
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
                    @RentalAmount, 
                    @HasTenant, 
                    @LeaseOriginalStartDate, 
                    @CurrentLeaseStartDate, 
                    @LeaseEndDate, 
                    @CurrentTenantId, 
                    @CreatedOn, 
                    @CreatedBy
                );
                SELECT CAST(SCOPE_IDENTITY() as int)";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // Execute query and get the newly inserted ID
                    var newId = await conn.QuerySingleAsync<int>(sql, new
                    {
                        property.OwnerId,
                        Street = property.Address?.Street,
                        UnitNumber = property.Address?.UnitNumber,
                        ComplexName = property.Address?.ComplexName,
                        BuildingName = property.Address?.BuildingName,
                        Floor = property.Address?.Floor,
                        City = property.Address?.City,
                        Suburb = property.Address?.Suburb,
                        Province = property.Address?.Province,
                        PostalCode = property.Address?.PostalCode,
                        Country = property.Address?.Country,
                        GateCode = property.Address?.GateCode,
                        IsResidential = property.Address?.IsResidential ?? true,
                        Latitude = property.Address?.Latitude,
                        Longitude = property.Address?.Longitude,
                        DeliveryInstructions = property.Address?.DeliveryInstructions,
                        property.RentalAmount,
                        property.HasTenant,
                        property.LeaseOriginalStartDate,
                        property.CurrentLeaseStartDate,
                        property.LeaseEndDate,
                        property.CurrentTenantId,
                        property.CreatedOn,
                        property.CreatedBy
                    });

                    // Update the property with the new ID
                    property.Id = newId;
                    response.Response = property;
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

        public async Task<ResponseModel> GetPropertyById(int id, int companyId)
        {
            ResponseModel response = new();
            string sql = @"
                SELECT p.* 
                FROM Properties p
                LEFT JOIN PropertyOwners po ON po.Id = p.OwnerId
                WHERE po.CompanyId = @CompanyId AND p.Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // Get raw data from database
                    var row = await conn.QueryFirstOrDefaultAsync(sql, new { CompanyId = companyId, Id = id });

                    if (row == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found.";
                        return response;
                    }

                    // Map the flat data to the nested object model
                    var property = new Property
                    {
                        Id = row.Id,
                        OwnerId = row.OwnerId,
                        RentalAmount = row.RentalAmount,
                        HasTenant = row.HasTenant,

                        // Handle nullable DateTime fields
                        LeaseOriginalStartDate = row.LeaseOriginalStartDate != null ? row.LeaseOriginalStartDate : DateTime.MinValue,
                        CurrentLeaseStartDate = row.CurrentLeaseStartDate != null ? row.CurrentLeaseStartDate : DateTime.MinValue,
                        LeaseEndDate = row.LeaseEndDate != null ? row.LeaseEndDate : DateTime.MinValue,

                        // Handle nullable Guid fields
                        CurrentTenantId = row.CurrentTenantId != null ? row.CurrentTenantId : Guid.Empty,
                        CreatedBy = row.CreatedBy != null ? row.CreatedBy : Guid.Empty,
                        UpdatedBy = row.UpdatedBy != null ? row.UpdatedBy : Guid.Empty,

                        // Handle other nullable DateTime fields
                        CreatedOn = row.CreatedOn != null ? row.CreatedOn : DateTime.MinValue,
                        UpdatedDate = row.UpdatedDate != null ? row.UpdatedDate : DateTime.MinValue,

                        // Map Address properties
                        Address = new Address
                        {
                            Street = row.Street,
                            UnitNumber = row.UnitNumber,
                            ComplexName = row.ComplexName,
                            BuildingName = row.BuildingName,
                            Floor = row.Floor,
                            City = row.City,
                            Suburb = row.Suburb,
                            Province = row.Province,
                            PostalCode = row.PostalCode,
                            Country = row.Country,
                            GateCode = row.GateCode,
                            IsResidential = row.IsResidential ?? true,
                            Latitude = row.Latitude,
                            Longitude = row.Longitude,
                            DeliveryInstructions = row.DeliveryInstructions
                        }
                    };

                    response.Response = property;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property retrieved successfully.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateProperty(int id, Property updatedProperty, int companyId)
        {
            ResponseModel response = new();
            string sql = @"
                UPDATE p
                SET 
                    OwnerId = @OwnerId,
                    Street = @Street,
                    UnitNumber = @UnitNumber,
                    ComplexName = @ComplexName,
                    BuildingName = @BuildingName,
                    Floor = @Floor,
                    City = @City,
                    Suburb = @Suburb,
                    Province = @Province,
                    PostalCode = @PostalCode,
                    Country = @Country,
                    GateCode = @GateCode,
                    IsResidential = @IsResidential,
                    Latitude = @Latitude,
                    Longitude = @Longitude,
                    DeliveryInstructions = @DeliveryInstructions,
                    RentalAmount = @RentalAmount, 
                    HasTenant = @HasTenant, 
                    LeaseOriginalStartDate = @LeaseOriginalStartDate, 
                    CurrentLeaseStartDate = @CurrentLeaseStartDate, 
                    LeaseEndDate = @LeaseEndDate, 
                    CurrentTenantId = @CurrentTenantId, 
                    UpdatedDate = @UpdatedDate, 
                    UpdatedBy = @UpdatedBy
                FROM Properties p
                LEFT JOIN PropertyOwners po ON po.Id = p.OwnerId
                WHERE po.CompanyId = @CompanyId AND p.Id = @Id";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = await conn.ExecuteAsync(sql, new
                    {
                        Id = id,
                        CompanyId = companyId,
                        updatedProperty.OwnerId,
                        Street = updatedProperty.Address?.Street,
                        UnitNumber = updatedProperty.Address?.UnitNumber,
                        ComplexName = updatedProperty.Address?.ComplexName,
                        BuildingName = updatedProperty.Address?.BuildingName,
                        Floor = updatedProperty.Address?.Floor,
                        City = updatedProperty.Address?.City,
                        Suburb = updatedProperty.Address?.Suburb,
                        Province = updatedProperty.Address?.Province,
                        PostalCode = updatedProperty.Address?.PostalCode,
                        Country = updatedProperty.Address?.Country,
                        GateCode = updatedProperty.Address?.GateCode,
                        IsResidential = updatedProperty.Address?.IsResidential ?? true,
                        Latitude = updatedProperty.Address?.Latitude,
                        Longitude = updatedProperty.Address?.Longitude,
                        DeliveryInstructions = updatedProperty.Address?.DeliveryInstructions,
                        updatedProperty.RentalAmount,
                        updatedProperty.HasTenant,
                        updatedProperty.LeaseOriginalStartDate,
                        updatedProperty.CurrentLeaseStartDate,
                        updatedProperty.LeaseEndDate,
                        updatedProperty.CurrentTenantId,
                        UpdatedDate = DateTime.Now,
                        updatedProperty.UpdatedBy
                    });

                    if (result > 0)
                    {
                        // Get the updated property
                        var getResponse = await GetPropertyById(id, companyId);
                        if (getResponse.ResponseInfo.Success)
                        {
                            response.Response = getResponse.Response;
                        }
                        else
                        {
                            response.Response = updatedProperty;
                        }

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

        public async Task<ResponseModel> DeleteProperty(int id, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // First get the property to return in the response
                    string selectSql = @"
                        SELECT p.* 
                        FROM Properties p
                        LEFT JOIN PropertyOwners po ON po.Id = p.OwnerId
                        WHERE po.CompanyId = @CompanyId AND p.Id = @Id";

                    var row = await conn.QueryFirstOrDefaultAsync(selectSql, new { CompanyId = companyId, Id = id });

                    if (row == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found.";
                        return response;
                    }

                    // Map the data before deletion
                    var property = new Property
                    {
                        Id = row.Id,
                        OwnerId = row.OwnerId,
                        RentalAmount = row.RentalAmount,
                        HasTenant = row.HasTenant,

                        // Handle nullable DateTime fields
                        LeaseOriginalStartDate = row.LeaseOriginalStartDate != null ? row.LeaseOriginalStartDate : DateTime.MinValue,
                        CurrentLeaseStartDate = row.CurrentLeaseStartDate != null ? row.CurrentLeaseStartDate : DateTime.MinValue,
                        LeaseEndDate = row.LeaseEndDate != null ? row.LeaseEndDate : DateTime.MinValue,

                        // Handle nullable Guid fields
                        CurrentTenantId = row.CurrentTenantId != null ? row.CurrentTenantId : Guid.Empty,
                        CreatedBy = row.CreatedBy != null ? row.CreatedBy : Guid.Empty,
                        UpdatedBy = row.UpdatedBy != null ? row.UpdatedBy : Guid.Empty,

                        // Handle other nullable DateTime fields
                        CreatedOn = row.CreatedOn != null ? row.CreatedOn : DateTime.MinValue,
                        UpdatedDate = row.UpdatedDate != null ? row.UpdatedDate : DateTime.MinValue,

                        // Map Address properties
                        Address = new Address
                        {
                            Street = row.Street,
                            UnitNumber = row.UnitNumber,
                            ComplexName = row.ComplexName,
                            BuildingName = row.BuildingName,
                            Floor = row.Floor,
                            City = row.City,
                            Suburb = row.Suburb,
                            Province = row.Province,
                            PostalCode = row.PostalCode,
                            Country = row.Country,
                            GateCode = row.GateCode,
                            IsResidential = row.IsResidential ?? true,
                            Latitude = row.Latitude,
                            Longitude = row.Longitude,
                            DeliveryInstructions = row.DeliveryInstructions
                        }
                    };

                    // Now perform the deletion
                    string deleteSql = @"
                        DELETE p
                        FROM Properties p
                        LEFT JOIN PropertyOwners po ON po.Id = p.OwnerId
                        WHERE po.CompanyId = @CompanyId AND p.Id = @Id";

                    var result = await conn.ExecuteAsync(deleteSql, new { CompanyId = companyId, Id = id });

                    if (result > 0)
                    {
                        response.Response = property;
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

        public async Task<ResponseModel> GetAllProperties(int companyId)
        {
            ResponseModel response = new();
            string sql = @"
                SELECT p.* 
                FROM Properties p
                LEFT JOIN PropertyOwners po ON po.Id = p.OwnerId
                WHERE po.CompanyId = @CompanyId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // Get the raw data from the database
                    var propertiesRaw = await conn.QueryAsync(sql, new { CompanyId = companyId });

                    // Map the results to the object model
                    var result = propertiesRaw.Select(row => {
                        var property = new Property
                        {
                            Id = row.Id,
                            OwnerId = row.OwnerId,
                            RentalAmount = row.RentalAmount,
                            HasTenant = row.HasTenant,

                            // Handle nullable DateTime fields
                            LeaseOriginalStartDate = row.LeaseOriginalStartDate != null ? row.LeaseOriginalStartDate : DateTime.MinValue,
                            CurrentLeaseStartDate = row.CurrentLeaseStartDate != null ? row.CurrentLeaseStartDate : DateTime.MinValue,
                            LeaseEndDate = row.LeaseEndDate != null ? row.LeaseEndDate : DateTime.MinValue,

                            // Handle nullable Guid fields
                            CurrentTenantId = row.CurrentTenantId != null ? row.CurrentTenantId : Guid.Empty,
                            CreatedBy = row.CreatedBy != null ? row.CreatedBy : Guid.Empty,
                            UpdatedBy = row.UpdatedBy != null ? row.UpdatedBy : Guid.Empty,

                            // Handle other nullable DateTime fields
                            CreatedOn = row.CreatedOn != null ? row.CreatedOn : DateTime.MinValue,
                            UpdatedDate = row.UpdatedDate != null ? row.UpdatedDate : DateTime.MinValue,

                            // Map Address properties
                            Address = new Address
                            {
                                Street = row.Street,
                                UnitNumber = row.UnitNumber,
                                ComplexName = row.ComplexName,
                                BuildingName = row.BuildingName,
                                Floor = row.Floor,
                                City = row.City,
                                Suburb = row.Suburb,
                                Province = row.Province,
                                PostalCode = row.PostalCode,
                                Country = row.Country,
                                GateCode = row.GateCode,
                                IsResidential = row.IsResidential ?? true,
                                Latitude = row.Latitude,
                                Longitude = row.Longitude,
                                DeliveryInstructions = row.DeliveryInstructions
                            }
                        };

                        return property;
                    }).ToList();

                    response.Response = result;
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

        public async Task<ResponseModel> GetPropertiesByOwner(int ownerId)
        {
            ResponseModel response = new();
            string sql = "SELECT * FROM Properties WHERE OwnerId = @OwnerId";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // Get the raw data from the database
                    var propertiesRaw = await conn.QueryAsync(sql, new { OwnerId = ownerId });

                    // Map the results to the object model
                    var result = propertiesRaw.Select(row => {
                        var property = new Property
                        {
                            Id = row.Id,
                            OwnerId = row.OwnerId,
                            RentalAmount = row.RentalAmount,
                            HasTenant = row.HasTenant,

                            // Handle nullable DateTime fields
                            LeaseOriginalStartDate = row.LeaseOriginalStartDate != null ? row.LeaseOriginalStartDate : DateTime.MinValue,
                            CurrentLeaseStartDate = row.CurrentLeaseStartDate != null ? row.CurrentLeaseStartDate : DateTime.MinValue,
                            LeaseEndDate = row.LeaseEndDate != null ? row.LeaseEndDate : DateTime.MinValue,

                            // Handle nullable Guid fields
                            CurrentTenantId = row.CurrentTenantId != null ? row.CurrentTenantId : Guid.Empty,
                            CreatedBy = row.CreatedBy != null ? row.CreatedBy : Guid.Empty,
                            UpdatedBy = row.UpdatedBy != null ? row.UpdatedBy : Guid.Empty,

                            // Handle other nullable DateTime fields
                            CreatedOn = row.CreatedOn != null ? row.CreatedOn : DateTime.MinValue,
                            UpdatedDate = row.UpdatedDate != null ? row.UpdatedDate : DateTime.MinValue,

                            // Map Address properties
                            Address = new Address
                            {
                                Street = row.Street,
                                UnitNumber = row.UnitNumber,
                                ComplexName = row.ComplexName,
                                BuildingName = row.BuildingName,
                                Floor = row.Floor,
                                City = row.City,
                                Suburb = row.Suburb,
                                Province = row.Province,
                                PostalCode = row.PostalCode,
                                Country = row.Country,
                                GateCode = row.GateCode,
                                IsResidential = row.IsResidential ?? true,
                                Latitude = row.Latitude,
                                Longitude = row.Longitude,
                                DeliveryInstructions = row.DeliveryInstructions
                            }
                        };

                        return property;
                    }).ToList();

                    response.Response = result;
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
    }
}