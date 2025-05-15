using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roovia.Services
{
    public class PropertyService : IProperty
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<PropertyService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<ResponseModel> CreateProperty(Property property)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Ensure dates are valid
                property.EnsureValidDates();

                // Set creation date if not already set
                if (property.CreatedOn == default)
                    property.CreatedOn = DateTime.Now;

                // Add the property
                await context.Properties.AddAsync(property);
                await context.SaveChangesAsync();

                // Reload with related data
                var createdProperty = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Include(p => p.Beneficiaries)
                    .Include(p => p.Tenants)
                    .FirstOrDefaultAsync(p => p.Id == property.Id);

                response.Response = createdProperty;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property created successfully.";

                _logger.LogInformation("Property created with ID: {PropertyId}", property.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyById(int id, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var property = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Company)
                    .Include(p => p.Branch)
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Include(p => p.Beneficiaries)
                        .ThenInclude(b => b.BenType)
                    .Include(p => p.Beneficiaries)
                        .ThenInclude(b => b.BenStatus)
                    .Include(p => p.Tenants)
                        .ThenInclude(t => t.Status)
                    .Include(p => p.Inspections)
                    .Include(p => p.MaintenanceTickets)
                    .Include(p => p.Payments)
                    .Where(p => p.Id == id && p.CompanyId == companyId && !p.IsRemoved)
                    .FirstOrDefaultAsync();

                if (property != null)
                {
                    response.Response = property;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property {PropertyId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateProperty(int id, Property updatedProperty, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                // Ensure dates are valid
                updatedProperty.EnsureValidDates();

                // Update property fields
                property.OwnerId = updatedProperty.OwnerId;
                property.PropertyName = updatedProperty.PropertyName;
                property.PropertyCode = updatedProperty.PropertyCode;
                property.CustomerRef = updatedProperty.CustomerRef;
                property.RentalAmount = updatedProperty.RentalAmount;
                property.PropertyAccountBalance = updatedProperty.PropertyAccountBalance;
                property.StatusId = updatedProperty.StatusId;
                property.ServiceLevel = updatedProperty.ServiceLevel;
                property.HasTenant = updatedProperty.HasTenant;
                property.LeaseOriginalStartDate = updatedProperty.LeaseOriginalStartDate;
                property.CurrentLeaseStartDate = updatedProperty.CurrentLeaseStartDate;
                property.LeaseEndDate = updatedProperty.LeaseEndDate;
                property.CurrentTenantId = updatedProperty.CurrentTenantId;
                property.CommissionTypeId = updatedProperty.CommissionTypeId;
                property.CommissionValue = updatedProperty.CommissionValue;
                property.PaymentsEnabled = updatedProperty.PaymentsEnabled;
                property.PaymentsVerify = updatedProperty.PaymentsVerify;
                property.MainImageId = updatedProperty.MainImageId;
                property.Address = updatedProperty.Address;
                property.Tags = updatedProperty.Tags;
                property.UpdatedDate = DateTime.Now;
                property.UpdatedBy = updatedProperty.UpdatedBy;

                await context.SaveChangesAsync();

                // Reload with related data
                var updatedResult = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Include(p => p.Beneficiaries)
                    .Include(p => p.Tenants)
                    .FirstOrDefaultAsync(p => p.Id == id);

                response.Response = updatedResult;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property updated successfully.";

                _logger.LogInformation("Property updated: {PropertyId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property {PropertyId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteProperty(int id, int companyId, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var property = await context.Properties
                    .Include(p => p.Tenants)
                    .Include(p => p.Beneficiaries)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                // Check if property has active tenants
                if (property.Tenants.Any(t => !t.IsRemoved && t.StatusId == 1)) // Assuming 1 is Active status
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete property with active tenants.";
                    return response;
                }

                // Soft delete
                property.IsRemoved = true;
                property.RemovedDate = DateTime.Now;
                property.RemovedBy = user?.Id;

                await context.SaveChangesAsync();

                response.Response = property;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property deleted successfully.";

                _logger.LogInformation("Property soft deleted: {PropertyId} by {UserId}", id, user?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property {PropertyId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the property: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllProperties(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Where(p => p.CompanyId == companyId && !p.IsRemoved)
                    .OrderBy(p => p.PropertyCode)
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} properties for company {CompanyId}", properties.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertiesByOwner(int ownerId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Include(p => p.Tenants.Where(t => !t.IsRemoved))
                    .Where(p => p.OwnerId == ownerId && !p.IsRemoved)
                    .OrderBy(p => p.PropertyCode)
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} properties for owner {OwnerId}", properties.Count, ownerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties for owner {OwnerId}", ownerId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertiesByBranch(int branchId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.CommissionType)
                    .Include(p => p.MainImage)
                    .Where(p => p.BranchId == branchId && !p.IsRemoved)
                    .OrderBy(p => p.PropertyCode)
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} properties for branch {BranchId}", properties.Count, branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties for branch {BranchId}", branchId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertiesWithTenants(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.Tenants.Where(t => !t.IsRemoved))
                        .ThenInclude(t => t.Status)
                    .Include(p => p.Tenants)
                        .ThenInclude(t => t.EmailAddresses.Where(e => e.IsActive))
                    .Include(p => p.Tenants)
                        .ThenInclude(t => t.ContactNumbers.Where(c => c.IsActive))
                    .Where(p => p.CompanyId == companyId && !p.IsRemoved && p.HasTenant)
                    .OrderBy(p => p.PropertyCode)
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Properties with tenants retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} properties with tenants for company {CompanyId}", properties.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties with tenants for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetVacantProperties(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Status)
                    .Include(p => p.MainImage)
                    .Where(p => p.CompanyId == companyId && !p.IsRemoved && !p.HasTenant)
                    .OrderBy(p => p.PropertyCode)
                    .ToListAsync();

                response.Response = properties;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vacant properties retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} vacant properties for company {CompanyId}", properties.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vacant properties for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving properties: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdatePropertyStatus(int propertyId, int statusId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == propertyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                property.StatusId = statusId;
                property.UpdatedDate = DateTime.Now;
                property.UpdatedBy = userId;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property status updated successfully.";

                _logger.LogInformation("Property status updated: {PropertyId} to status {StatusId} by {UserId}", propertyId, statusId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property status {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating property status: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyStatistics(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var properties = await context.Properties
                    .Where(p => p.CompanyId == companyId && !p.IsRemoved)
                    .ToListAsync();

                var statistics = new
                {
                    TotalProperties = properties.Count,
                    OccupiedProperties = properties.Count(p => p.HasTenant),
                    VacantProperties = properties.Count(p => !p.HasTenant),
                    TotalMonthlyRental = properties.Where(p => p.HasTenant).Sum(p => p.RentalAmount),
                    PropertiesByStatus = properties.GroupBy(p => p.StatusId)
                        .Select(g => new { StatusId = g.Key, Count = g.Count() })
                        .ToList()
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property statistics for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving statistics: " + ex.Message;
            }

            return response;
        }
    }
}