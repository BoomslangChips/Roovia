using Roovia.Models.BusinessHelperModels;

namespace Roovia.Interfaces
{
    public interface IReminderService
    {
        // CRUD Operations
        Task<ResponseModel> CreateReminder(Reminder reminder);

        Task<ResponseModel> GetReminderById(int id);

        Task<ResponseModel> UpdateReminder(int id, Reminder updatedReminder);

        Task<ResponseModel> DeleteReminder(int id, string userId);

        // Status Management
        Task<ResponseModel> CompleteReminder(int id, string userId);

        Task<ResponseModel> SnoozeReminder(int id, DateTime snoozeUntil, string userId);

        // Listing Methods
        Task<ResponseModel> GetRemindersByEntity(string entityType, object entityId);

        Task<ResponseModel> GetRemindersByStatus(int statusId, int companyId);

        Task<ResponseModel> GetRemindersByUser(string userId);

        Task<ResponseModel> GetActiveReminders(string userId = null, int companyId = 0);

        Task<ResponseModel> GetOverdueReminders(string userId = null, int companyId = 0);

        Task<ResponseModel> GetUpcomingReminders(int days = 7, string userId = null, int companyId = 0);

        // Recurring Reminders
        Task<ResponseModel> CreateRecurringReminder(Reminder reminder, int recurrenceFrequencyId, int recurrenceInterval, DateTime? endDate = null);

        Task<ResponseModel> UpdateRecurringReminder(int id, Reminder updatedReminder, bool updateAllInstances = false);

        Task<ResponseModel> ProcessRecurringReminders();

        // Notification Integration
        Task<ResponseModel> SendReminderNotifications();

        // Bulk Operations
        Task<ResponseModel> CreateBulkReminders(List<Reminder> reminders);

        Task<ResponseModel> DeleteRemindersByEntity(string entityType, object entityId, string userId);

        // Statistics
        Task<ResponseModel> GetReminderStatistics(int companyId);
    }
}