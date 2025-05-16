using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using Roovia.Services.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Roovia.Services
{
    public class InspectionService : IInspection
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<InspectionService> _logger;
        private readonly ICdnService _cdnService;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKeyPrefix = "Inspection_";

        public InspectionService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<InspectionService> logger,
            ICdnService cdnService,
            IMemoryCache cache)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _cdnService = cdnService;
            _cache = cache;
        }

        public async Task<ResponseModel> CreateInspection(PropertyInspection inspection, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify the property exists and belongs to the company
                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == inspection.PropertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found or does not belong to the company.";
                    return response;
                }

                // Generate unique inspection code
                inspection.InspectionCode = await GenerateUniqueInspectionCode(context);
                inspection.CompanyId = companyId;
                inspection.CreatedOn = DateTime.Now;

                // Add the inspection
                await context.PropertyInspections.AddAsync(inspection);
                await context.SaveChangesAsync();

                // Create CDN folder structure for inspection
                var cdnFolderPath = $"company-{companyId}/properties/{inspection.PropertyId}/inspections/{inspection.Id}";
                await _cdnService.CreateFolderAsync("inspections", cdnFolderPath, "images");
                await _cdnService.CreateFolderAsync("inspections", cdnFolderPath, "reports");

                // Reload with related data
                var createdInspection = await GetInspectionWithDetails(context, inspection.Id);

                // Schedule next inspection if this is a routine inspection
                if (inspection.InspectionTypeId == 1) // Assuming 1 is "Routine" inspection type
                {
                    var nextInspectionDate = CalculateNextInspectionDate(inspection.ScheduledDate);
                    inspection.NextInspectionDue = nextInspectionDate;
                    await context.SaveChangesAsync();
                }

                scope.Complete();

                // Invalidate any cached inspection lists for this property
                InvalidatePropertyInspectionCache(property.Id);

                response.Response = createdInspection;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspection created successfully.";

                _logger.LogInformation("Inspection created with ID: {InspectionId} for property {PropertyId}",
                    inspection.Id, inspection.PropertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inspection");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the inspection: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetInspectionById(int id, int companyId)
        {
            ResponseModel response = new();

            try
            {
                // Try to get from cache first
                var cacheKey = $"{_cacheKeyPrefix}{id}";
                if (_cache.TryGetValue(cacheKey, out PropertyInspection cachedInspection))
                {
                    response.Response = cachedInspection;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Inspection retrieved from cache successfully.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await context.PropertyInspections
                    .Include(i => i.Property)
                        .ThenInclude(p => p.Owner)
                    .Include(i => i.Property)
                        .ThenInclude(p => p.Address)
                    .Include(i => i.InspectionType)
                    .Include(i => i.Status)
                    .Include(i => i.OverallCondition)
                    .Include(i => i.ReportDocument)
                    .Include(i => i.InspectionItems)
                        .ThenInclude(ii => ii.Area)
                    .Include(i => i.InspectionItems)
                        .ThenInclude(ii => ii.Condition)
                    .Include(i => i.InspectionItems)
                        .ThenInclude(ii => ii.MaintenancePriority)
                    .Include(i => i.InspectionItems)
                        .ThenInclude(ii => ii.Image)
                    .Where(i => i.Id == id && i.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                if (inspection != null)
                {
                    // Cache the result
                    _cache.Set(cacheKey, inspection, TimeSpan.FromMinutes(15));

                    response.Response = inspection;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Inspection retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inspection {InspectionId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the inspection: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateInspection(int id, PropertyInspection updatedInspection, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await context.PropertyInspections
                    .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);

                if (inspection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
                    return response;
                }

                // Track status change for business logic
                var statusChanged = inspection.StatusId != updatedInspection.StatusId;
                var oldStatusId = inspection.StatusId;

                // Update inspection fields
                inspection.StatusId = updatedInspection.StatusId;
                inspection.ScheduledDate = updatedInspection.ScheduledDate;
                inspection.ActualDate = updatedInspection.ActualDate;
                inspection.InspectorName = updatedInspection.InspectorName;
                inspection.InspectorUserId = updatedInspection.InspectorUserId;
                inspection.GeneralNotes = updatedInspection.GeneralNotes;
                inspection.OverallRating = updatedInspection.OverallRating;
                inspection.OverallConditionId = updatedInspection.OverallConditionId;
                inspection.TenantName = updatedInspection.TenantName;
                inspection.TenantPresent = updatedInspection.TenantPresent;
                inspection.NextInspectionDue = updatedInspection.NextInspectionDue;
                inspection.UpdatedDate = DateTime.Now;
                inspection.UpdatedBy = updatedInspection.UpdatedBy;

                await context.SaveChangesAsync();

                // Handle status change related business logic
                if (statusChanged)
                {
                    await HandleStatusChange(context, inspection, oldStatusId);
                }

                scope.Complete();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{id}";
                _cache.Remove(cacheKey);
                InvalidatePropertyInspectionCache(inspection.PropertyId);

                // Reload with related data
                var updatedResult = await GetInspectionWithDetails(context, id);

                response.Response = updatedResult;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspection updated successfully.";

                _logger.LogInformation("Inspection updated: {InspectionId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inspection {InspectionId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the inspection: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteInspection(int id, int companyId, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await context.PropertyInspections
                    .Include(i => i.InspectionItems)
                    .Include(i => i.MaintenanceTickets)
                    .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);

                if (inspection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
                    return response;
                }

                // Check if inspection has maintenance tickets
                if (inspection.MaintenanceTickets.Any())
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete inspection with associated maintenance tickets.";
                    return response;
                }

                // Get property ID for cache invalidation
                var propertyId = inspection.PropertyId;

                // Delete inspection items
                context.InspectionItems.RemoveRange(inspection.InspectionItems);

                // Delete the inspection
                context.PropertyInspections.Remove(inspection);
                await context.SaveChangesAsync();

                scope.Complete();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{id}";
                _cache.Remove(cacheKey);
                InvalidatePropertyInspectionCache(propertyId);

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspection deleted successfully.";

                _logger.LogInformation("Inspection deleted: {InspectionId} by {UserId}", id, user?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inspection {InspectionId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the inspection: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetInspectionsByProperty(int propertyId, int companyId, int page = 1, int pageSize = 20,
            string sortField = "ScheduledDate", bool sortAscending = false, DateTime? startDate = null, DateTime? endDate = null, int? statusId = null)
        {
            ResponseModel response = new();

            try
            {
                // Create cache key based on parameters
                var cacheKey = $"{_cacheKeyPrefix}ByProperty_{propertyId}_{page}_{pageSize}_{sortField}_{sortAscending}_{startDate}_{endDate}_{statusId}";

                // Try to get from cache
                if (_cache.TryGetValue(cacheKey, out object cachedResult))
                {
                    response.Response = cachedResult;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Inspections retrieved from cache successfully.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                // Base query
                var query = context.PropertyInspections
                    .Include(i => i.InspectionType)
                    .Include(i => i.Status)
                    .Include(i => i.OverallCondition)
                    .Where(i => i.PropertyId == propertyId && i.CompanyId == companyId);

                // Apply date range filter
                if (startDate.HasValue)
                {
                    query = query.Where(i => i.ScheduledDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(i => i.ScheduledDate <= endDate.Value);
                }

                // Apply status filter
                if (statusId.HasValue)
                {
                    query = query.Where(i => i.StatusId == statusId.Value);
                }

                // Apply sorting
                query = ApplySorting(query, sortField, sortAscending);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var inspections = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    Items = inspections,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                // Cache the result
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                response.Response = result;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspections retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} inspections for property {PropertyId}",
                    inspections.Count, propertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inspections for property {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving inspections: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddInspectionItem(int inspectionId, InspectionItem item)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await context.PropertyInspections
                    .FirstOrDefaultAsync(i => i.Id == inspectionId);

                if (inspection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
                    return response;
                }

                // Validate item
                if (string.IsNullOrWhiteSpace(item.ItemName))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Item name is required.";
                    return response;
                }

                item.InspectionId = inspectionId;
                await context.InspectionItems.AddAsync(item);
                await context.SaveChangesAsync();

                // Load complete item with relations
                var createdItem = await context.InspectionItems
                    .Include(ii => ii.Area)
                    .Include(ii => ii.Condition)
                    .Include(ii => ii.MaintenancePriority)
                    .Include(ii => ii.Image)
                    .FirstOrDefaultAsync(ii => ii.Id == item.Id);

                scope.Complete();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{inspectionId}";
                _cache.Remove(cacheKey);

                response.Response = createdItem;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspection item added successfully.";

                _logger.LogInformation("Inspection item added: {ItemId} to inspection {InspectionId}",
                    item.Id, inspectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding inspection item to inspection {InspectionId}", inspectionId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the inspection item: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AddInspectionItems(int inspectionId, List<InspectionItem> items)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await context.PropertyInspections
                    .FirstOrDefaultAsync(i => i.Id == inspectionId);

                if (inspection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
                    return response;
                }

                // Validate items
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.ItemName))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Item name is required for all items.";
                        return response;
                    }
                    item.InspectionId = inspectionId;
                }

                await context.InspectionItems.AddRangeAsync(items);
                await context.SaveChangesAsync();

                scope.Complete();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{inspectionId}";
                _cache.Remove(cacheKey);

                response.Response = new { ItemCount = items.Count };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"{items.Count} inspection items added successfully.";

                _logger.LogInformation("{Count} inspection items added to inspection {InspectionId}",
                    items.Count, inspectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding inspection items to inspection {InspectionId}", inspectionId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the inspection items: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateInspectionItem(int itemId, InspectionItem updatedItem)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var item = await context.InspectionItems
                    .FirstOrDefaultAsync(ii => ii.Id == itemId);

                if (item == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection item not found.";
                    return response;
                }

                // Update item fields
                item.ItemName = updatedItem.ItemName;
                item.AreaId = updatedItem.AreaId;
                item.ConditionId = updatedItem.ConditionId;
                item.Rating = updatedItem.Rating;
                item.Notes = updatedItem.Notes;
                item.RequiresMaintenance = updatedItem.RequiresMaintenance;
                item.MaintenancePriorityId = updatedItem.MaintenancePriorityId;
                item.MaintenanceNotes = updatedItem.MaintenanceNotes;
                item.ImageId = updatedItem.ImageId;

                await context.SaveChangesAsync();

                scope.Complete();

                // Invalidate cache for parent inspection
                var cacheKey = $"{_cacheKeyPrefix}{item.InspectionId}";
                _cache.Remove(cacheKey);

                // Load updated item with relations
                var updatedResult = await context.InspectionItems
                    .Include(ii => ii.Area)
                    .Include(ii => ii.Condition)
                    .Include(ii => ii.MaintenancePriority)
                    .Include(ii => ii.Image)
                    .FirstOrDefaultAsync(ii => ii.Id == itemId);

                response.Response = updatedResult;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspection item updated successfully.";

                _logger.LogInformation("Inspection item updated: {ItemId}", itemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inspection item {ItemId}", itemId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the inspection item: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteInspectionItem(int itemId)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var item = await context.InspectionItems
                    .FirstOrDefaultAsync(ii => ii.Id == itemId);

                if (item == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection item not found.";
                    return response;
                }

                // Store inspection ID for cache invalidation
                var inspectionId = item.InspectionId;

                context.InspectionItems.Remove(item);
                await context.SaveChangesAsync();

                scope.Complete();

                // Invalidate cache for parent inspection
                var cacheKey = $"{_cacheKeyPrefix}{inspectionId}";
                _cache.Remove(cacheKey);

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspection item deleted successfully.";

                _logger.LogInformation("Inspection item deleted: {ItemId}", itemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inspection item {ItemId}", itemId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the inspection item: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GenerateInspectionReport(int inspectionId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await GetInspectionWithDetails(context, inspectionId);
                if (inspection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
                    return response;
                }

                // Generate PDF report
                var pdfBytes = await GeneratePdfReport(inspection);
                var fileName = $"inspection_report_{inspection.InspectionCode}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                // Upload to CDN
                using (var stream = new MemoryStream(pdfBytes))
                {
                    var cdnPath = $"company-{inspection.CompanyId}/properties/{inspection.PropertyId}/inspections/{inspection.Id}/reports";
                    var cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                        stream,
                        fileName,
                        "application/pdf",
                        "inspections",
                        cdnPath);

                    // Save report reference
                    var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);
                    if (fileMetadata != null)
                    {
                        inspection.ReportDocumentId = fileMetadata.Id;
                        inspection.UpdatedDate = DateTime.Now;
                        inspection.UpdatedBy = userId;

                        await context.SaveChangesAsync();

                        response.Response = new
                        {
                            ReportUrl = cdnUrl,
                            FileId = fileMetadata.Id,
                            FileName = fileName,
                            InspectionId = inspectionId
                        };
                    }
                    else
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Failed to save report metadata.";
                        return response;
                    }
                }

                scope.Complete();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{inspectionId}";
                _cache.Remove(cacheKey);

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspection report generated successfully.";

                _logger.LogInformation("Inspection report generated: {InspectionId} by {UserId}", inspectionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inspection report {InspectionId}", inspectionId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while generating the report: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetUpcomingInspections(int companyId, int days = 30, int page = 1, int pageSize = 20, int? propertyId = null)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var cutoffDate = DateTime.Now.AddDays(days);

                // Base query
                var query = context.PropertyInspections
                    .Include(i => i.Property)
                    .Include(i => i.InspectionType)
                    .Include(i => i.Status)
                    .Where(i => i.CompanyId == companyId &&
                               i.ScheduledDate >= DateTime.Now &&
                               i.ScheduledDate <= cutoffDate &&
                               i.StatusId == 1); // Assuming 1 is Scheduled status

                // Filter by property if specified
                if (propertyId.HasValue)
                {
                    query = query.Where(i => i.PropertyId == propertyId.Value);
                }

                // Apply sorting by scheduled date
                query = query.OrderBy(i => i.ScheduledDate);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var inspections = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                response.Response = new
                {
                    Items = inspections,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    NextWeekCount = await query.Where(i => i.ScheduledDate <= DateTime.Now.AddDays(7)).CountAsync(),
                    ThisMonthCount = await query.Where(i => i.ScheduledDate.Month == DateTime.Now.Month &&
                                                           i.ScheduledDate.Year == DateTime.Now.Year).CountAsync()
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Upcoming inspections retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} upcoming inspections for company {CompanyId}",
                    inspections.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming inspections for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving inspections: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> CompleteInspection(int inspectionId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await context.PropertyInspections
                    .Include(i => i.InspectionItems)
                    .FirstOrDefaultAsync(i => i.Id == inspectionId);

                if (inspection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
                    return response;
                }

                // Update inspection status
                inspection.StatusId = 3; // Assuming 3 is Completed status
                inspection.ActualDate = DateTime.Now;
                inspection.UpdatedDate = DateTime.Now;
                inspection.UpdatedBy = userId;

                // Create maintenance tickets for items that require maintenance
                var maintenanceItems = inspection.InspectionItems
                    .Where(ii => ii.RequiresMaintenance)
                    .ToList();

                var maintenanceTickets = new List<MaintenanceTicket>();
                foreach (var item in maintenanceItems)
                {
                    var ticket = new MaintenanceTicket
                    {
                        PropertyId = inspection.PropertyId,
                        CompanyId = inspection.CompanyId,
                        InspectionId = inspectionId,
                        TicketNumber = await GenerateUniqueTicketNumber(context),
                        Title = $"Maintenance Required - {item.ItemName}",
                        Description = item.MaintenanceNotes,
                        CategoryId = GetMaintenanceCategoryFromArea(item.AreaId),
                        PriorityId = item.MaintenancePriorityId ?? 2, // Medium priority as default
                        StatusId = 1, // Open status
                        CreatedOn = DateTime.Now,
                        CreatedBy = userId
                    };

                    maintenanceTickets.Add(ticket);
                }

                await context.MaintenanceTickets.AddRangeAsync(maintenanceTickets);
                await context.SaveChangesAsync();

                // Schedule next routine inspection if applicable
                if (inspection.InspectionTypeId == 1) // Routine inspection
                {
                    var nextInspectionDate = CalculateNextInspectionDate(inspection.ActualDate ?? inspection.ScheduledDate);
                    inspection.NextInspectionDue = nextInspectionDate;

                    // Create the next scheduled inspection
                    await CreateFollowUpInspection(context, inspection, nextInspectionDate);

                    await context.SaveChangesAsync();
                }

                scope.Complete();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{inspectionId}";
                _cache.Remove(cacheKey);
                InvalidatePropertyInspectionCache(inspection.PropertyId);

                response.Response = new
                {
                    Inspection = inspection,
                    MaintenanceTicketsCreated = maintenanceTickets.Count,
                    MaintenanceTickets = maintenanceTickets.Select(t => new
                    {
                        t.Id,
                        t.TicketNumber,
                        t.Title,
                        t.PriorityId
                    }).ToList()
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Inspection completed successfully. {maintenanceTickets.Count} maintenance tickets created.";

                _logger.LogInformation("Inspection completed: {InspectionId} by {UserId}", inspectionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing inspection {InspectionId}", inspectionId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while completing the inspection: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetInspectionStatistics(int companyId, int? propertyId = null)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Base query
                var query = context.PropertyInspections
                    .Where(i => i.CompanyId == companyId);

                // Filter by property if specified
                if (propertyId.HasValue)
                {
                    query = query.Where(i => i.PropertyId == propertyId.Value);
                }

                var inspections = await query.ToListAsync();

                // Calculate statistics
                var today = DateTime.Today;
                var statistics = new
                {
                    TotalInspections = inspections.Count,
                    ScheduledInspections = inspections.Count(i => i.StatusId == 1), // Scheduled
                    CompletedInspections = inspections.Count(i => i.StatusId == 3), // Completed
                    OverdueInspections = inspections.Count(i => i.StatusId == 1 && i.ScheduledDate < today), // Scheduled but past date
                    ThisMonthInspections = inspections.Count(i => i.ScheduledDate.Month == today.Month && i.ScheduledDate.Year == today.Year),
                    NextMonthInspections = inspections.Count(i => i.ScheduledDate.Month == today.AddMonths(1).Month && i.ScheduledDate.Year == today.AddMonths(1).Year),
                    AverageRating = inspections.Where(i => i.OverallRating.HasValue).Any() ?
                        inspections.Where(i => i.OverallRating.HasValue).Average(i => i.OverallRating.Value) : 0,
                    MaintenanceItemsIdentified = inspections
                        .SelectMany(i => i.InspectionItems)
                        .Count(ii => ii.RequiresMaintenance),
                    InspectionsByType = inspections
                        .GroupBy(i => i.InspectionTypeId)
                        .Select(g => new { TypeId = g.Key, Count = g.Count() })
                        .ToList(),
                    InspectionsByMonth = inspections
                        .Where(i => i.ScheduledDate >= today.AddMonths(-6))
                        .GroupBy(i => new { i.ScheduledDate.Year, i.ScheduledDate.Month })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                            Count = g.Count(),
                            Completed = g.Count(i => i.StatusId == 3)
                        })
                        .OrderBy(x => x.Year)
                        .ThenBy(x => x.Month)
                        .ToList()
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspection statistics retrieved successfully.";

                _logger.LogInformation("Retrieved inspection statistics for company {CompanyId}", companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inspection statistics");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving inspection statistics: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UploadInspectionItemImage(int itemId, Stream imageStream, string fileName, string contentType, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                var item = await context.InspectionItems
                    .Include(ii => ii.Inspection)
                    .FirstOrDefaultAsync(ii => ii.Id == itemId);

                if (item == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection item not found.";
                    return response;
                }

                // Delete old image if exists
                if (item.ImageId.HasValue)
                {
                    var oldImage = await context.CdnFileMetadata
                        .FirstOrDefaultAsync(f => f.Id == item.ImageId.Value);

                    if (oldImage != null)
                    {
                        await _cdnService.DeleteFileAsync(oldImage.Url);
                    }
                }

                // Upload new image with base64 backup
                var cdnPath = $"company-{item.Inspection.CompanyId}/properties/{item.Inspection.PropertyId}/inspections/{item.Inspection.Id}/images";
                var cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                    imageStream,
                    fileName,
                    contentType,
                    "inspections",
                    cdnPath
                );

                // Get the file metadata
                var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);
                if (fileMetadata != null)
                {
                    item.ImageId = fileMetadata.Id;
                    await context.SaveChangesAsync();

                    response.Response = new { ImageUrl = cdnUrl, FileId = fileMetadata.Id };
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Inspection item image uploaded successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Failed to save image metadata.";
                    return response;
                }

                scope.Complete();

                // Invalidate cache
                var cacheKey = $"{_cacheKeyPrefix}{item.InspectionId}";
                _cache.Remove(cacheKey);

                _logger.LogInformation("Inspection item image uploaded: Item {ItemId} by {UserId}", itemId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading inspection item image");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the image: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> ScheduleRecurringInspections(int propertyId, int companyId, int frequencyMonths, DateTime startDate, int? count = null, DateTime? endDate = null, string userId = null)
        {
            ResponseModel response = new();

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify the property exists and belongs to the company
                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == propertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found or does not belong to the company.";
                    return response;
                }

                // Validate parameters
                if (frequencyMonths <= 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Frequency must be a positive number of months.";
                    return response;
                }

                if (startDate < DateTime.Today)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Start date must be today or in the future.";
                    return response;
                }

                if (count.HasValue && count.Value <= 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Count must be a positive number.";
                    return response;
                }

                if (endDate.HasValue && endDate.Value <= startDate)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "End date must be after the start date.";
                    return response;
                }

                // Calculate inspection dates
                var inspectionDates = new List<DateTime>();
                var currentDate = startDate;

                if (count.HasValue)
                {
                    // Schedule fixed number of inspections
                    for (int i = 0; i < count.Value; i++)
                    {
                        inspectionDates.Add(currentDate);
                        currentDate = currentDate.AddMonths(frequencyMonths);
                    }
                }
                else if (endDate.HasValue)
                {
                    // Schedule inspections until end date
                    while (currentDate <= endDate.Value)
                    {
                        inspectionDates.Add(currentDate);
                        currentDate = currentDate.AddMonths(frequencyMonths);
                    }
                }
                else
                {
                    // Schedule just one inspection
                    inspectionDates.Add(currentDate);
                }

                // Create inspections
                var createdInspections = new List<PropertyInspection>();
                foreach (var date in inspectionDates)
                {
                    var inspection = new PropertyInspection
                    {
                        PropertyId = propertyId,
                        CompanyId = companyId,
                        InspectionCode = await GenerateUniqueInspectionCode(context),
                        InspectionTypeId = 1, // Routine inspection
                        StatusId = 1, // Scheduled
                        ScheduledDate = date,
                        CreatedOn = DateTime.Now,
                        CreatedBy = userId
                    };

                    await context.PropertyInspections.AddAsync(inspection);
                    createdInspections.Add(inspection);
                }

                await context.SaveChangesAsync();

                scope.Complete();

                // Invalidate cache
                InvalidatePropertyInspectionCache(propertyId);

                response.Response = new
                {
                    InspectionsCreated = createdInspections.Count,
                    FirstInspection = createdInspections.First().ScheduledDate,
                    LastInspection = createdInspections.Last().ScheduledDate,
                    InspectionIds = createdInspections.Select(i => i.Id).ToList()
                };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Successfully scheduled {createdInspections.Count} recurring inspections.";

                _logger.LogInformation("Created {Count} recurring inspections for property {PropertyId} by {UserId}",
                    createdInspections.Count, propertyId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling recurring inspections");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while scheduling recurring inspections: " + ex.Message;
            }

            return response;
        }

        #region Private Helper Methods

        private async Task<PropertyInspection> GetInspectionWithDetails(ApplicationDbContext context, int inspectionId)
        {
            return await context.PropertyInspections
                .Include(i => i.Property)
                    .ThenInclude(p => p.Owner)
                .Include(i => i.Property)
                    .ThenInclude(p => p.Address)
                .Include(i => i.InspectionType)
                .Include(i => i.Status)
                .Include(i => i.OverallCondition)
                .Include(i => i.ReportDocument)
                .Include(i => i.InspectionItems)
                    .ThenInclude(ii => ii.Area)
                .Include(i => i.InspectionItems)
                    .ThenInclude(ii => ii.Condition)
                .Include(i => i.InspectionItems)
                    .ThenInclude(ii => ii.MaintenancePriority)
                .Include(i => i.InspectionItems)
                    .ThenInclude(ii => ii.Image)
                .FirstOrDefaultAsync(i => i.Id == inspectionId);
        }

        private async Task<string> GenerateUniqueInspectionCode(ApplicationDbContext context)
        {
            string code;
            do
            {
                code = $"INS-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
            }
            while (await context.PropertyInspections.AnyAsync(i => i.InspectionCode == code));

            return code;
        }

        private async Task<string> GenerateUniqueTicketNumber(ApplicationDbContext context)
        {
            string number;
            do
            {
                number = $"MT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
            }
            while (await context.MaintenanceTickets.AnyAsync(t => t.TicketNumber == number));

            return number;
        }

        private async Task<byte[]> GeneratePdfReport(PropertyInspection inspection)
        {
            // Build a simple PDF report
            var stringBuilder = new StringBuilder();

            // Header
            stringBuilder.AppendLine($"Inspection Report");
            stringBuilder.AppendLine($"==================");
            stringBuilder.AppendLine();

            // Inspection Details
            stringBuilder.AppendLine($"Inspection Code: {inspection.InspectionCode}");
            stringBuilder.AppendLine($"Property: {inspection.Property?.PropertyName}");
            stringBuilder.AppendLine($"Address: {FormatAddress(inspection.Property?.Address)}");
            stringBuilder.AppendLine($"Inspection Type: {inspection.InspectionType?.Name}");
            stringBuilder.AppendLine($"Date: {(inspection.ActualDate ?? inspection.ScheduledDate):yyyy-MM-dd}");
            stringBuilder.AppendLine($"Inspector: {inspection.InspectorName}");
            stringBuilder.AppendLine();

            // Overall Rating
            stringBuilder.AppendLine($"Overall Rating: {inspection.OverallRating}/5");
            stringBuilder.AppendLine($"Overall Condition: {inspection.OverallCondition?.Name}");
            stringBuilder.AppendLine();

            // General Notes
            stringBuilder.AppendLine("General Notes:");
            stringBuilder.AppendLine(inspection.GeneralNotes ?? "No general notes provided.");
            stringBuilder.AppendLine();

            // Inspection Items
            stringBuilder.AppendLine("Inspection Items:");
            stringBuilder.AppendLine("=================");

            if (inspection.InspectionItems?.Any() == true)
            {
                foreach (var item in inspection.InspectionItems.OrderBy(i => i.AreaId))
                {
                    stringBuilder.AppendLine($"\nItem: {item.ItemName}");
                    stringBuilder.AppendLine($"Area: {item.Area?.Name}");
                    stringBuilder.AppendLine($"Condition: {item.Condition?.Name}");
                    stringBuilder.AppendLine($"Rating: {item.Rating}/5");
                    stringBuilder.AppendLine($"Notes: {item.Notes}");

                    if (item.RequiresMaintenance)
                    {
                        stringBuilder.AppendLine($"Requires Maintenance: Yes");
                        stringBuilder.AppendLine($"Priority: {item.MaintenancePriority?.Name}");
                        stringBuilder.AppendLine($"Maintenance Notes: {item.MaintenanceNotes}");
                    }
                    else
                    {
                        stringBuilder.AppendLine($"Requires Maintenance: No");
                    }

                    stringBuilder.AppendLine();
                }
            }
            else
            {
                stringBuilder.AppendLine("No inspection items recorded.");
            }

            // Add tenant information if available
            if (!string.IsNullOrEmpty(inspection.TenantName))
            {
                stringBuilder.AppendLine("\nTenant Information:");
                stringBuilder.AppendLine("====================");
                stringBuilder.AppendLine($"Tenant Name: {inspection.TenantName}");
                stringBuilder.AppendLine($"Tenant Present: {(inspection.TenantPresent == true ? "Yes" : "No")}");
                stringBuilder.AppendLine();
            }

            // Add next inspection information if available
            if (inspection.NextInspectionDue.HasValue)
            {
                stringBuilder.AppendLine("\nNext Inspection:");
                stringBuilder.AppendLine("=================");
                stringBuilder.AppendLine($"Next inspection due: {inspection.NextInspectionDue.Value:yyyy-MM-dd}");
            }

            // Add signature section
            stringBuilder.AppendLine("\n\n");
            stringBuilder.AppendLine("____________________________");
            stringBuilder.AppendLine("Inspector Signature");

            stringBuilder.AppendLine("\n\n");
            stringBuilder.AppendLine("____________________________");
            stringBuilder.AppendLine("Property Owner/Tenant Signature");

            // Add footer with date
            stringBuilder.AppendLine($"\n\nReport generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            // Convert to bytes
            // In a real implementation, use a PDF library (iTextSharp, PdfSharp, etc.)
            // For this sample, we'll just return the string as bytes
            return System.Text.Encoding.UTF8.GetBytes(stringBuilder.ToString());
        }

        private string FormatAddress(Address address)
        {
            if (address == null)
                return "Address not available";

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(address.UnitNumber))
                parts.Add($"Unit {address.UnitNumber}");

            if (!string.IsNullOrEmpty(address.Street))
                parts.Add(address.Street);

            if (!string.IsNullOrEmpty(address.Suburb))
                parts.Add(address.Suburb);

            if (!string.IsNullOrEmpty(address.City))
                parts.Add(address.City);

            if (!string.IsNullOrEmpty(address.Province))
                parts.Add(address.Province);

            if (!string.IsNullOrEmpty(address.PostalCode))
                parts.Add(address.PostalCode);

            if (!string.IsNullOrEmpty(address.Country))
                parts.Add(address.Country);

            return string.Join(", ", parts);
        }

        private IQueryable<PropertyInspection> ApplySorting(IQueryable<PropertyInspection> query, string sortField, bool sortAscending)
        {
            return sortField.ToLower() switch
            {
                "scheduleddate" => sortAscending ? query.OrderBy(i => i.ScheduledDate) : query.OrderByDescending(i => i.ScheduledDate),
                "actualdate" => sortAscending ? query.OrderBy(i => i.ActualDate) : query.OrderByDescending(i => i.ActualDate),
                "inspectioncode" => sortAscending ? query.OrderBy(i => i.InspectionCode) : query.OrderByDescending(i => i.InspectionCode),
                "inspectiontype" => sortAscending ? query.OrderBy(i => i.InspectionType.Name) : query.OrderByDescending(i => i.InspectionType.Name),
                "status" => sortAscending ? query.OrderBy(i => i.Status.Name) : query.OrderByDescending(i => i.Status.Name),
                "inspectorname" => sortAscending ? query.OrderBy(i => i.InspectorName) : query.OrderByDescending(i => i.InspectorName),
                "overallrating" => sortAscending ? query.OrderBy(i => i.OverallRating) : query.OrderByDescending(i => i.OverallRating),
                "createdon" => sortAscending ? query.OrderBy(i => i.CreatedOn) : query.OrderByDescending(i => i.CreatedOn),
                _ => query.OrderByDescending(i => i.ScheduledDate)
            };
        }

        private void InvalidatePropertyInspectionCache(int propertyId)
        {
            // Clear all property inspection list cache keys
            var cacheKeysToRemove = new List<string>();

            // Simple way - create a pattern to remove all inspection caches for this property
            var cacheKeyPattern = $"{_cacheKeyPrefix}ByProperty_{propertyId}_";

            // In a real implementation, you would have a more sophisticated cache key tracking system
            // For now, we rely on cache expiration to eventually clear out old keys

            // If we had access to the underlying IMemoryCache implementation details, 
            // we could enumerate and remove keys matching the pattern
        }

        private DateTime CalculateNextInspectionDate(DateTime currentDate)
        {
            // Default to 3 months, but this could be configurable per property or company policy
            return currentDate.AddMonths(3);
        }

        private async Task CreateFollowUpInspection(ApplicationDbContext context, PropertyInspection completedInspection, DateTime scheduledDate)
        {
            var newInspection = new PropertyInspection
            {
                PropertyId = completedInspection.PropertyId,
                CompanyId = completedInspection.CompanyId,
                InspectionCode = await GenerateUniqueInspectionCode(context),
                InspectionTypeId = completedInspection.InspectionTypeId,
                StatusId = 1, // Scheduled
                ScheduledDate = scheduledDate,
                InspectorName = completedInspection.InspectorName,
                InspectorUserId = completedInspection.InspectorUserId,
                CreatedOn = DateTime.Now,
                CreatedBy = completedInspection.UpdatedBy
            };

            await context.PropertyInspections.AddAsync(newInspection);
        }

        private async Task HandleStatusChange(ApplicationDbContext context, PropertyInspection inspection, int oldStatusId)
        {
            // Handle business logic based on status changes
            var newStatusId = inspection.StatusId;

            // If moving to cancelled status
            if (newStatusId == 4 && oldStatusId != 4) // Assuming 4 is Cancelled
            {
                // Schedule replacement inspection if it was a routine inspection
                if (inspection.InspectionTypeId == 1) // Routine
                {
                    var replacementDate = CalculateNextAvailableDate(inspection.ScheduledDate);
                    await CreateFollowUpInspection(context, inspection, replacementDate);
                }
            }

            // If moving to completed status
            if (newStatusId == 3 && oldStatusId != 3) // Completed
            {
                inspection.ActualDate = DateTime.Now;

                // Logic for creating maintenance tickets is handled in the CompleteInspection method
            }
        }

        private DateTime CalculateNextAvailableDate(DateTime baseDate)
        {
            // Calculate next available inspection date
            // Skip weekends and add buffer days as needed

            var nextDate = baseDate.AddDays(7); // Start with a week later

            // If it falls on a weekend, move to Monday
            if (nextDate.DayOfWeek == DayOfWeek.Saturday)
                nextDate = nextDate.AddDays(2);
            else if (nextDate.DayOfWeek == DayOfWeek.Sunday)
                nextDate = nextDate.AddDays(1);

            return nextDate;
        }

        private int GetMaintenanceCategoryFromArea(int areaId)
        {
            // Map inspection areas to maintenance categories
            // This could be data-driven in a more sophisticated implementation
            return areaId switch
            {
                1 => 1, // Kitchen -> Plumbing
                2 => 2, // Bathroom -> Plumbing
                3 => 3, // Electrical room -> Electrical
                4 => 4, // Living room -> General
                5 => 5, // Bedroom -> General
                6 => 6, // Exterior -> Landscaping
                7 => 7, // Roof -> Structural
                _ => 8, // Default -> General maintenance
            };
        }

        #endregion
    }
}