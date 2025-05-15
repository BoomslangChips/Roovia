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
    public class InspectionService : IInspection
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<InspectionService> _logger;
        private readonly ICdnService _cdnService;

        public InspectionService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<InspectionService> logger,
            ICdnService cdnService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _cdnService = cdnService;
        }

        public async Task<ResponseModel> CreateInspection(PropertyInspection inspection, int companyId)
        {
            ResponseModel response = new();

            try
            {
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

                // Reload with related data
                var createdInspection = await GetInspectionWithDetails(context, inspection.Id);

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
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await context.PropertyInspections
                    .Include(i => i.Property)
                        .ThenInclude(p => p.Owner)
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
                    .Where(i => i.Id == id && i.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                if (inspection != null)
                {
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
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await context.PropertyInspections
                    .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);

                if (inspection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
                    return response;
                }

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

                // Delete inspection items
                context.InspectionItems.RemoveRange(inspection.InspectionItems);

                // Delete the inspection
                context.PropertyInspections.Remove(inspection);
                await context.SaveChangesAsync();

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

        public async Task<ResponseModel> GetInspectionsByProperty(int propertyId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspections = await context.PropertyInspections
                    .Include(i => i.InspectionType)
                    .Include(i => i.Status)
                    .Include(i => i.OverallCondition)
                    .Where(i => i.PropertyId == propertyId && i.CompanyId == companyId)
                    .OrderByDescending(i => i.ScheduledDate)
                    .ToListAsync();

                response.Response = inspections;
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
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await context.PropertyInspections
                    .FirstOrDefaultAsync(i => i.Id == inspectionId);

                if (inspection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
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
                    .FirstOrDefaultAsync(ii => ii.Id == item.Id);

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

        public async Task<ResponseModel> UpdateInspectionItem(int itemId, InspectionItem updatedItem)
        {
            ResponseModel response = new();

            try
            {
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

                await context.SaveChangesAsync();

                // Load updated item with relations
                var updatedResult = await context.InspectionItems
                    .Include(ii => ii.Area)
                    .Include(ii => ii.Condition)
                    .Include(ii => ii.MaintenancePriority)
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
                using var context = await _contextFactory.CreateDbContextAsync();

                var item = await context.InspectionItems
                    .FirstOrDefaultAsync(ii => ii.Id == itemId);

                if (item == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection item not found.";
                    return response;
                }

                context.InspectionItems.Remove(item);
                await context.SaveChangesAsync();

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
                using var context = await _contextFactory.CreateDbContextAsync();

                var inspection = await GetInspectionWithDetails(context, inspectionId);
                if (inspection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Inspection not found.";
                    return response;
                }

                // Generate PDF report (this is a placeholder - implement actual PDF generation)
                var pdfBytes = await GeneratePdfReport(inspection);
                var fileName = $"inspection_report_{inspection.InspectionCode}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                // Upload to CDN
                using (var stream = new MemoryStream(pdfBytes))
                {
                    var cdnUrl = await _cdnService.UploadFileAsync(stream, fileName, "application/pdf", 
                        "inspection-reports", inspection.PropertyId.ToString());

                    // Save report reference
                    inspection.ReportDocumentId = await GetFileIdFromUrl(cdnUrl, context);
                    inspection.UpdatedDate = DateTime.Now;
                    inspection.UpdatedBy = userId;

                    await context.SaveChangesAsync();

                    response.Response = new { ReportUrl = cdnUrl, InspectionId = inspectionId };
                }

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

        public async Task<ResponseModel> GetUpcomingInspections(int companyId, int days = 30)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var cutoffDate = DateTime.Now.AddDays(days);

                var inspections = await context.PropertyInspections
                    .Include(i => i.Property)
                    .Include(i => i.InspectionType)
                    .Include(i => i.Status)
                    .Where(i => i.CompanyId == companyId &&
                               i.ScheduledDate >= DateTime.Now &&
                               i.ScheduledDate <= cutoffDate &&
                               i.StatusId == 1) // Assuming 1 is Scheduled status
                    .OrderBy(i => i.ScheduledDate)
                    .ToListAsync();

                response.Response = inspections;
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
                        CategoryId = 1, // Default category - should be configured
                        PriorityId = item.MaintenancePriorityId ?? 2, // Medium priority as default
                        StatusId = 1, // Open status
                        CreatedOn = DateTime.Now,
                        CreatedBy = userId
                    };

                    await context.MaintenanceTickets.AddAsync(ticket);
                }

                await context.SaveChangesAsync();

                response.Response = inspection;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Inspection completed successfully. {maintenanceItems.Count} maintenance tickets created.";

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

        // Helper methods
        private async Task<PropertyInspection> GetInspectionWithDetails(ApplicationDbContext context, int inspectionId)
        {
            return await context.PropertyInspections
                .Include(i => i.Property)
                    .ThenInclude(p => p.Owner)
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
            // This is a placeholder - implement actual PDF generation logic
            // You might use a library like iTextSharp, PDFSharp, or similar
            var content = $"Inspection Report for {inspection.Property?.PropertyName}\n" +
                         $"Date: {inspection.ActualDate ?? inspection.ScheduledDate}\n" +
                         $"Inspector: {inspection.InspectorName}\n\n" +
                         $"Overall Rating: {inspection.OverallRating}/5\n" +
                         $"Overall Condition: {inspection.OverallCondition?.Name}\n\n" +
                         $"Items Inspected: {inspection.InspectionItems?.Count ?? 0}";

            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        private async Task<int?> GetFileIdFromUrl(string cdnUrl, ApplicationDbContext context)
        {
            var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);
            return fileMetadata?.Id;
        }
    }
}