using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface IEmailService
    {
        // Core email sending
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);

        Task SendEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true);

        Task SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentPath, bool isHtml = true);

        // Registration & Account emails
        Task SendRegistrationNotificationAsync(Company company, ApplicationUser user);

        Task SendAccountApprovalNotificationAsync(ApplicationUser user);

        Task SendAccountRejectionNotificationAsync(ApplicationUser user, string reason);

        Task SendAccountCreationEmailAsync(ApplicationUser user, string passwordResetToken);

        Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink);

        Task SendPasswordResetNotificationAsync(ApplicationUser user, string newPassword);

        Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink);

        // Property Management emails
        Task SendPropertyCreatedNotificationAsync(Property property, PropertyOwner owner);

        Task SendTenantWelcomeEmailAsync(PropertyTenant tenant, Property property);

        Task SendLeaseExpiryReminderAsync(PropertyTenant tenant, Property property, int daysUntilExpiry);

        Task SendRentDueReminderAsync(PropertyTenant tenant, Property property, PropertyPayment payment);

        // Maintenance emails
        Task SendMaintenanceTicketCreatedAsync(MaintenanceTicket ticket, PropertyTenant tenant = null);

        Task SendMaintenanceTicketUpdatedAsync(MaintenanceTicket ticket, string updateMessage);

        Task SendMaintenanceCompletedAsync(MaintenanceTicket ticket, PropertyTenant tenant = null);

        Task SendVendorAssignmentAsync(MaintenanceTicket ticket, Vendor vendor);

        Task SendVendorWelcomeEmailAsync(Vendor vendor);

        Task SendVendorInsuranceUpdateAsync(Vendor vendor);

        // Payment emails
        Task SendPaymentReceivedAsync(PropertyPayment payment, PropertyTenant tenant);

        Task SendPaymentReminderAsync(PropertyTenant tenant, Property property, decimal amountDue);

        Task SendBeneficiaryPaymentNotificationAsync(BeneficiaryPayment payment, PropertyBeneficiary beneficiary);

        // Inspection emails
        Task SendInspectionScheduledAsync(PropertyInspection inspection, PropertyTenant tenant = null);

        Task SendInspectionCompletedAsync(PropertyInspection inspection, PropertyOwner owner);

        Task SendInspectionReportAsync(PropertyInspection inspection, List<string> recipients);

        // Bulk email operations
        Task SendBulkEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true);

        Task SendNewsletterAsync(List<Company> companies, string subject, string body);
    }
}