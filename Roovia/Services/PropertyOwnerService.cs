using Dapper;
using Microsoft.Data.SqlClient;
using Roovia.Interfaces;
using Roovia.Models.Helper;
using Roovia.Models.Properties;
using Roovia.Models.PropertyOwner;
using Roovia.Models.Users;

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
              CompanyId,  
              FirstName,  
              LastName,  
              IdNumber,  
              VatNumber,  
              EmailAddress,  
              IsEmailNotificationsEnabled,  
              MobileNumber,  
              IsSmsNotificationsEnabled,  
              AccountType,  
              AccountNumber,  
              BankName,  
              BranchCode,  
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
              CreatedOn,  
              CreatedBy  
          )  
          VALUES  
          (  
              @CompanyId,  
              @FirstName,  
              @LastName,  
              @IdNumber,  
              @VatNumber,  
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
              @CreatedOn,  
              @CreatedBy  
          );
          SELECT CAST(SCOPE_IDENTITY() as int)";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // Execute the query and get the newly inserted ID
                    var newId = await conn.QuerySingleAsync<int>(sql, new
                    {
                        propertyOwner.CompanyId,
                        propertyOwner.FirstName,
                        propertyOwner.LastName,
                        propertyOwner.IdNumber,
                        propertyOwner.VatNumber,
                        propertyOwner.EmailAddress,
                        propertyOwner.IsEmailNotificationsEnabled,
                        propertyOwner.MobileNumber,
                        propertyOwner.IsSmsNotificationsEnabled,
                        AccountType = propertyOwner.BankAccount?.AccountType,
                        AccountNumber = propertyOwner.BankAccount?.AccountNumber,
                        BankName = propertyOwner.BankAccount?.BankName.ToString(),
                        BranchCode = propertyOwner.BankAccount?.BranchCode,
                        Street = propertyOwner.Address?.Street,
                        UnitNumber = propertyOwner.Address?.UnitNumber,
                        ComplexName = propertyOwner.Address?.ComplexName,
                        BuildingName = propertyOwner.Address?.BuildingName,
                        Floor = propertyOwner.Address?.Floor,
                        City = propertyOwner.Address?.City,
                        Suburb = propertyOwner.Address?.Suburb,
                        Province = propertyOwner.Address?.Province,
                        PostalCode = propertyOwner.Address?.PostalCode,
                        Country = propertyOwner.Address?.Country,
                        GateCode = propertyOwner.Address?.GateCode,
                        IsResidential = propertyOwner.Address?.IsResidential ?? false,
                        Latitude = propertyOwner.Address?.Latitude,
                        Longitude = propertyOwner.Address?.Longitude,
                        DeliveryInstructions = propertyOwner.Address?.DeliveryInstructions,
                        propertyOwner.CreatedOn,
                        propertyOwner.CreatedBy
                    });

                    // Update the object with the new ID and return it
                    propertyOwner.Id = newId;
                    response.Response = propertyOwner;
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

        public async Task<ResponseModel> GetPropertyOwnerById(int companyId, int id)
        {
            ResponseModel response = new();
            string sql = @"SELECT * FROM PropertyOwners WHERE Id = @Id AND CompanyId = @CompanyId AND (IsRemoved = 0 OR IsRemoved IS NULL)";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // Get the raw data from the database
                    var row = await conn.QueryFirstOrDefaultAsync(sql, new { Id = id, CompanyId = companyId });

                    if (row == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property owner not found.";
                        return response;
                    }

                    // Map the flat data to the nested object model
                    var propertyOwner = new PropertyOwner
                    {
                        Id = row.Id,
                        CompanyId = row.CompanyId,
                        FirstName = row.FirstName,
                        LastName = row.LastName,
                        IdNumber = row.IdNumber,
                        VatNumber = row.VatNumber,
                        EmailAddress = row.EmailAddress,
                        IsEmailNotificationsEnabled = row.IsEmailNotificationsEnabled,
                        MobileNumber = row.MobileNumber,
                        IsSmsNotificationsEnabled = row.IsSmsNotificationsEnabled,

                        // Handle nullable DateTime fields
                        CreatedOn = row.CreatedOn != null ? row.CreatedOn : DateTime.MinValue,
                        CreatedBy = row.CreatedBy != null ? row.CreatedBy : Guid.Empty,
                        UpdatedDate = row.UpdatedDate != null ? row.UpdatedDate : DateTime.MinValue,
                        UpdatedBy = row.UpdatedBy != null ? row.UpdatedBy : Guid.Empty,

                        // Map BankAccount properties
                        BankAccount = new BankAccount
                        {
                            AccountType = row.AccountType,
                            AccountNumber = row.AccountNumber,
                            BankName = Enum.TryParse<BankName>(row.BankName?.ToString(), out BankName bankName)
                                ? bankName
                                : default,
                            BranchCode = row.BranchCode
                        },

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
                            IsResidential = row.IsResidential ?? false,
                            Latitude = row.Latitude,
                            Longitude = row.Longitude,
                            DeliveryInstructions = row.DeliveryInstructions
                        }
                    };

                    // Get related properties if applicable
                    string propertiesSql = @"SELECT * FROM Properties WHERE OwnerId = @OwnerId";
                    var properties = await conn.QueryAsync(propertiesSql, new { OwnerId = id });

                    if (properties != null && properties.Any())
                    {
                        propertyOwner.Properties = properties.Select(p => new Property
                        {
                            Id = p.Id,
                            OwnerId = p.OwnerId,
                            RentalAmount = p.RentalAmount,
                            HasTenant = p.HasTenant,
                            LeaseOriginalStartDate = p.LeaseOriginalStartDate != null ? p.LeaseOriginalStartDate : DateTime.MinValue,
                            CurrentLeaseStartDate = p.CurrentLeaseStartDate != null ? p.CurrentLeaseStartDate : DateTime.MinValue,
                            LeaseEndDate = p.LeaseEndDate != null ? p.LeaseEndDate : DateTime.MinValue,
                            CurrentTenantId = p.CurrentTenantId != null ? p.CurrentTenantId : Guid.Empty,
                            CreatedOn = p.CreatedOn != null ? p.CreatedOn : DateTime.MinValue,
                            CreatedBy = p.CreatedBy != null ? p.CreatedBy : Guid.Empty,
                            UpdatedDate = p.UpdatedDate != null ? p.UpdatedDate : DateTime.MinValue,
                            UpdatedBy = p.UpdatedBy != null ? p.UpdatedBy : Guid.Empty,

                            // Map property address
                            Address = new Address
                            {
                                Street = p.Street,
                                UnitNumber = p.UnitNumber,
                                ComplexName = p.ComplexName,
                                BuildingName = p.BuildingName,
                                Floor = p.Floor,
                                City = p.City,
                                Suburb = p.Suburb,
                                Province = p.Province,
                                PostalCode = p.PostalCode,
                                Country = p.Country,
                                GateCode = p.GateCode,
                                IsResidential = p.IsResidential ?? true,
                                Latitude = p.Latitude,
                                Longitude = p.Longitude,
                                DeliveryInstructions = p.DeliveryInstructions
                            }
                        }).ToList();
                    }

                    response.Response = propertyOwner;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property owner retrieved successfully.";
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
               CompanyId = @CompanyId,  
               FirstName = @FirstName,  
               LastName = @LastName,  
               IdNumber = @IdNumber,  
               VatNumber = @VatNumber,  
               EmailAddress = @EmailAddress,  
               IsEmailNotificationsEnabled = @IsEmailNotificationsEnabled,  
               IsSmsNotificationsEnabled = @IsSmsNotificationsEnabled,  
               MobileNumber = @MobileNumber,  
               AccountType = @AccountType,  
               AccountNumber = @AccountNumber,  
               BankName = @BankName,  
               BranchCode = @BranchCode,  
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
                        updatedProperty.CompanyId,
                        updatedProperty.FirstName,
                        updatedProperty.LastName,
                        updatedProperty.IdNumber,
                        updatedProperty.VatNumber,
                        updatedProperty.EmailAddress,
                        updatedProperty.IsEmailNotificationsEnabled,
                        updatedProperty.IsSmsNotificationsEnabled,
                        updatedProperty.MobileNumber,
                        AccountType = updatedProperty.BankAccount?.AccountType,
                        AccountNumber = updatedProperty.BankAccount?.AccountNumber,
                        BankName = updatedProperty.BankAccount?.BankName.ToString(),
                        BranchCode = updatedProperty.BankAccount?.BranchCode,
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
                        IsResidential = updatedProperty.Address?.IsResidential ?? false,
                        Latitude = updatedProperty.Address?.Latitude,
                        Longitude = updatedProperty.Address?.Longitude,
                        DeliveryInstructions = updatedProperty.Address?.DeliveryInstructions,
                        UpdatedDate = DateTime.UtcNow,
                        updatedProperty.UpdatedBy
                    });

                    if (result > 0)
                    {
                        // Get the updated property owner with all the details
                        var getResponse = await GetPropertyOwnerById(updatedProperty.CompanyId, id);
                        if (getResponse.ResponseInfo.Success)
                        {
                            response.Response = getResponse.Response;
                        }
                        else
                        {
                            response.Response = updatedProperty;
                        }

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

        public async Task<ResponseModel> DeleteProperty(int id, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // First get the property owner to return in the response
                    string selectSql = "SELECT * FROM PropertyOwners WHERE Id = @Id";
                    var row = await conn.QueryFirstOrDefaultAsync(selectSql, new { Id = id });

                    if (row == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property owner not found.";
                        return response;
                    }

                    // Map the data to return the deleted object
                    var propertyOwner = new PropertyOwner
                    {
                        Id = row.Id,
                        CompanyId = row.CompanyId,
                        FirstName = row.FirstName,
                        LastName = row.LastName,
                        IdNumber = row.IdNumber,
                        VatNumber = row.VatNumber,
                        EmailAddress = row.EmailAddress,
                        IsEmailNotificationsEnabled = row.IsEmailNotificationsEnabled,
                        MobileNumber = row.MobileNumber,
                        IsSmsNotificationsEnabled = row.IsSmsNotificationsEnabled,
                        CreatedOn = row.CreatedOn != null ? row.CreatedOn : DateTime.MinValue,
                        CreatedBy = row.CreatedBy != null ? row.CreatedBy : Guid.Empty,
                        UpdatedDate = row.UpdatedDate != null ? row.UpdatedDate : DateTime.MinValue,
                        UpdatedBy = row.UpdatedBy != null ? row.UpdatedBy : Guid.Empty,
                        BankAccount = new BankAccount
                        {
                            AccountType = row.AccountType,
                            AccountNumber = row.AccountNumber,
                            BankName = Enum.TryParse<BankName>(row.BankName?.ToString(), out BankName bankName)
                                ? bankName
                                : default,
                            BranchCode = row.BranchCode
                        },
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
                            IsResidential = row.IsResidential ?? false,
                            Latitude = row.Latitude,
                            Longitude = row.Longitude,
                            DeliveryInstructions = row.DeliveryInstructions
                        }
                    };

                    // Update IsRemoved, RemovedDate, RemovedBy
                    string updateSql = @"UPDATE PropertyOwners 
                                         SET IsRemoved = 1, RemovedDate = @RemovedDate, RemovedBy = @RemovedBy 
                                         WHERE Id = @Id";
                    var result = await conn.ExecuteAsync(updateSql, new
                    {
                        Id = id,
                        RemovedDate = DateTime.UtcNow,
                        RemovedBy = !string.IsNullOrEmpty(user?.Id) ? Guid.Parse(user.Id) : Guid.Empty
                    });

                    if (result > 0)
                    {
                        response.Response = propertyOwner;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Property owner removed successfully.";
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
                response.ResponseInfo.Message = "An error occurred while removing the property owner: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllPropertyOwners(int companyId)
        {
            ResponseModel response = new();
            string sql = @"  
        SELECT *  
        FROM PropertyOwners 
        WHERE CompanyId = @CompanyId AND (IsRemoved = 0 OR IsRemoved IS NULL)";
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // First, get the raw data from the database
                    var propertyOwnersRaw = await conn.QueryAsync(sql, new { CompanyId = companyId });

                    // Process the results to map flattened DB columns to nested objects
                    var result = propertyOwnersRaw.Select(row =>
                    {
                        var propertyOwner = new PropertyOwner
                        {
                            Id = row.Id,
                            CompanyId = row.CompanyId,
                            FirstName = row.FirstName,
                            LastName = row.LastName,
                            IdNumber = row.IdNumber,
                            VatNumber = row.VatNumber,
                            EmailAddress = row.EmailAddress,
                            IsEmailNotificationsEnabled = row.IsEmailNotificationsEnabled,
                            MobileNumber = row.MobileNumber,
                            IsSmsNotificationsEnabled = row.IsSmsNotificationsEnabled,

                            // Handle nullable DateTime fields
                            CreatedOn = row.CreatedOn != null ? row.CreatedOn : DateTime.MinValue,
                            CreatedBy = row.CreatedBy != null ? row.CreatedBy : Guid.Empty,
                            UpdatedDate = row.UpdatedDate != null ? row.UpdatedDate : DateTime.MinValue,
                            UpdatedBy = row.UpdatedBy != null ? row.UpdatedBy : Guid.Empty,

                            // Map BankAccount properties
                            BankAccount = new BankAccount
                            {
                                AccountType = row.AccountType,
                                AccountNumber = row.AccountNumber,
                                BankName = Enum.TryParse<BankName>(row.BankName?.ToString(), out BankName bankName)
                                    ? bankName
                                    : default,
                                BranchCode = row.BranchCode
                            },

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
                                IsResidential = row.IsResidential ?? false,
                                Latitude = row.Latitude,
                                Longitude = row.Longitude,
                                DeliveryInstructions = row.DeliveryInstructions
                            }
                        };

                        return propertyOwner;
                    }).ToList();

                    // Get the properties for each owner
                    string propertiesSql = @"SELECT * FROM Properties WHERE OwnerId IN @OwnerIds";
                    var ownerIds = result.Select(o => o.Id).ToArray();

                    if (ownerIds.Any())
                    {
                        var properties = await conn.QueryAsync(propertiesSql, new { OwnerIds = ownerIds });

                        if (properties != null && properties.Any())
                        {
                            foreach (var property in properties)
                            {
                                var owner = result.FirstOrDefault(o => o.Id == property.OwnerId);
                                if (owner != null)
                                {
                                    if (owner.Properties == null)
                                        owner.Properties = new List<Property>();

                                    owner.Properties.Add(new Property
                                    {
                                        Id = property.Id,
                                        OwnerId = property.OwnerId,
                                        RentalAmount = property.RentalAmount,
                                        HasTenant = property.HasTenant,
                                        LeaseOriginalStartDate = property.LeaseOriginalStartDate != null ? property.LeaseOriginalStartDate : DateTime.MinValue,
                                        CurrentLeaseStartDate = property.CurrentLeaseStartDate != null ? property.CurrentLeaseStartDate : DateTime.MinValue,
                                        LeaseEndDate = property.LeaseEndDate != null ? property.LeaseEndDate : DateTime.MinValue,
                                        CurrentTenantId = property.CurrentTenantId != null ? property.CurrentTenantId : Guid.Empty,
                                        CreatedOn = property.CreatedOn != null ? property.CreatedOn : DateTime.MinValue,
                                        CreatedBy = property.CreatedBy != null ? property.CreatedBy : Guid.Empty,
                                        UpdatedDate = property.UpdatedDate != null ? property.UpdatedDate : DateTime.MinValue,
                                        UpdatedBy = property.UpdatedBy != null ? property.UpdatedBy : Guid.Empty,

                                        // Map property address
                                        Address = new Address
                                        {
                                            Street = property.Street,
                                            UnitNumber = property.UnitNumber,
                                            ComplexName = property.ComplexName,
                                            BuildingName = property.BuildingName,
                                            Floor = property.Floor,
                                            City = property.City,
                                            Suburb = property.Suburb,
                                            Province = property.Province,
                                            PostalCode = property.PostalCode,
                                            Country = property.Country,
                                            GateCode = property.GateCode,
                                            IsResidential = property.IsResidential ?? true,
                                            Latitude = property.Latitude,
                                            Longitude = property.Longitude,
                                            DeliveryInstructions = property.DeliveryInstructions
                                        }
                                    });
                                }
                            }
                        }
                    }

                    response.Response = result;
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
