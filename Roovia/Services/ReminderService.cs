using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roovia.Services
{
    public class ReminderService : IReminderService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public ReminderService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IAuditService auditService,
            INotificationService notificationService)
        {
            _contextFactory = contextFactory;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        public async Task<ResponseModel> CreateReminder(Reminder reminder)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Set creation date and other defaults
                reminder.CreatedOn = DateTime.Now;
                if (reminder.ReminderStatusId == 0)
                {
                    // Get the "Pending" status ID
                    var pendingStatus = await context.ReminderStatuses
                        .FirstOrDefaultAsync(rs => rs.Name.ToLower() == "pending");
                    
                    if (pendingStatus != null)
                        reminder.ReminderStatusId = pendingStatus.Id;
                    else
                        reminder.ReminderStatusId = 1; // Default to ID 1 if not found
                }
                
                // Add the reminder
                await context.Reminders.AddAsync(reminder);
                await context.SaveChangesAsync();
                
                // Log the action
                if (reminder.RelatedEntityId.HasValue)
                {
                    await _auditService.LogEntityChange(
                        reminder.RelatedEntityType, 
                        reminder.RelatedEntityId.Value, 
                        reminder.CreatedBy, 
                        "CreateReminder", 
                        $"Created reminder: {reminder.Title} due on {reminder.DueDate:yyyy-MM-dd}");
                }
                
                response.Response = reminder;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Reminder created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating reminder: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetReminderById(int id)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var reminder = await context.Reminders
                    .Include(r => r.ReminderType)
                    .Include(r => r.ReminderStatus)
                    .Include(r => r.RecurrenceFrequency)
                    .FirstOrDefaultAsync(r => r.Id == id);
                
                if (reminder == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Reminder with ID {id} not found";
                    return response;
                }
                
                response.Response = reminder;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving reminder: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> UpdateReminder(int id, Reminder updatedReminder)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var reminder = await context.Reminders.FindAsync(id);
                
                if (reminder == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Reminder with ID {id} not found";
                    return response;
                }
                
                // Store original values for audit
                DateTime originalDueDate = reminder.DueDate;
                string originalTitle = reminder.Title;
                
                // Update the reminder properties
                reminder.Title = updatedReminder.Title;
                reminder.Description = updatedReminder.Description;
                reminder.ReminderTypeId = updatedReminder.ReminderTypeId;
                reminder.ReminderStatusId = updatedReminder.ReminderStatusId;
                reminder.DueDate = updatedReminder.DueDate;
                reminder.IsRecurring = updatedReminder.IsRecurring;
                reminder.RecurrenceFrequencyId = updatedReminder.RecurrenceFrequencyId;
                reminder.RecurrenceInterval = updatedReminder.RecurrenceInterval;
                reminder.RecurrenceEndDate = updatedReminder.RecurrenceEndDate;
                reminder.SendNotification = updatedReminder.SendNotification;
                reminder.NotifyDaysBefore = updatedReminder.NotifyDaysBefore;
                reminder.AssignedToUserId = updatedReminder.AssignedToUserId;
                reminder.UpdatedDate = DateTime.Now;
                reminder.UpdatedBy = updatedReminder.UpdatedBy;
                
                await context.SaveChangesAsync();
                
                // Log the change
                if (reminder.RelatedEntityId.HasValue)
                {
                    string changeDetails = $"Updated reminder: {reminder.Title} due on {reminder.DueDate:yyyy-MM-dd}";
                    if (originalDueDate != reminder.DueDate)
                    {
                        changeDetails += $". Due date changed from {originalDueDate:yyyy-MM-dd} to {reminder.DueDate:yyyy-MM-dd}";
                    }
                    
                    await _auditService.LogEntityChange(
                        reminder.RelatedEntityType, 
                        reminder.RelatedEntityId.Value, 
                        reminder.UpdatedBy, 
                        "UpdateReminder", 
                        changeDetails);
                }
                
                response.Response = reminder;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Reminder updated successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating reminder: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> DeleteReminder(int id, string userId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var reminder = await context.Reminders.FindAsync(id);
                
                if (reminder == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Reminder with ID {id} not found";
                    return response;
                }
                
                // Store values for audit logging
                string entityType = reminder.RelatedEntityType;
                int? entityId = reminder.RelatedEntityId;
                string title = reminder.Title;
                
                context.Reminders.Remove(reminder);
                await context.SaveChangesAsync();
                
                // Log the deletion
                if (entityId.HasValue)
                {
                    await _auditService.LogEntityChange(
                        entityType, 
                        entityId.Value, 
                        userId, 
                        "DeleteReminder", 
                        $"Deleted reminder: {title}");
                }
                
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Reminder deleted successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting reminder: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> CompleteReminder(int id, string userId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Find the reminder
                var reminder = await context.Reminders.FindAsync(id);
                if (reminder == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Reminder with ID {id} not found";
                    return response;
                }
                
                // Get completed status ID
                var completedStatus = await context.ReminderStatuses
                    .FirstOrDefaultAsync(rs => rs.Name.ToLower() == "completed");
                
                if (completedStatus == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Completed status not found";
                    return response;
                }
                
                // Update reminder status
                reminder.ReminderStatusId = completedStatus.Id;
                reminder.CompletedDate = DateTime.Now;
                reminder.UpdatedDate = DateTime.Now;
                reminder.UpdatedBy = userId;
                
                await context.SaveChangesAsync();
                
                // Log completion
                if (reminder.RelatedEntityId.HasValue)
                {
                    await _auditService.LogEntityChange(
                        reminder.RelatedEntityType, 
                        reminder.RelatedEntityId.Value, 
                        userId, 
                        "CompleteReminder", 
                        $"Completed reminder: {reminder.Title}");
                }
                
                // Create next occurrence if recurring
                if (reminder.IsRecurring && 
                    reminder.RecurrenceFrequencyId.HasValue && 
                    reminder.RecurrenceInterval.HasValue)
                {
                    await CreateNextRecurringInstance(reminder);
                }
                
                response.Response = reminder;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Reminder marked as completed";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error completing reminder: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> SnoozeReminder(int id, DateTime snoozeUntil, string userId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Find the reminder
                var reminder = await context.Reminders.FindAsync(id);
                if (reminder == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Reminder with ID {id} not found";
                    return response;
                }
                
                // Get snoozed status ID
                var snoozedStatus = await context.ReminderStatuses
                    .FirstOrDefaultAsync(rs => rs.Name.ToLower() == "snoozed");
                
                if (snoozedStatus == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Snoozed status not found";
                    return response;
                }
                
                // Store original due date for audit
                DateTime originalDueDate = reminder.DueDate;
                
                // Update reminder
                reminder.ReminderStatusId = snoozedStatus.Id;
                reminder.DueDate = snoozeUntil;
                reminder.UpdatedDate = DateTime.Now;
                reminder.UpdatedBy = userId;
                
                await context.SaveChangesAsync();
                
                // Log snooze action
                if (reminder.RelatedEntityId.HasValue)
                {
                    await _auditService.LogEntityChange(
                        reminder.RelatedEntityType, 
                        reminder.RelatedEntityId.Value, 
                        userId, 
                        "SnoozeReminder", 
                        $"Snoozed reminder: {reminder.Title} from {originalDueDate:yyyy-MM-dd} to {snoozeUntil:yyyy-MM-dd}");
                }
                
                response.Response = reminder;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Reminder snoozed until {snoozeUntil:yyyy-MM-dd}";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error snoozing reminder: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetRemindersByEntity(string entityType, object entityId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var query = context.Reminders
                    .Include(r => r.ReminderType)
                    .Include(r => r.ReminderStatus)
                    .Include(r => r.RecurrenceFrequency)
                    .Where(r => r.RelatedEntityType == entityType)
                    .OrderBy(r => r.DueDate);
                
                // Handle different ID types
                if (entityId is int intId)
                {
                    query = (IOrderedQueryable<Reminder>)query.Where(r => r.RelatedEntityId == intId);
                }
                else if (entityId is string stringId)
                {
                    query = (IOrderedQueryable<Reminder>)query.Where(r => r.RelatedEntityStringId == stringId);
                }
                
                var reminders = await query.ToListAsync();
                
                response.Response = reminders;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving reminders: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetRemindersByStatus(int statusId, int companyId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get entities associated with the company
                var propertyIds = await context.Properties
                    .Where(p => p.CompanyId == companyId)
                    .Select(p => p.Id)
                    .ToListAsync();
                
                var ownerIds = await context.PropertyOwners
                    .Where(o => o.CompanyId == companyId)
                    .Select(o => o.Id)
                    .ToListAsync();
                
                var tenantIds = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId)
                    .Select(t => t.Id)
                    .ToListAsync();
                
                var beneficiaryIds = await context.PropertyBeneficiaries
                    .Where(b => b.CompanyId == companyId)
                    .Select(b => b.Id)
                    .ToListAsync();
                
                // Get reminders by status for this company's entities
                var reminders = await context.Reminders
                    .Include(r => r.ReminderType)
                    .Include(r => r.ReminderStatus)
                    .Include(r => r.RecurrenceFrequency)
                    .Where(r => r.ReminderStatusId == statusId)
                    .Where(r => 
                        (r.RelatedEntityType == "Property" && propertyIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "Company" && r.RelatedEntityId == companyId)
                    )
                    .OrderBy(r => r.DueDate)
                    .ToListAsync();
                
                response.Response = reminders;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving reminders by status: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetRemindersByUser(string userId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var reminders = await context.Reminders
                    .Include(r => r.ReminderType)
                    .Include(r => r.ReminderStatus)
                    .Include(r => r.RecurrenceFrequency)
                    .Where(r => r.AssignedToUserId == userId)
                    .OrderBy(r => r.DueDate)
                    .ToListAsync();
                
                response.Response = reminders;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving reminders by user: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetActiveReminders(string userId = null, int companyId = 0)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get statuses that are considered "active"
                var activeStatusIds = await context.ReminderStatuses
                    .Where(rs => rs.Name.ToLower() != "completed" && rs.Name.ToLower() != "cancelled")
                    .Select(rs => rs.Id)
                    .ToListAsync();
                
                // Base query for active reminders
                var query = context.Reminders
                    .Include(r => r.ReminderType)
                    .Include(r => r.ReminderStatus)
                    .Include(r => r.RecurrenceFrequency)
                    .Where(r => activeStatusIds.Contains(r.ReminderStatusId))
                    .OrderBy(r => r.DueDate);
                
                // Filter by user if specified
                if (!string.IsNullOrEmpty(userId))
                {
                    query = (IOrderedQueryable<Reminder>)query.Where(r => r.AssignedToUserId == userId);
                }
                
                // Filter by company if specified
                if (companyId > 0)
                {
                    // Get entity IDs for this company
                    var propertyIds = await context.Properties
                        .Where(p => p.CompanyId == companyId)
                        .Select(p => p.Id)
                        .ToListAsync();
                    
                    var ownerIds = await context.PropertyOwners
                        .Where(o => o.CompanyId == companyId)
                        .Select(o => o.Id)
                        .ToListAsync();
                    
                    var tenantIds = await context.PropertyTenants
                        .Where(t => t.CompanyId == companyId)
                        .Select(t => t.Id)
                        .ToListAsync();
                    
                    var beneficiaryIds = await context.PropertyBeneficiaries
                        .Where(b => b.CompanyId == companyId)
                        .Select(b => b.Id)
                        .ToListAsync();
                    
                    query = (IOrderedQueryable<Reminder>)query.Where(r => 
                        (r.RelatedEntityType == "Property" && propertyIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "Company" && r.RelatedEntityId == companyId)
                    );
                }
                
                var reminders = await query.ToListAsync();
                
                response.Response = reminders;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving active reminders: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetOverdueReminders(string userId = null, int companyId = 0)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get statuses that are considered "active"
                var activeStatusIds = await context.ReminderStatuses
                    .Where(rs => rs.Name.ToLower() != "completed" && rs.Name.ToLower() != "cancelled")
                    .Select(rs => rs.Id)
                    .ToListAsync();
                
                // Current date for overdue calculation
                var today = DateTime.Today;
                
                // Base query for overdue reminders
                var query = context.Reminders
                    .Include(r => r.ReminderType)
                    .Include(r => r.ReminderStatus)
                    .Include(r => r.RecurrenceFrequency)
                    .Where(r => activeStatusIds.Contains(r.ReminderStatusId) && r.DueDate < today)
                    .OrderBy(r => r.DueDate);
                
                // Filter by user if specified
                if (!string.IsNullOrEmpty(userId))
                {
                    query = (IOrderedQueryable<Reminder>)query.Where(r => r.AssignedToUserId == userId);
                }
                
                // Filter by company if specified
                if (companyId > 0)
                {
                    // Get entity IDs for this company
                    var propertyIds = await context.Properties
                        .Where(p => p.CompanyId == companyId)
                        .Select(p => p.Id)
                        .ToListAsync();
                    
                    var ownerIds = await context.PropertyOwners
                        .Where(o => o.CompanyId == companyId)
                        .Select(o => o.Id)
                        .ToListAsync();
                    
                    var tenantIds = await context.PropertyTenants
                        .Where(t => t.CompanyId == companyId)
                        .Select(t => t.Id)
                        .ToListAsync();
                    
                    var beneficiaryIds = await context.PropertyBeneficiaries
                        .Where(b => b.CompanyId == companyId)
                        .Select(b => b.Id)
                        .ToListAsync();
                    
                    query = (IOrderedQueryable<Reminder>)query.Where(r => 
                        (r.RelatedEntityType == "Property" && propertyIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "Company" && r.RelatedEntityId == companyId)
                    );
                }
                
                var reminders = await query.ToListAsync();
                
                response.Response = reminders;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving overdue reminders: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetUpcomingReminders(int days = 7, string userId = null, int companyId = 0)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get statuses that are considered "active"
                var activeStatusIds = await context.ReminderStatuses
                    .Where(rs => rs.Name.ToLower() != "completed" && rs.Name.ToLower() != "cancelled")
                    .Select(rs => rs.Id)
                    .ToListAsync();
                
                // Calculate date range
                var startDate = DateTime.Today;
                var endDate = startDate.AddDays(days);
                
                // Base query for upcoming reminders
                var query = context.Reminders
                    .Include(r => r.ReminderType)
                    .Include(r => r.ReminderStatus)
                    .Include(r => r.RecurrenceFrequency)
                    .Where(r => activeStatusIds.Contains(r.ReminderStatusId) && 
                           r.DueDate >= startDate && r.DueDate <= endDate)
                    .OrderBy(r => r.DueDate);
                
                // Filter by user if specified
                if (!string.IsNullOrEmpty(userId))
                {
                    query = (IOrderedQueryable<Reminder>)query.Where(r => r.AssignedToUserId == userId);
                }
                
                // Filter by company if specified
                if (companyId > 0)
                {
                    // Get entity IDs for this company
                    var propertyIds = await context.Properties
                        .Where(p => p.CompanyId == companyId)
                        .Select(p => p.Id)
                        .ToListAsync();
                    
                    var ownerIds = await context.PropertyOwners
                        .Where(o => o.CompanyId == companyId)
                        .Select(o => o.Id)
                        .ToListAsync();
                    
                    var tenantIds = await context.PropertyTenants
                        .Where(t => t.CompanyId == companyId)
                        .Select(t => t.Id)
                        .ToListAsync();
                    
                    var beneficiaryIds = await context.PropertyBeneficiaries
                        .Where(b => b.CompanyId == companyId)
                        .Select(b => b.Id)
                        .ToListAsync();
                    
                    query = (IOrderedQueryable<Reminder>)query.Where(r => 
                        (r.RelatedEntityType == "Property" && propertyIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(r.RelatedEntityId.Value)) ||
                        (r.RelatedEntityType == "Company" && r.RelatedEntityId == companyId)
                    );
                }
                
                var reminders = await query.ToListAsync();
                
                response.Response = reminders;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving upcoming reminders: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> CreateRecurringReminder(Reminder reminder, int recurrenceFrequencyId, int recurrenceInterval, DateTime? endDate = null)
        {
            var response = new ResponseModel();
            
            try
            {
                // Set recurring properties
                reminder.IsRecurring = true;
                reminder.RecurrenceFrequencyId = recurrenceFrequencyId;
                reminder.RecurrenceInterval = recurrenceInterval;
                reminder.RecurrenceEndDate = endDate;
                
                // Create the initial reminder
                return await CreateReminder(reminder);
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating recurring reminder: {ex.Message}";
                return response;
            }
        }

        public async Task<ResponseModel> UpdateRecurringReminder(int id, Reminder updatedReminder, bool updateAllInstances = false)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var reminder = await context.Reminders.FindAsync(id);
                
                if (reminder == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Reminder with ID {id} not found";
                    return response;
                }
                
                if (!updateAllInstances)
                {
                    // Update only this instance
                    return await UpdateReminder(id, updatedReminder);
                }
                
                // For updating all instances, we need to find all reminders with the same properties
                if (!reminder.IsRecurring || 
                    string.IsNullOrEmpty(reminder.RelatedEntityType) || 
                    !reminder.RelatedEntityId.HasValue)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot update all instances: not a valid recurring reminder";
                    return response;
                }
                
                // Find all related reminders
                var relatedReminders = await context.Reminders
                    .Where(r => r.RelatedEntityType == reminder.RelatedEntityType &&
                           r.RelatedEntityId == reminder.RelatedEntityId &&
                           r.Title == reminder.Title &&
                           r.IsRecurring &&
                           r.Id != id) // Exclude current reminder as we'll update it separately
                    .ToListAsync();
                
                // Update all related reminders
                foreach (var relatedReminder in relatedReminders)
                {
                    relatedReminder.Description = updatedReminder.Description;
                    relatedReminder.ReminderTypeId = updatedReminder.ReminderTypeId;
                    relatedReminder.SendNotification = updatedReminder.SendNotification;
                    relatedReminder.NotifyDaysBefore = updatedReminder.NotifyDaysBefore;
                    relatedReminder.RecurrenceFrequencyId = updatedReminder.RecurrenceFrequencyId;
                    relatedReminder.RecurrenceInterval = updatedReminder.RecurrenceInterval;
                    relatedReminder.RecurrenceEndDate = updatedReminder.RecurrenceEndDate;
                    relatedReminder.UpdatedDate = DateTime.Now;
                    relatedReminder.UpdatedBy = updatedReminder.UpdatedBy;
                    
                    // Don't update the due date of already scheduled reminders,
                    // only the recurrence pattern for future instances
                }
                
                await context.SaveChangesAsync();
                
                // Update the current reminder separately
                await UpdateReminder(id, updatedReminder);
                
                response.Response = true;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Updated {relatedReminders.Count + 1} reminder instances";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating recurring reminders: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> ProcessRecurringReminders()
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get completed recurring reminders without next instances created
                var completedStatusId = await context.ReminderStatuses
                    .Where(rs => rs.Name.ToLower() == "completed")
                    .Select(rs => rs.Id)
                    .FirstOrDefaultAsync();
                
                if (completedStatusId == 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Completed status not found";
                    return response;
                }
                
                var recurringReminders = await context.Reminders
                    .Include(r => r.RecurrenceFrequency)
                    .Where(r => r.IsRecurring && 
                           r.RecurrenceFrequencyId.HasValue && 
                           r.RecurrenceInterval.HasValue &&
                           r.ReminderStatusId == completedStatusId &&
                           (r.RecurrenceEndDate == null || r.RecurrenceEndDate > DateTime.Today))
                    .ToListAsync();
                
                int createdCount = 0;
                
                foreach (var reminder in recurringReminders)
                {
                    // Check if we already created the next instance
                    var existingNext = await context.Reminders
                        .AnyAsync(r => r.RelatedEntityType == reminder.RelatedEntityType &&
                               r.RelatedEntityId == reminder.RelatedEntityId &&
                               r.Title == reminder.Title &&
                               r.ReminderStatusId != completedStatusId &&
                               r.DueDate > reminder.DueDate);
                    
                    if (!existingNext)
                    {
                        await CreateNextRecurringInstance(reminder);
                        createdCount++;
                    }
                }
                
                response.Response = createdCount;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Created {createdCount} new recurring reminders";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error processing recurring reminders: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> SendReminderNotifications()
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get active reminders due in the next few days
                var activeStatusIds = await context.ReminderStatuses
                    .Where(rs => rs.Name.ToLower() != "completed" && rs.Name.ToLower() != "cancelled")
                    .Select(rs => rs.Id)
                    .ToListAsync();
                
                var today = DateTime.Today;
                var upcomingReminders = await context.Reminders
                    .Include(r => r.ReminderType)
                    .Where(r => activeStatusIds.Contains(r.ReminderStatusId) && 
                           r.SendNotification && 
                           r.DueDate > today &&
                           r.AssignedToUserId != null)
                    .ToListAsync();
                
                int notificationCount = 0;
                
                foreach (var reminder in upcomingReminders)
                {
                    if (reminder.NotifyDaysBefore.HasValue)
                    {
                        int daysDue = (reminder.DueDate - today).Days;
                        
                        // Check if today is the notification day
                        if (daysDue == reminder.NotifyDaysBefore.Value)
                        {
                            string title = $"Reminder: {reminder.Title}";
                            string message = $"You have a reminder due on {reminder.DueDate:yyyy-MM-dd}: {reminder.Title}";
                            if (!string.IsNullOrEmpty(reminder.Description))
                            {
                                message += $"\n\n{reminder.Description}";
                            }
                            
                            // Send notification
                            var notificationResult = await _notificationService.SendNotification(
                                "ReminderDue", 
                                title, 
                                message, 
                                reminder.AssignedToUserId, 
                                reminder.RelatedEntityType, 
                                reminder.RelatedEntityId ?? 0);
                            
                            if (notificationResult.ResponseInfo.Success)
                            {
                                notificationCount++;
                            }
                        }
                    }
                }
                
                response.Response = notificationCount;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Sent {notificationCount} reminder notifications";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending reminder notifications: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> CreateBulkReminders(List<Reminder> reminders)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Set creation date for all reminders
                foreach (var reminder in reminders)
                {
                    reminder.CreatedOn = DateTime.Now;
                }
                
                // Add the reminders
                await context.Reminders.AddRangeAsync(reminders);
                await context.SaveChangesAsync();
                
                // Log the actions (simplified for bulk operations)
                foreach (var reminder in reminders)
                {
                    if (reminder.RelatedEntityId.HasValue)
                    {
                        await _auditService.LogEntityChange(
                            reminder.RelatedEntityType, 
                            reminder.RelatedEntityId.Value, 
                            reminder.CreatedBy, 
                            "CreateReminder", 
                            $"Bulk created reminder: {reminder.Title} due on {reminder.DueDate:yyyy-MM-dd}");
                    }
                }
                
                response.Response = reminders;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"{reminders.Count} reminders created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating bulk reminders: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> DeleteRemindersByEntity(string entityType, object entityId, string userId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                IQueryable<Reminder> query = context.Reminders.Where(r => r.RelatedEntityType == entityType);
                
                // Handle different ID types
                if (entityId is int intId)
                {
                    query = query.Where(r => r.RelatedEntityId == intId);
                }
                else if (entityId is string stringId)
                {
                    query = query.Where(r => r.RelatedEntityStringId == stringId);
                }
                
                var remindersToDelete = await query.ToListAsync();
                
                if (!remindersToDelete.Any())
                {
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "No reminders found to delete";
                    return response;
                }
                
                context.Reminders.RemoveRange(remindersToDelete);
                await context.SaveChangesAsync();
                
                // Log the bulk deletion
                if (entityId is int intEntityId)
                {
                    await _auditService.LogEntityChange(
                        entityType, 
                        intEntityId, 
                        userId, 
                        "DeleteReminders", 
                        $"Deleted {remindersToDelete.Count} reminders for entity");
                }
                
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"{remindersToDelete.Count} reminders deleted successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting reminders by entity: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetReminderStatistics(int companyId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get entities associated with the company
                var propertyIds = await context.Properties
                    .Where(p => p.CompanyId == companyId)
                    .Select(p => p.Id)
                    .ToListAsync();
                
                var ownerIds = await context.PropertyOwners
                    .Where(o => o.CompanyId == companyId)
                    .Select(o => o.Id)
                    .ToListAsync();
                
                var tenantIds = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId)
                    .Select(t => t.Id)
                    .ToListAsync();
                
                var beneficiaryIds = await context.PropertyBeneficiaries
                    .Where(b => b.CompanyId == companyId)
                    .Select(b => b.Id)
                    .ToListAsync();
                
                // Define the filtering condition for reminders related to this company
                var companyRemindersQuery = context.Reminders.Where(r => 
                    (r.RelatedEntityType == "Property" && propertyIds.Contains(r.RelatedEntityId.Value)) ||
                    (r.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(r.RelatedEntityId.Value)) ||
                    (r.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(r.RelatedEntityId.Value)) ||
                    (r.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(r.RelatedEntityId.Value)) ||
                    (r.RelatedEntityType == "Company" && r.RelatedEntityId == companyId)
                );
                
                // Get statistics
                var today = DateTime.Today;
                
                var totalReminders = await companyRemindersQuery.CountAsync();
                var completedReminders = await companyRemindersQuery
                    .CountAsync(r => r.CompletedDate.HasValue);
                var overdueReminders = await companyRemindersQuery
                    .CountAsync(r => !r.CompletedDate.HasValue && r.DueDate < today);
                var upcomingReminders = await companyRemindersQuery
                    .CountAsync(r => !r.CompletedDate.HasValue && r.DueDate >= today && r.DueDate <= today.AddDays(7));
                var recurringReminders = await companyRemindersQuery
                    .CountAsync(r => r.IsRecurring);
                
                var remindersByStatus = await companyRemindersQuery
                    .GroupBy(r => r.ReminderStatusId)
                    .Select(g => new { StatusId = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                var remindersByType = await companyRemindersQuery
                    .GroupBy(r => r.ReminderTypeId)
                    .Select(g => new { TypeId = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                var remindersByEntityType = await companyRemindersQuery
                    .GroupBy(r => r.RelatedEntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                var statuses = await context.ReminderStatuses.ToListAsync();
                var types = await context.ReminderTypes.ToListAsync();
                
                // Users with most reminders assigned
                var userReminders = await companyRemindersQuery
                    .Where(r => r.AssignedToUserId != null)
                    .GroupBy(r => r.AssignedToUserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();
                
                // Combine results
                var statistics = new
                {
                    TotalReminders = totalReminders,
                    CompletedReminders = completedReminders,
                    OverdueReminders = overdueReminders,
                    UpcomingReminders = upcomingReminders,
                    RecurringReminders = recurringReminders,
                    RemindersByStatus = remindersByStatus.Select(r => new 
                    {
                        StatusId = r.StatusId,
                        StatusName = statuses.FirstOrDefault(s => s.Id == r.StatusId)?.Name,
                        Count = r.Count
                    }),
                    RemindersByType = remindersByType.Select(r => new 
                    {
                        TypeId = r.TypeId,
                        TypeName = types.FirstOrDefault(t => t.Id == r.TypeId)?.Name,
                        Count = r.Count
                    }),
                    RemindersByEntityType = remindersByEntityType,
                    UsersWithMostReminders = userReminders,
                    CompletionRatePercent = totalReminders > 0 ? 
                        Math.Round((double)completedReminders / totalReminders * 100, 1) : 0
                };
                
                response.Response = statistics;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving reminder statistics: {ex.Message}";
            }
            
            return response;
        }

        #region Helper Methods

        private async Task<Reminder> CreateNextRecurringInstance(Reminder completedReminder)
        {
            using var context = _contextFactory.CreateDbContext();
            
            // Get the RecurrenceFrequency to determine how to calculate next date
            var frequency = await context.RecurrenceFrequencies
                .FirstOrDefaultAsync(rf => rf.Id == completedReminder.RecurrenceFrequencyId);
            
            if (frequency == null || !completedReminder.RecurrenceInterval.HasValue)
            {
                return null;
            }
            
            // Get the "Pending" status ID
            var pendingStatus = await context.ReminderStatuses
                .FirstOrDefaultAsync(rs => rs.Name.ToLower() == "pending");
            
            if (pendingStatus == null)
            {
                return null;
            }
            
            // Calculate next due date
            DateTime nextDueDate;
            
            if (frequency.DaysMultiplier.HasValue)
            {
                // Calculate based on frequency days multiplier (e.g., Daily=1, Weekly=7)
                int daysToAdd = frequency.DaysMultiplier.Value * completedReminder.RecurrenceInterval.Value;
                nextDueDate = completedReminder.DueDate.AddDays(daysToAdd);
            }
            else
            {
                // Fallback to calculating based on frequency name
                switch (frequency.Name.ToLower())
                {
                    case "daily":
                        nextDueDate = completedReminder.DueDate.AddDays(completedReminder.RecurrenceInterval.Value);
                        break;
                    case "weekly":
                        nextDueDate = completedReminder.DueDate.AddDays(7 * completedReminder.RecurrenceInterval.Value);
                        break;
                    case "monthly":
                        nextDueDate = completedReminder.DueDate.AddMonths(completedReminder.RecurrenceInterval.Value);
                        break;
                    case "quarterly":
                        nextDueDate = completedReminder.DueDate.AddMonths(3 * completedReminder.RecurrenceInterval.Value);
                        break;
                    case "yearly":
                    case "annually":
                        nextDueDate = completedReminder.DueDate.AddYears(completedReminder.RecurrenceInterval.Value);
                        break;
                    default:
                        // Default to daily if unknown frequency
                        nextDueDate = completedReminder.DueDate.AddDays(completedReminder.RecurrenceInterval.Value);
                        break;
                }
            }
            
            // Check if next due date is within recurrence end date
            if (completedReminder.RecurrenceEndDate.HasValue && nextDueDate > completedReminder.RecurrenceEndDate.Value)
            {
                // Past the end date, don't create a new instance
                return null;
            }
            
            // Create new reminder instance
            var newReminder = new Reminder
            {
                Title = completedReminder.Title,
                Description = completedReminder.Description,
                ReminderTypeId = completedReminder.ReminderTypeId,
                ReminderStatusId = pendingStatus.Id,
                DueDate = nextDueDate,
                IsRecurring = true,
                RecurrenceFrequencyId = completedReminder.RecurrenceFrequencyId,
                RecurrenceInterval = completedReminder.RecurrenceInterval,
                RecurrenceEndDate = completedReminder.RecurrenceEndDate,
                SendNotification = completedReminder.SendNotification,
                NotifyDaysBefore = completedReminder.NotifyDaysBefore,
                RelatedEntityType = completedReminder.RelatedEntityType,
                RelatedEntityId = completedReminder.RelatedEntityId,
                RelatedEntityStringId = completedReminder.RelatedEntityStringId,
                AssignedToUserId = completedReminder.AssignedToUserId,
                CreatedOn = DateTime.Now,
                CreatedBy = completedReminder.CreatedBy
            };
            
            await context.Reminders.AddAsync(newReminder);
            await context.SaveChangesAsync();
            
            // Log the creation
            if (newReminder.RelatedEntityId.HasValue)
            {
                await _auditService.LogEntityChange(
                    newReminder.RelatedEntityType, 
                    newReminder.RelatedEntityId.Value, 
                    newReminder.CreatedBy, 
                    "CreateReminderRecurrence", 
                    $"Created recurring reminder: {newReminder.Title} due on {newReminder.DueDate:yyyy-MM-dd}");
            }
            
            return newReminder;
        }
        
        #endregion
    }
}