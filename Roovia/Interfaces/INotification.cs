using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roovia.Interfaces
{
    public interface INotificationService
    {
        // Notification Operations
        Task<ResponseModel> CreateNotification(Notification notification);
        Task<ResponseModel> GetNotificationById(int id);
        Task<ResponseModel> MarkNotificationAsRead(int id, string userId);
        Task<ResponseModel> DeleteNotification(int id, string userId);

        // Notification Listings
        Task<ResponseModel> GetUserNotifications(string userId, bool includeRead = false, int page = 1, int pageSize = 20);
        Task<ResponseModel> GetUnreadNotificationCount(string userId);
        Task<ResponseModel> GetNotificationsByEntity(string entityType, int entityId, string userId = null);

        // Notification Preferences
        Task<ResponseModel> GetUserNotificationPreferences(string userId);
        Task<ResponseModel> UpdateNotificationPreference(int preferenceId, bool emailEnabled, bool smsEnabled, bool pushEnabled, bool webEnabled);
        Task<ResponseModel> CreateNotificationPreference(NotificationPreference preference);

        // Notification Templates
        Task<ResponseModel> GetNotificationTemplates(int companyId, string eventType = null);
        Task<ResponseModel> UpdateNotificationTemplate(int templateId, string subject, string bodyTemplate, string smsTemplate);

        // Send Notifications
        Task<ResponseModel> SendNotification(string eventType, string title, string message, string recipientUserId, string relatedEntityType = null, int? relatedEntityId = null);
        Task<ResponseModel> SendBulkNotifications(string eventType, string title, string message, List<string> recipientUserIds, string relatedEntityType = null, int? relatedEntityId = null);
        Task<ResponseModel> SendEntityNotification(string eventType, string title, string message, string relatedEntityType, int relatedEntityId);

        // Notification Channels
        Task<ResponseModel> SendEmailNotification(string email, string subject, string body);
        Task<ResponseModel> SendSmsNotification(string phoneNumber, string message);
        Task<ResponseModel> SendPushNotification(string userId, string title, string message);

        // Bulk Operations
        Task<ResponseModel> MarkAllNotificationsAsRead(string userId);
        Task<ResponseModel> DeleteAllNotifications(string userId, bool onlyRead = true);

        // Process Notifications
        Task<ResponseModel> ProcessScheduledNotifications();
        Task<ResponseModel> CleanupOldNotifications(int daysToKeep = 90);
    }
}