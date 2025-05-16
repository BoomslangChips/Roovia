using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;

namespace Roovia.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public NotificationService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IEmailService emailService,
            IAuditService auditService)
        {
            _contextFactory = contextFactory;
            _emailService = emailService;
            _auditService = auditService;
        }

        public async Task<ResponseModel> CreateNotification(Notification notification)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set creation date and defaults
                notification.CreatedDate = DateTime.Now;
                notification.IsRead = false;
                notification.ReadDate = null;

                await context.Notifications.AddAsync(notification);
                await context.SaveChangesAsync();

                // Send via email if user has email notifications enabled
                if (!string.IsNullOrEmpty(notification.RecipientUserId))
                {
                    var preference = await context.NotificationPreferences
                        .FirstOrDefaultAsync(np =>
                            np.RelatedEntityType == "User" &&
                            np.RelatedEntityStringId == notification.RecipientUserId &&
                            np.NotificationEventTypeId == notification.NotificationEventTypeId);

                    if (preference != null && preference.EmailEnabled)
                    {
                        var user = await context.Users
                            .Include(u => u.EmailAddresses)
                            .FirstOrDefaultAsync(u => u.Id == notification.RecipientUserId);

                        if (user != null && user.EmailAddresses.Any(e => e.IsPrimary))
                        {
                            var email = user.EmailAddresses.First(e => e.IsPrimary).EmailAddress;

                            // Send email
                            await _emailService.SendEmailAsync(
                                email,
                                notification.Title,
                                notification.Message,
                                true);

                            // Update notification
                            notification.EmailSent = true;
                            notification.EmailSentDate = DateTime.Now;
                            await context.SaveChangesAsync();
                        }
                    }

                    // Similarly for SMS
                    if (preference != null && preference.SmsEnabled)
                    {
                        var user = await context.Users
                            .Include(u => u.ContactNumbers)
                            .FirstOrDefaultAsync(u => u.Id == notification.RecipientUserId);

                        if (user != null && user.ContactNumbers.Any(c => c.IsPrimary))
                        {
                            // Code for SMS sending would go here
                            // This is a placeholder for actual SMS implementation

                            // Update notification
                            notification.SmsSent = true;
                            notification.SmsSentDate = DateTime.Now;
                            await context.SaveChangesAsync();
                        }
                    }
                }

                response.Response = notification;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Notification created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating notification: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetNotificationById(int id)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var notification = await context.Notifications
                    .Include(n => n.NotificationEventType)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (notification == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Notification with ID {id} not found";
                    return response;
                }

                response.Response = notification;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving notification: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> MarkNotificationAsRead(int id, string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var notification = await context.Notifications.FindAsync(id);

                if (notification == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Notification with ID {id} not found";
                    return response;
                }

                // Check if the notification belongs to the user
                if (notification.RecipientUserId != userId)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "You don't have permission to mark this notification as read";
                    return response;
                }

                // Mark as read
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;

                await context.SaveChangesAsync();

                response.Response = notification;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Notification marked as read";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error marking notification as read: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> DeleteNotification(int id, string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var notification = await context.Notifications.FindAsync(id);

                if (notification == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Notification with ID {id} not found";
                    return response;
                }

                // Check if the notification belongs to the user
                if (notification.RecipientUserId != userId)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "You don't have permission to delete this notification";
                    return response;
                }

                context.Notifications.Remove(notification);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Notification deleted successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting notification: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetUserNotifications(string userId, bool includeRead = false, int page = 1, int pageSize = 20)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var query = context.Notifications
                    .Include(n => n.NotificationEventType)
                    .Where(n => n.RecipientUserId == userId);

                if (!includeRead)
                {
                    query = query.Where(n => !n.IsRead);
                }

                // Calculate total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var notifications = await query
                    .OrderByDescending(n => n.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    Notifications = notifications,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving user notifications: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetUnreadNotificationCount(string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var count = await context.Notifications
                    .CountAsync(n => n.RecipientUserId == userId && !n.IsRead);

                response.Response = count;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving unread notification count: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetNotificationsByEntity(string entityType, int entityId, string userId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var query = context.Notifications
                    .Include(n => n.NotificationEventType)
                    .Where(n => n.RelatedEntityType == entityType && n.RelatedEntityId == entityId);

                // Filter by user if specified
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(n => n.RecipientUserId == userId);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedDate)
                    .ToListAsync();

                response.Response = notifications;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving entity notifications: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetUserNotificationPreferences(string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var preferences = await context.NotificationPreferences
                    .Include(np => np.NotificationEventType)
                    .Where(np => np.RelatedEntityType == "User" && np.RelatedEntityStringId == userId)
                    .ToListAsync();

                // Get all notification event types to ensure coverage of all possible notifications
                var allEventTypes = await context.NotificationEventTypes
                    .Where(net => net.IsActive)
                    .ToListAsync();

                // Create default preferences for any missing event types
                var existingEventTypeIds = preferences.Select(p => p.NotificationEventTypeId).ToList();
                var missingEventTypes = allEventTypes.Where(et => !existingEventTypeIds.Contains(et.Id)).ToList();

                var result = new
                {
                    ExistingPreferences = preferences,
                    MissingEventTypes = missingEventTypes
                };

                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving notification preferences: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> UpdateNotificationPreference(int preferenceId, bool emailEnabled, bool smsEnabled, bool pushEnabled, bool webEnabled)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var preference = await context.NotificationPreferences.FindAsync(preferenceId);

                if (preference == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Notification preference with ID {preferenceId} not found";
                    return response;
                }

                // Update preferences
                preference.EmailEnabled = emailEnabled;
                preference.SmsEnabled = smsEnabled;
                preference.PushEnabled = pushEnabled;
                preference.WebEnabled = webEnabled;
                preference.UpdatedDate = DateTime.Now;

                await context.SaveChangesAsync();

                response.Response = preference;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Notification preferences updated successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating notification preference: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> CreateNotificationPreference(NotificationPreference preference)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Check if preference already exists
                var existingPreference = await context.NotificationPreferences
                    .FirstOrDefaultAsync(np =>
                        np.RelatedEntityType == preference.RelatedEntityType &&
                        (
                            (np.RelatedEntityId.HasValue && np.RelatedEntityId == preference.RelatedEntityId) ||
                            (np.RelatedEntityStringId == preference.RelatedEntityStringId)
                        ) &&
                        np.NotificationEventTypeId == preference.NotificationEventTypeId);

                if (existingPreference != null)
                {
                    // Update existing preference
                    existingPreference.EmailEnabled = preference.EmailEnabled;
                    existingPreference.SmsEnabled = preference.SmsEnabled;
                    existingPreference.PushEnabled = preference.PushEnabled;
                    existingPreference.WebEnabled = preference.WebEnabled;
                    existingPreference.OnlyDuringBusinessHours = preference.OnlyDuringBusinessHours;
                    existingPreference.PreferredTimeOfDay = preference.PreferredTimeOfDay;
                    existingPreference.UpdatedDate = DateTime.Now;
                    existingPreference.UpdatedBy = preference.CreatedBy;

                    await context.SaveChangesAsync();

                    response.Response = existingPreference;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Notification preference updated successfully";
                }
                else
                {
                    // Create new preference
                    preference.CreatedOn = DateTime.Now;

                    await context.NotificationPreferences.AddAsync(preference);
                    await context.SaveChangesAsync();

                    response.Response = preference;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Notification preference created successfully";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating notification preference: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetNotificationTemplates(int companyId, string eventType = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var query = context.NotificationTemplates
                    .Include(nt => nt.NotificationEventType)
                    .Where(nt => nt.CompanyId == companyId || nt.CompanyId == null);

                // Filter by event type if specified
                if (!string.IsNullOrEmpty(eventType))
                {
                    var eventTypes = await context.NotificationEventTypes
                        .Where(net => net.SystemName.Contains(eventType) || net.Name.Contains(eventType))
                        .Select(net => net.Id)
                        .ToListAsync();

                    query = query.Where(nt => eventTypes.Contains(nt.NotificationEventTypeId));
                }

                var templates = await query.ToListAsync();

                response.Response = templates;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving notification templates: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> UpdateNotificationTemplate(int templateId, string subject, string bodyTemplate, string smsTemplate)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var template = await context.NotificationTemplates.FindAsync(templateId);

                if (template == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Notification template with ID {templateId} not found";
                    return response;
                }

                // Update template
                template.Subject = subject;
                template.BodyTemplate = bodyTemplate;
                template.SmsTemplate = smsTemplate;

                await context.SaveChangesAsync();

                response.Response = template;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Notification template updated successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating notification template: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendNotification(string eventType, string title, string message, string recipientUserId, string relatedEntityType = null, int? relatedEntityId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find notification event type
                var eventTypeObj = await context.NotificationEventTypes
                    .FirstOrDefaultAsync(net => net.SystemName == eventType);

                if (eventTypeObj == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Notification event type '{eventType}' not found";
                    return response;
                }

                // Create notification
                var notification = new Notification
                {
                    NotificationEventTypeId = eventTypeObj.Id,
                    Title = title,
                    Message = message,
                    RecipientUserId = recipientUserId,
                    CreatedDate = DateTime.Now,
                    IsRead = false,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId
                };

                // Use the CreateNotification method to handle sending via channels
                var createResult = await CreateNotification(notification);

                if (!createResult.ResponseInfo.Success)
                {
                    return createResult;
                }

                response.Response = createResult.Response;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Notification sent to user {recipientUserId}";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending notification: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendBulkNotifications(string eventType, string title, string message, List<string> recipientUserIds, string relatedEntityType = null, int? relatedEntityId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find notification event type
                var eventTypeObj = await context.NotificationEventTypes
                    .FirstOrDefaultAsync(net => net.SystemName == eventType);

                if (eventTypeObj == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Notification event type '{eventType}' not found";
                    return response;
                }

                // Create notifications
                var notifications = new List<Notification>();

                foreach (var userId in recipientUserIds)
                {
                    var notification = new Notification
                    {
                        NotificationEventTypeId = eventTypeObj.Id,
                        Title = title,
                        Message = message,
                        RecipientUserId = userId,
                        CreatedDate = DateTime.Now,
                        IsRead = false,
                        RelatedEntityType = relatedEntityType,
                        RelatedEntityId = relatedEntityId
                    };

                    notifications.Add(notification);
                }

                // Add all notifications
                await context.Notifications.AddRangeAsync(notifications);
                await context.SaveChangesAsync();

                // Process notification channels (email, SMS) for each user
                foreach (var notification in notifications)
                {
                    // Check user preferences
                    var preference = await context.NotificationPreferences
                        .FirstOrDefaultAsync(np =>
                            np.RelatedEntityType == "User" &&
                            np.RelatedEntityStringId == notification.RecipientUserId &&
                            np.NotificationEventTypeId == notification.NotificationEventTypeId);

                    if (preference != null)
                    {
                        // Send via email if enabled
                        if (preference.EmailEnabled)
                        {
                            var user = await context.Users
                                .Include(u => u.EmailAddresses)
                                .FirstOrDefaultAsync(u => u.Id == notification.RecipientUserId);

                            if (user != null && user.EmailAddresses.Any(e => e.IsPrimary))
                            {
                                var email = user.EmailAddresses.First(e => e.IsPrimary).EmailAddress;

                                await _emailService.SendEmailAsync(
                                    email,
                                    notification.Title,
                                    notification.Message,
                                    true);

                                notification.EmailSent = true;
                                notification.EmailSentDate = DateTime.Now;
                            }
                        }

                        // Send via SMS if enabled
                        if (preference.SmsEnabled)
                        {
                            var user = await context.Users
                                .Include(u => u.ContactNumbers)
                                .FirstOrDefaultAsync(u => u.Id == notification.RecipientUserId);

                            if (user != null && user.ContactNumbers.Any(c => c.IsPrimary))
                            {
                                // Code for SMS sending would go here
                                // This is a placeholder for actual SMS implementation

                                notification.SmsSent = true;
                                notification.SmsSentDate = DateTime.Now;
                            }
                        }
                    }
                }

                // Save channel status
                await context.SaveChangesAsync();

                response.Response = notifications.Count;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Sent {notifications.Count} notifications";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending bulk notifications: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendEntityNotification(string eventType, string title, string message, string relatedEntityType, int relatedEntityId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find relevant users based on entity type
                List<string> recipientUserIds = new List<string>();

                switch (relatedEntityType.ToLower())
                {
                    case "property":
                        // Find users associated with the property
                        var property = await context.Properties
                            .Include(p => p.Company)
                                .ThenInclude(c => c.Users)
                            .FirstOrDefaultAsync(p => p.Id == relatedEntityId);

                        if (property != null)
                        {
                            // Add company users with property permissions
                            var userIds = property.Company.Users
                                .Where(u => u.IsActive)
                                .Select(u => u.Id)
                                .ToList();

                            recipientUserIds.AddRange(userIds);
                        }
                        break;

                    case "propertytenant":
                        // Add property manager
                        var tenant = await context.PropertyTenants
                            .Include(t => t.Property)
                            .FirstOrDefaultAsync(t => t.Id == relatedEntityId);

                        if (tenant != null && !string.IsNullOrEmpty(tenant.ResponsibleUser))
                        {
                            recipientUserIds.Add(tenant.ResponsibleUser);
                        }
                        break;

                    case "maintenanceticket":
                        // Add assigned user
                        var ticket = await context.MaintenanceTickets
                            .FirstOrDefaultAsync(t => t.Id == relatedEntityId);

                        if (ticket != null && !string.IsNullOrEmpty(ticket.AssignedToUserId))
                        {
                            recipientUserIds.Add(ticket.AssignedToUserId);
                        }
                        break;

                    default:
                        // For other entity types, find users with relevant permissions
                        // This would require a more complex query based on permissions
                        break;
                }

                // If no recipients found, return error
                if (!recipientUserIds.Any())
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "No recipients found for the specified entity";
                    return response;
                }

                // Send notifications to all recipients
                return await SendBulkNotifications(
                    eventType,
                    title,
                    message,
                    recipientUserIds,
                    relatedEntityType,
                    relatedEntityId);
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending entity notification: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendEmailNotification(string email, string subject, string body)
        {
            var response = new ResponseModel();

            try
            {
                // Call email service to send the email
                await _emailService.SendEmailAsync(email, subject, body, true);

                // Log the email send
                using var context = _contextFactory.CreateDbContext();

                // Try to find the user by email
                var userEmail = await context.Emails
                    .FirstOrDefaultAsync(e => e.EmailAddress == email);

                if (userEmail != null && userEmail.ApplicationUserId != null)
                {
                    await _auditService.LogEntityChange(
                        "User",
                        0, // Not applicable
                        "System", // System-generated
                        "SendEmail",
                        $"Sent email '{subject}' to {email}");
                }

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Email notification sent to {email}";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending email notification: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendSmsNotification(string phoneNumber, string message)
        {
            var response = new ResponseModel();

            try
            {
                // This is a placeholder for actual SMS sending implementation
                // Would integrate with an SMS service provider

                // Log the SMS send
                using var context = _contextFactory.CreateDbContext();

                // Try to find the user by phone number
                var userPhone = await context.ContactNumbers
                    .FirstOrDefaultAsync(c => c.Number == phoneNumber);

                if (userPhone != null && userPhone.ApplicationUserId != null)
                {
                    await _auditService.LogEntityChange(
                        "User",
                        0, // Not applicable
                        "System", // System-generated
                        "SendSms",
                        $"Sent SMS to {phoneNumber}");
                }

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"SMS notification sent to {phoneNumber}";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending SMS notification: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendPushNotification(string userId, string title, string message)
        {
            var response = new ResponseModel();

            try
            {
                // This is a placeholder for actual push notification implementation
                // Would integrate with a push notification service

                // Log the push notification
                await _auditService.LogEntityChange(
                    "User",
                    0, // Not applicable
                    "System", // System-generated
                    "SendPushNotification",
                    $"Sent push notification '{title}' to user {userId}");

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Push notification sent to user {userId}";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending push notification: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> MarkAllNotificationsAsRead(string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var notifications = await context.Notifications
                    .Where(n => n.RecipientUserId == userId && !n.IsRead)
                    .ToListAsync();

                if (!notifications.Any())
                {
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "No unread notifications found";
                    return response;
                }

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadDate = DateTime.Now;
                }

                await context.SaveChangesAsync();

                response.Response = notifications.Count;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Marked {notifications.Count} notifications as read";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error marking all notifications as read: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> DeleteAllNotifications(string userId, bool onlyRead = true)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var query = context.Notifications
                    .Where(n => n.RecipientUserId == userId);

                if (onlyRead)
                {
                    query = query.Where(n => n.IsRead);
                }

                var notifications = await query.ToListAsync();

                if (!notifications.Any())
                {
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = onlyRead ? "No read notifications found" : "No notifications found";
                    return response;
                }

                context.Notifications.RemoveRange(notifications);
                await context.SaveChangesAsync();

                response.Response = notifications.Count;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Deleted {notifications.Count} notifications";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting notifications: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> ProcessScheduledNotifications()
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Process different types of scheduled notifications

                // 1. Lease expiry notifications
                await ProcessLeaseExpiryNotifications(context);

                // 2. Rent due notifications
                await ProcessRentDueNotifications(context);

                // 3. Maintenance due notifications
                await ProcessMaintenanceDueNotifications(context);

                // 4. Inspection scheduled notifications
                await ProcessInspectionNotifications(context);

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Processed scheduled notifications";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error processing scheduled notifications: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> CleanupOldNotifications(int daysToKeep = 90)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                var oldNotifications = await context.Notifications
                    .Where(n => n.CreatedDate < cutoffDate && n.IsRead)
                    .ToListAsync();

                if (!oldNotifications.Any())
                {
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "No old notifications to clean up";
                    return response;
                }

                context.Notifications.RemoveRange(oldNotifications);
                await context.SaveChangesAsync();

                response.Response = oldNotifications.Count;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Cleaned up {oldNotifications.Count} old notifications";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error cleaning up old notifications: {ex.Message}";
            }

            return response;
        }

        #region Helper Methods

        private async Task ProcessLeaseExpiryNotifications(ApplicationDbContext context)
        {
            var today = DateTime.Today;
            var thirtyDaysFromNow = today.AddDays(30);
            var sixtyDaysFromNow = today.AddDays(60);
            var ninetyDaysFromNow = today.AddDays(90);

            // Find leases expiring in 30, 60, and 90 days
            var expiringLeases = await context.PropertyTenants
                .Include(t => t.Property)
                .Where(t =>
                    (t.LeaseEndDate == thirtyDaysFromNow ||
                     t.LeaseEndDate == sixtyDaysFromNow ||
                     t.LeaseEndDate == ninetyDaysFromNow) &&
                    t.StatusId == 1) // Active tenants only
                .ToListAsync();

            foreach (var tenant in expiringLeases)
            {
                // Determine days until expiry
                int daysUntilExpiry = (tenant.LeaseEndDate - today).Days;

                // Find responsible users
                string responsibleUser = tenant.ResponsibleUser;

                if (!string.IsNullOrEmpty(responsibleUser))
                {
                    // Create notification
                    var notification = new Notification
                    {
                        NotificationEventTypeId = 1, // Lease expiry notification type ID
                        Title = $"Lease Expiring in {daysUntilExpiry} Days",
                        Message = $"The lease for property {tenant.Property.PropertyName} ({tenant.Property.PropertyCode}) " +
                                 $"with tenant {tenant.DisplayName} will expire on {tenant.LeaseEndDate:yyyy-MM-dd}.",
                        RecipientUserId = responsibleUser,
                        CreatedDate = DateTime.Now,
                        IsRead = false,
                        RelatedEntityType = "PropertyTenant",
                        RelatedEntityId = tenant.Id
                    };

                    await context.Notifications.AddAsync(notification);

                    // Send email if user has email notifications enabled
                    var preference = await context.NotificationPreferences
                        .FirstOrDefaultAsync(np =>
                            np.RelatedEntityType == "User" &&
                            np.RelatedEntityStringId == responsibleUser &&
                            np.NotificationEventTypeId == 1);

                    if (preference != null && preference.EmailEnabled)
                    {
                        var user = await context.Users
                            .Include(u => u.EmailAddresses)
                            .FirstOrDefaultAsync(u => u.Id == responsibleUser);

                        if (user != null && user.EmailAddresses.Any(e => e.IsPrimary))
                        {
                            var email = user.EmailAddresses.First(e => e.IsPrimary).EmailAddress;

                            await _emailService.SendLeaseExpiryReminderAsync(
                                tenant,
                                tenant.Property,
                                daysUntilExpiry);

                            notification.EmailSent = true;
                            notification.EmailSentDate = DateTime.Now;
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private async Task ProcessRentDueNotifications(ApplicationDbContext context)
        {
            var today = DateTime.Today;

            // Find tenants with rent due in 5 days
            var rentDueDay = today.Day + 5;
            if (rentDueDay > DateTime.DaysInMonth(today.Year, today.Month))
            {
                rentDueDay = rentDueDay - DateTime.DaysInMonth(today.Year, today.Month);
            }

            var tenantsWithRentDue = await context.PropertyTenants
                .Include(t => t.Property)
                .Where(t =>
                    t.DebitDayOfMonth == rentDueDay &&
                    t.StatusId == 1) // Active tenants only
                .ToListAsync();

            foreach (var tenant in tenantsWithRentDue)
            {
                // Find responsible users
                string responsibleUser = tenant.ResponsibleUser;

                if (!string.IsNullOrEmpty(responsibleUser))
                {
                    // Create notification
                    var notification = new Notification
                    {
                        NotificationEventTypeId = 2, // Rent due notification type ID
                        Title = "Rent Due Reminder",
                        Message = $"Rent of {tenant.RentAmount:C} is due in 5 days for property {tenant.Property.PropertyName} " +
                                 $"({tenant.Property.PropertyCode}) with tenant {tenant.DisplayName}.",
                        RecipientUserId = responsibleUser,
                        CreatedDate = DateTime.Now,
                        IsRead = false,
                        RelatedEntityType = "PropertyTenant",
                        RelatedEntityId = tenant.Id
                    };

                    await context.Notifications.AddAsync(notification);

                    // Send email if user has email notifications enabled
                    var preference = await context.NotificationPreferences
                        .FirstOrDefaultAsync(np =>
                            np.RelatedEntityType == "User" &&
                            np.RelatedEntityStringId == responsibleUser &&
                            np.NotificationEventTypeId == 2);

                    if (preference != null && preference.EmailEnabled)
                    {
                        var user = await context.Users
                            .Include(u => u.EmailAddresses)
                            .FirstOrDefaultAsync(u => u.Id == responsibleUser);

                        if (user != null && user.EmailAddresses.Any(e => e.IsPrimary))
                        {
                            var email = user.EmailAddresses.First(e => e.IsPrimary).EmailAddress;

                            // Create a dummy payment object for the reminder
                            var payment = new PropertyPayment
                            {
                                Amount = tenant.RentAmount,
                                DueDate = new DateTime(today.Year, today.Month, tenant.DebitDayOfMonth)
                            };

                            await _emailService.SendRentDueReminderAsync(
                                tenant,
                                tenant.Property,
                                payment);

                            notification.EmailSent = true;
                            notification.EmailSentDate = DateTime.Now;
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private async Task ProcessMaintenanceDueNotifications(ApplicationDbContext context)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Find maintenance tickets scheduled for tomorrow
            var scheduledTickets = await context.MaintenanceTickets
                .Include(t => t.Property)
                .Include(t => t.Vendor)
                .Where(t =>
                    t.ScheduledDate.HasValue &&
                    t.ScheduledDate.Value.Date == tomorrow.Date &&
                    t.StatusId != 4) // Not completed
                .ToListAsync();

            foreach (var ticket in scheduledTickets)
            {
                // Notify assigned user
                if (!string.IsNullOrEmpty(ticket.AssignedToUserId))
                {
                    // Create notification
                    var notification = new Notification
                    {
                        NotificationEventTypeId = 3, // Maintenance scheduled notification type ID
                        Title = "Maintenance Scheduled Tomorrow",
                        Message = $"Maintenance ticket #{ticket.TicketNumber} - {ticket.Title} for property " +
                                 $"{ticket.Property.PropertyName} is scheduled for tomorrow {ticket.ScheduledDate:yyyy-MM-dd}.",
                        RecipientUserId = ticket.AssignedToUserId,
                        CreatedDate = DateTime.Now,
                        IsRead = false,
                        RelatedEntityType = "MaintenanceTicket",
                        RelatedEntityId = ticket.Id
                    };

                    await context.Notifications.AddAsync(notification);

                    // Send email if user has email notifications enabled
                    var preference = await context.NotificationPreferences
                        .FirstOrDefaultAsync(np =>
                            np.RelatedEntityType == "User" &&
                            np.RelatedEntityStringId == ticket.AssignedToUserId &&
                            np.NotificationEventTypeId == 3);

                    if (preference != null && preference.EmailEnabled)
                    {
                        var user = await context.Users
                            .Include(u => u.EmailAddresses)
                            .FirstOrDefaultAsync(u => u.Id == ticket.AssignedToUserId);

                        if (user != null && user.EmailAddresses.Any(e => e.IsPrimary))
                        {
                            var email = user.EmailAddresses.First(e => e.IsPrimary).EmailAddress;

                            await _emailService.SendMaintenanceTicketUpdatedAsync(
                                ticket,
                                "This is a reminder that this maintenance ticket is scheduled for tomorrow.");

                            notification.EmailSent = true;
                            notification.EmailSentDate = DateTime.Now;
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private async Task ProcessInspectionNotifications(ApplicationDbContext context)
        {
            var today = DateTime.Today;
            var threeDaysFromNow = today.AddDays(3);

            // Find inspections scheduled in 3 days
            var scheduledInspections = await context.PropertyInspections
                .Include(i => i.Property)
                    .ThenInclude(p => p.Tenants.Where(t => t.StatusId == 1))
                .Where(i =>
                    i.ScheduledDate.Date == threeDaysFromNow.Date &&
                    i.StatusId == 1) // Scheduled status
                .ToListAsync();

            foreach (var inspection in scheduledInspections)
            {
                // Notify inspector
                if (!string.IsNullOrEmpty(inspection.InspectorUserId))
                {
                    // Create notification
                    var notification = new Notification
                    {
                        NotificationEventTypeId = 4, // Inspection scheduled notification type ID
                        Title = "Inspection Scheduled in 3 Days",
                        Message = $"Inspection {inspection.InspectionCode} for property {inspection.Property.PropertyName} " +
                                 $"is scheduled for {inspection.ScheduledDate:yyyy-MM-dd}.",
                        RecipientUserId = inspection.InspectorUserId,
                        CreatedDate = DateTime.Now,
                        IsRead = false,
                        RelatedEntityType = "PropertyInspection",
                        RelatedEntityId = inspection.Id
                    };

                    await context.Notifications.AddAsync(notification);

                    // Send email if user has email notifications enabled
                    var preference = await context.NotificationPreferences
                        .FirstOrDefaultAsync(np =>
                            np.RelatedEntityType == "User" &&
                            np.RelatedEntityStringId == inspection.InspectorUserId &&
                            np.NotificationEventTypeId == 4);

                    if (preference != null && preference.EmailEnabled)
                    {
                        var user = await context.Users
                            .Include(u => u.EmailAddresses)
                            .FirstOrDefaultAsync(u => u.Id == inspection.InspectorUserId);

                        if (user != null && user.EmailAddresses.Any(e => e.IsPrimary))
                        {
                            var email = user.EmailAddresses.First(e => e.IsPrimary).EmailAddress;

                            // Get current tenant if any
                            var tenant = inspection.Property.Tenants.FirstOrDefault();

                            await _emailService.SendInspectionScheduledAsync(
                                inspection,
                                tenant);

                            notification.EmailSent = true;
                            notification.EmailSentDate = DateTime.Now;
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        #endregion Helper Methods
    }
}