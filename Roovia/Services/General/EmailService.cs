using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using System.Net.Mime;
using Roovia.Models.UserCompanyModels;
using Microsoft.Extensions.Configuration;
using Roovia.Models.BusinessModels;
using Roovia.Interfaces;

namespace Roovia.Services.General
{
    public class EmailService : IEmailService
    {
        private readonly SmtpClient _smtpClient;
        private readonly ILogger<EmailService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;
        private readonly IConfiguration _configuration;

        private readonly string[] _adminEmails = new[] { "jean@roovia.co.za", "chris@roovia.co.za" };
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(
            ILogger<EmailService> logger,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _configuration = configuration;
            _contentTypeProvider = new FileExtensionContentTypeProvider();

            // Configure SMTP from configuration
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.office365.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["Email:Username"] ?? "info@roovia.co.za";
            var smtpPassword = _configuration["Email:Password"] ?? "fqlwdjlbklwcncrc";

            _fromEmail = smtpUsername;
            _fromName = _configuration["Email:FromName"] ?? "Roovia Estate Management";

            _smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };
        }

        // Core email sending methods
        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(new MailAddress(to));

                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {to} with subject: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {to} with subject: {subject}");
                throw;
            }
        }

        public async Task SendEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                foreach (var recipient in recipients)
                {
                    mailMessage.To.Add(new MailAddress(recipient));
                }

                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {recipients.Count} recipients with subject: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to multiple recipients with subject: {subject}");
                throw;
            }
        }

        public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentPath, bool isHtml = true)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(new MailAddress(to));

                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    var attachment = new Attachment(attachmentPath);
                    mailMessage.Attachments.Add(attachment);
                }

                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email with attachment sent successfully to {to} with subject: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email with attachment to {to} with subject: {subject}");
                throw;
            }
        }

        // Property Management emails
        public async Task SendPropertyCreatedNotificationAsync(Property property, PropertyOwner owner)
        {
            try
            {
                var subject = $"New Property Added: {property.PropertyName}";
                var body = GetEmailTemplate("PropertyCreated", new
                {
                    OwnerName = owner.DisplayName,
                    PropertyName = property.PropertyName,
                    PropertyCode = property.PropertyCode,
                    Address = $"{property.Address.Street}, {property.Address.City}, {property.Address.PostalCode}",
                    RentalAmount = $"R {property.RentalAmount:N2}",
                    Status = property.Status?.Name
                });

                var recipientEmail = owner.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress ?? owner.EmailAddresses.FirstOrDefault()?.EmailAddress;
                if (!string.IsNullOrEmpty(recipientEmail))
                {
                    await SendEmailAsync(recipientEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending property created notification for property {property.Id}");
            }
        }

        public async Task SendTenantWelcomeEmailAsync(PropertyTenant tenant, Property property)
        {
            try
            {
                var subject = $"Welcome to {property.PropertyName}";
                var body = GetEmailTemplate("TenantWelcome", new
                {
                    TenantName = tenant.DisplayName,
                    PropertyName = property.PropertyName,
                    Address = $"{property.Address.Street}, {property.Address.City}, {property.Address.PostalCode}",
                    LeaseStartDate = tenant.LeaseStartDate.ToString("d"),
                    LeaseEndDate = tenant.LeaseEndDate.ToString("d"),
                    RentAmount = $"R {tenant.RentAmount:N2}",
                    DebitDay = tenant.DebitDayOfMonth
                });

                var recipientEmail = tenant.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress ?? tenant.EmailAddresses.FirstOrDefault()?.EmailAddress;
                if (!string.IsNullOrEmpty(recipientEmail))
                {
                    await SendEmailAsync(recipientEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending tenant welcome email for tenant {tenant.Id}");
            }
        }

        public async Task SendLeaseExpiryReminderAsync(PropertyTenant tenant, Property property, int daysUntilExpiry)
        {
            try
            {
                var subject = $"Lease Expiry Reminder - {property.PropertyName}";
                var body = GetEmailTemplate("LeaseExpiryReminder", new
                {
                    TenantName = tenant.DisplayName,
                    PropertyName = property.PropertyName,
                    DaysUntilExpiry = daysUntilExpiry,
                    LeaseEndDate = tenant.LeaseEndDate.ToString("d"),
                    ContactEmail = property.Company?.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress
                });

                var recipientEmail = tenant.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(recipientEmail))
                {
                    await SendEmailAsync(recipientEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending lease expiry reminder for tenant {tenant.Id}");
            }
        }

        public async Task SendRentDueReminderAsync(PropertyTenant tenant, Property property, PropertyPayment payment)
        {
            try
            {
                var subject = $"Rent Payment Due - {property.PropertyName}";
                var body = GetEmailTemplate("RentDueReminder", new
                {
                    TenantName = tenant.DisplayName,
                    PropertyName = property.PropertyName,
                    Amount = $"R {payment.Amount:N2}",
                    DueDate = payment.DueDate.ToString("d"),
                    PaymentReference = payment.PaymentReference,
                });

                var recipientEmail = tenant.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(recipientEmail))
                {
                    await SendEmailAsync(recipientEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending rent due reminder for tenant {tenant.Id}");
            }
        }

        // Maintenance emails
        public async Task SendMaintenanceTicketCreatedAsync(MaintenanceTicket ticket, PropertyTenant tenant = null)
        {
            try
            {
                var subject = $"Maintenance Ticket Created: {ticket.TicketNumber}";
                var body = GetEmailTemplate("MaintenanceTicketCreated", new
                {
                    TicketNumber = ticket.TicketNumber,
                    Title = ticket.Title,
                    Description = ticket.Description,
                    Priority = ticket.Priority?.Name,
                    Category = ticket.Category?.Name,
                    PropertyName = ticket.Property?.PropertyName,
                    ScheduledDate = ticket.ScheduledDate?.ToString("d"),
                    EstimatedDuration = ticket.EstimatedDuration?.ToString()
                });

                // Send to tenant if provided
                if (tenant != null)
                {
                    var tenantEmail = tenant.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                    if (!string.IsNullOrEmpty(tenantEmail))
                    {
                        await SendEmailAsync(tenantEmail, subject, body);
                    }
                }

                // Send to property owner
                var ownerEmail = ticket.Property?.Owner?.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(ownerEmail))
                {
                    await SendEmailAsync(ownerEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending maintenance ticket created notification for ticket {ticket.Id}");
            }
        }

        public async Task SendMaintenanceCompletedAsync(MaintenanceTicket ticket, PropertyTenant tenant = null)
        {
            try
            {
                var subject = $"Maintenance Completed: {ticket.TicketNumber}";
                var body = GetEmailTemplate("MaintenanceCompleted", new
                {
                    TicketNumber = ticket.TicketNumber,
                    Title = ticket.Title,
                    PropertyName = ticket.Property?.PropertyName,
                    CompletedDate = ticket.CompletedDate?.ToString("d"),
                    IssueResolved = ticket.IssueResolved.HasValue ? true : false,
                    CompletionNotes = ticket.CompletionNotes,
                    ActualCost = ticket.ActualCost.HasValue ? $"R {ticket.ActualCost:N2}" : "N/A"
                });

                // Send to tenant if provided
                if (tenant != null)
                {
                    var tenantEmail = tenant.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                    if (!string.IsNullOrEmpty(tenantEmail))
                    {
                        await SendEmailAsync(tenantEmail, subject, body);
                    }
                }

                // Send to property owner
                var ownerEmail = ticket.Property?.Owner?.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(ownerEmail))
                {
                    await SendEmailAsync(ownerEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending maintenance completed notification for ticket {ticket.Id}");
            }
        }

        public async Task SendVendorAssignmentAsync(MaintenanceTicket ticket, Vendor vendor)
        {
            try
            {
                var subject = $"New Maintenance Assignment: {ticket.TicketNumber}";
                var body = GetEmailTemplate("VendorAssignment", new
                {
                    VendorName = vendor.Name,
                    TicketNumber = ticket.TicketNumber,
                    Title = ticket.Title,
                    Description = ticket.Description,
                    PropertyName = ticket.Property?.PropertyName,
                    PropertyAddress = ticket.Property != null ?
                        $"{ticket.Property.Address.Street}, {ticket.Property.Address.City}" : "N/A",
                    Priority = ticket.Priority?.Name,
                    ScheduledDate = ticket.ScheduledDate?.ToString("d"),
                    ContactPerson = ticket.Property?.Company?.ContactNumbers.FirstOrDefault(c => c.IsPrimary)?.Number,
                    AccessInstructions = ticket.AccessInstructions
                });

                var vendorEmail = vendor.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(vendorEmail))
                {
                    await SendEmailAsync(vendorEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending vendor assignment notification for ticket {ticket.Id}");
            }
        }

        public async Task SendMaintenanceTicketUpdatedAsync(MaintenanceTicket ticket, string updateMessage)
        {
            try
            {
                var subject = $"Maintenance Ticket Updated: {ticket.TicketNumber}";
                var body = GetEmailTemplate("MaintenanceTicketUpdated", new
                {
                    TicketNumber = ticket.TicketNumber,
                    Title = ticket.Title,
                    UpdateMessage = updateMessage,
                    Status = ticket.Status?.Name,
                    UpdatedDate = ticket.UpdatedDate?.ToString("g")
                });

                var recipients = new List<string>();

                // Add tenant email if applicable
                if (ticket.Tenant != null)
                {
                    var tenantEmail = ticket.Tenant.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                    if (!string.IsNullOrEmpty(tenantEmail))
                        recipients.Add(tenantEmail);
                }

                // Add owner email
                var ownerEmail = ticket.Property?.Owner?.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(ownerEmail))
                    recipients.Add(ownerEmail);

                // Add vendor email if assigned
                if (ticket.Vendor != null)
                {
                    var vendorEmail = ticket.Vendor.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                    if (!string.IsNullOrEmpty(vendorEmail))
                        recipients.Add(vendorEmail);
                }

                if (recipients.Any())
                {
                    await SendEmailAsync(recipients, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending maintenance ticket updated notification for ticket {ticket.Id}");
            }
        }

        // Payment emails
        public async Task SendPaymentReceivedAsync(PropertyPayment payment, PropertyTenant tenant)
        {
            try
            {
                var subject = $"Payment Received - {payment.Property?.PropertyName}";
                var body = GetEmailTemplate("PaymentReceived", new
                {
                    TenantName = tenant.DisplayName,
                    PropertyName = payment.Property?.PropertyName,
                    PaymentReference = payment.PaymentReference,
                    Amount = $"R {payment.Amount:N2}",
                    PaymentDate = payment.PaymentDate?.ToString("d"),
                    Balance = $"R {tenant.Balance:N2}",
                    PaymentMethod = payment.PaymentMethod?.Name
                });

                var tenantEmail = tenant.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(tenantEmail))
                {
                    await SendEmailAsync(tenantEmail, subject, body);
                }

                // Also notify property owner
                var ownerEmail = payment.Property?.Owner?.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(ownerEmail))
                {
                    var ownerBody = GetEmailTemplate("PaymentReceivedOwner", new
                    {
                        OwnerName = payment.Property.Owner.DisplayName,
                        PropertyName = payment.Property.PropertyName,
                        TenantName = tenant.DisplayName,
                        PaymentReference = payment.PaymentReference,
                        Amount = $"R {payment.Amount:N2}",
                        PaymentDate = payment.PaymentDate?.ToString("d")
                    });

                    await SendEmailAsync(ownerEmail, subject, ownerBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending payment received notification for payment {payment.Id}");
            }
        }

        public async Task SendPaymentReminderAsync(PropertyTenant tenant, Property property, decimal amountDue)
        {
            try
            {
                var subject = $"Payment Reminder - {property.PropertyName}";
                var body = GetEmailTemplate("PaymentReminder", new
                {
                    TenantName = tenant.DisplayName,
                    PropertyName = property.PropertyName,
                    AmountDue = $"R {amountDue:N2}",
                    DebitDay = tenant.DebitDayOfMonth,
                    BankDetails = property.Owner?.BankAccount != null ?
                        $"{property.Owner.BankAccount.AccountType} - {property.Owner.BankAccount.AccountNumber}" : "Contact office for bank details"
                });

                var tenantEmail = tenant.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(tenantEmail))
                {
                    await SendEmailAsync(tenantEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending payment reminder for tenant {tenant.Id}");
            }
        }

        public async Task SendBeneficiaryPaymentNotificationAsync(BeneficiaryPayment payment, PropertyBeneficiary beneficiary)
        {
            try
            {
                var subject = $"Payment Processed - {payment.PaymentReference}";
                var body = GetEmailTemplate("BeneficiaryPayment", new
                {
                    BeneficiaryName = beneficiary.Name,
                    PaymentReference = payment.PaymentReference,
                    Amount = $"R {payment.Amount:N2}",
                    PaymentDate = payment.PaymentDate?.ToString("d"),
                    PropertyName = beneficiary.Property?.PropertyName,
                    TransactionReference = payment.TransactionReference,
                    BankAccount = beneficiary.BankAccount != null ?
                        $"{beneficiary.BankAccount.AccountType} - {beneficiary.BankAccount.AccountNumber}" : "Not specified"
                });

                var beneficiaryEmail = beneficiary.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(beneficiaryEmail))
                {
                    await SendEmailAsync(beneficiaryEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending beneficiary payment notification for payment {payment.Id}");
            }
        }

        // Inspection emails
        public async Task SendInspectionScheduledAsync(PropertyInspection inspection, PropertyTenant tenant = null)
        {
            try
            {
                var subject = $"Inspection Scheduled - {inspection.Property?.PropertyName}";
                var body = GetEmailTemplate("InspectionScheduled", new
                {
                    PropertyName = inspection.Property?.PropertyName,
                    InspectionType = inspection.InspectionType?.Name,
                    ScheduledDate = inspection.ScheduledDate.ToString("d"),
                    InspectorName = inspection.InspectorName,
                    TenantRequired = inspection.TenantPresent.HasValue && inspection.TenantPresent.Value ? "Your presence is required" : "Your presence is optional"
                });

                var recipients = new List<string>();

                // Send to tenant if provided
                if (tenant != null)
                {
                    var tenantEmail = tenant.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                    if (!string.IsNullOrEmpty(tenantEmail))
                        recipients.Add(tenantEmail);
                }

                // Send to property owner
                var ownerEmail = inspection.Property?.Owner?.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(ownerEmail))
                    recipients.Add(ownerEmail);

                if (recipients.Any())
                {
                    await SendEmailAsync(recipients, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending inspection scheduled notification for inspection {inspection.Id}");
            }
        }

        public async Task SendInspectionCompletedAsync(PropertyInspection inspection, PropertyOwner owner)
        {
            try
            {
                var subject = $"Inspection Completed - {inspection.Property?.PropertyName}";
                var body = GetEmailTemplate("InspectionCompleted", new
                {
                    OwnerName = owner.DisplayName,
                    PropertyName = inspection.Property?.PropertyName,
                    InspectionType = inspection.InspectionType?.Name,
                    InspectionDate = inspection.ActualDate?.ToString("d") ?? inspection.ScheduledDate.ToString("d"),
                    InspectorName = inspection.InspectorName,
                    OverallRating = inspection.OverallRating.HasValue ? $"{inspection.OverallRating}/5" : "N/A",
                    OverallCondition = inspection.OverallCondition?.Name,
                    GeneralNotes = inspection.GeneralNotes,
                    ItemsRequiringMaintenance = inspection.InspectionItems?.Count(i => i.RequiresMaintenance) ?? 0,
                    NextInspectionDue = inspection.NextInspectionDue?.ToString("d") ?? "To be scheduled"
                });

                var ownerEmail = owner.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(ownerEmail))
                {
                    await SendEmailAsync(ownerEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending inspection completed notification for inspection {inspection.Id}");
            }
        }

        public async Task SendInspectionReportAsync(PropertyInspection inspection, List<string> recipients)
        {
            try
            {
                var subject = $"Inspection Report - {inspection.Property?.PropertyName} - {inspection.InspectionCode}";
                var body = GetEmailTemplate("InspectionReport", new
                {
                    PropertyName = inspection.Property?.PropertyName,
                    PropertyAddress = inspection.Property != null ?
                        $"{inspection.Property.Address.Street}, {inspection.Property.Address.City}" : "N/A",
                    InspectionCode = inspection.InspectionCode,
                    InspectionType = inspection.InspectionType?.Name,
                    InspectionDate = inspection.ActualDate?.ToString("d") ?? inspection.ScheduledDate.ToString("d"),
                    InspectorName = inspection.InspectorName,
                    OverallRating = inspection.OverallRating.HasValue ? $"{inspection.OverallRating}/5" : "N/A",
                    OverallCondition = inspection.OverallCondition?.Name,
                    ReportUrl = inspection.ReportDocument?.Url
                });

                await SendEmailAsync(recipients, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending inspection report for inspection {inspection.Id}");
            }
        }

        // Bulk email operations
        public async Task SendBulkEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true)
        {
            try
            {
                const int batchSize = 50; // Send in batches to avoid SMTP limits
                var batches = recipients.Select((recipient, index) => new { recipient, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.recipient).ToList())
                    .ToList();

                foreach (var batch in batches)
                {
                    await SendEmailAsync(batch, subject, body, isHtml);
                    await Task.Delay(1000); // Delay between batches to avoid rate limiting
                }

                _logger.LogInformation($"Bulk email sent to {recipients.Count} recipients in {batches.Count} batches");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending bulk email to {recipients.Count} recipients");
                throw;
            }
        }

        public async Task SendNewsletterAsync(List<Company> companies, string subject, string body)
        {
            try
            {
                var recipients = companies
                    .Where(c => c.EmailAddresses.Any(e => e.IsPrimary && e.IsActive))
                    .Select(c => c.EmailAddresses.First(e => e.IsPrimary && e.IsActive).EmailAddress)
                    .Where(email => !string.IsNullOrEmpty(email))
                    .ToList();

                if (recipients.Any())
                {
                    await SendBulkEmailAsync(recipients, subject, GetNewsletterTemplate(body), true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending newsletter");
                throw;
            }
        }

        // Adding missing methods referenced by other services
        public async Task SendPasswordResetNotificationAsync(ApplicationUser user, string newPassword)
        {
            try
            {
                var subject = "Your Roovia Password Has Been Reset";
                var body = @$"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style type='text/css'>
        body {{ margin: 0; padding: 20px; font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #3B5BA9; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; margin: -40px -40px 20px; }}
        .password-container {{ background-color: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 20px 0; }}
        .btn {{ display: inline-block; background-color: #3B5BA9; color: white; text-decoration: none; padding: 10px 20px; border-radius: 4px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset</h1>
        </div>
        <h2>Your Password Has Been Reset</h2>
        <p>Dear {user.FirstName} {user.LastName},</p>
        <p>As requested, your password for Roovia Estate Management has been reset.</p>
        <div class='password-container'>
            <p><strong>Your temporary password is:</strong> {newPassword}</p>
            <p>Please use this password to log in. You will be prompted to change your password upon first login.</p>
        </div>
        <p>For security reasons, please change this password immediately after logging in.</p>
        <a href='https://roovia.co.za/login' class='btn'>Login to Roovia</a>
        <p style='margin-top: 30px; font-size: 12px; color: #666;'>If you did not request this password reset, please contact support immediately.</p>
    </div>
</body>
</html>";

                var userEmail = user.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress ?? user.Email;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    await SendEmailAsync(userEmail, subject, body);
                    _logger.LogInformation($"Password reset notification sent to {userEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset notification to user {user.Id}");
                throw;
            }
        }

        public async Task SendVendorWelcomeEmailAsync(Vendor vendor)
        {
            try
            {
                var subject = "Welcome to Roovia - Vendor Registration";
                var body = @$"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style type='text/css'>
        body {{ margin: 0; padding: 20px; font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #3B5BA9; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; margin: -40px -40px 20px; }}
        .details {{ background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .btn {{ display: inline-block; background-color: #3B5BA9; color: white; text-decoration: none; padding: 10px 20px; border-radius: 4px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to Roovia</h1>
        </div>
        <h2>Vendor Registration Confirmation</h2>
        <p>Dear {vendor.Name},</p>
        <p>Welcome to Roovia! You have been registered as a vendor in our estate management system.</p>
        <div class='details'>
            <h3>Your Vendor Information</h3>
            <p><strong>Name:</strong> {vendor.Name}</p>
            <p><strong>Contact Person:</strong> {vendor.ContactPerson}</p>
            <p><strong>Specializations:</strong> {vendor.Specializations}</p>
        </div>
        <p>You will receive notifications about maintenance requests and assignments through this email address.</p>
        <p>If you have any questions, please contact the property management office.</p>
    </div>
</body>
</html>";

                var vendorEmail = vendor.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(vendorEmail))
                {
                    await SendEmailAsync(vendorEmail, subject, body);
                    _logger.LogInformation($"Vendor welcome email sent to {vendorEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending welcome email to vendor {vendor.Id}");
                throw;
            }
        }

        public async Task SendVendorInsuranceUpdateAsync(Vendor vendor)
        {
            try
            {
                bool isExpired = vendor.InsuranceExpiryDate.HasValue && vendor.InsuranceExpiryDate.Value < DateTime.Now;
                bool isExpiringSoon = vendor.InsuranceExpiryDate.HasValue &&
                                     vendor.InsuranceExpiryDate.Value > DateTime.Now &&
                                     vendor.InsuranceExpiryDate.Value < DateTime.Now.AddDays(30);

                string subject;
                string statusMessage;

                if (isExpired)
                {
                    subject = "IMPORTANT: Your Insurance Policy Has Expired";
                    statusMessage = "Your insurance policy has <strong>expired</strong>. This may affect your ability to receive new assignments.";
                }
                else if (isExpiringSoon)
                {
                    subject = "REMINDER: Insurance Policy Expiring Soon";
                    statusMessage = $"Your insurance policy will expire on {vendor.InsuranceExpiryDate?.ToString("d")}. Please renew it to continue receiving assignments.";
                }
                else
                {
                    subject = "Insurance Information Updated";
                    statusMessage = $"Your insurance policy has been updated and is valid until {vendor.InsuranceExpiryDate?.ToString("d")}.";
                }

                var body = @$"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style type='text/css'>
        body {{ margin: 0; padding: 20px; font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #3B5BA9; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; margin: -40px -40px 20px; }}
        .alert {{ background-color: {(isExpired ? "#ffebee" : (isExpiringSoon ? "#fff3cd" : "#e8f5e9"))}; 
                 padding: 15px; border-radius: 8px; 
                 border-left: 4px solid {(isExpired ? "#f44336" : (isExpiringSoon ? "#ffc107" : "#4caf50"))}; margin: 20px 0; }}
        .details {{ background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Insurance Update</h1>
        </div>
        <h2>Insurance Policy Status</h2>
        <p>Dear {vendor.Name},</p>
        <div class='alert'>
            <p>{statusMessage}</p>
        </div>
        <div class='details'>
            <h3>Policy Details</h3>
            <p><strong>Policy Number:</strong> {vendor.InsurancePolicyNumber}</p>
            <p><strong>Expiry Date:</strong> {vendor.InsuranceExpiryDate?.ToString("d") ?? "Not specified"}</p>
        </div>
        <p>Please ensure your insurance remains current to maintain your active status with Roovia.</p>
    </div>
</body>
</html>";

                var vendorEmail = vendor.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress;
                if (!string.IsNullOrEmpty(vendorEmail))
                {
                    await SendEmailAsync(vendorEmail, subject, body);
                    _logger.LogInformation($"Insurance update notification sent to vendor {vendor.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending insurance update notification to vendor {vendor.Id}");
                throw;
            }
        }

        // Registration and account emails
        public async Task SendRegistrationNotificationAsync(Company company, ApplicationUser user)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("info@roovia.co.za", "Roovia Estate Management"),
                    Subject = "New Estate Agency Registration Pending Activation",
                    IsBodyHtml = true
                };

                // Add admin email addresses
                foreach (var adminEmail in _adminEmails)
                {
                    mailMessage.To.Add(new MailAddress(adminEmail));
                }

                var body = new System.Text.StringBuilder();
                body.AppendLine(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <style type='text/css'>
        /* Reset styles */
        body, #bodyTable { margin: 0; padding: 0; width: 100% !important; }
        img { border: 0; height: auto; line-height: 100%; outline: none; text-decoration: none; }
        table { border-collapse: collapse !important; }
        body { height: 100% !important; margin: 0; padding: 0; width: 100% !important; }
        /* Outlook-specific styles */
        .ReadMsgBody { width: 100%; }
        .ExternalClass { width: 100%; }
        .ExternalClass * { line-height: 100%; }
        /* Mobile-specific styles */
        @media screen and (max-width: 525px) {
            .responsive-table {
                width: 100% !important;
            }
            .mobile-padding {
                padding: 15px 5% !important;
            }
            .mobile-button {
                width: 100% !important;
                padding: 15px 0 !important;
            }
        }
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F5F5F5;'>
    <center>
        <table border='0' cellpadding='0' cellspacing='0' width='100%' id='bodyTable' style='height: 100%; background-color: #F5F5F5;'>
            <tr>
                <td align='center' valign='top'>
                    <table border='0' cellpadding='0' cellspacing='0' width='600' class='responsive-table'>
                        <!-- Main Content -->
                        <tr>
                            <td bgcolor='#ffffff' style='padding: 40px 30px; border-radius: 4px 4px 0 0;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <!-- Header -->
                                    <tr>
                                        <td style='padding: 0 0 30px 0; text-align: center;'>
                                            <h1 style='color: #3B5BA9; margin: 0; font-family: Arial, sans-serif; font-size: 28px; font-weight: bold;'>New Registration Pending</h1>
                                            <p style='color: #666666; margin: 15px 0 0 0; font-family: Arial, sans-serif; font-size: 16px;'>A new estate agency has registered and requires activation</p>
                                        </td>
                                    </tr>
                                    
                                    <!-- Company Information -->
                                    <tr>
                                        <td style='background-color: #f8f9fa; border-radius: 8px; padding: 25px; margin-bottom: 30px;'>
                                            <h2 style='color: #3B5BA9; margin: 0 0 20px 0; font-family: Arial, sans-serif; font-size: 20px;'>Company Information</h2>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                <tr>
                                                    <td width='140' style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Company Name:</strong></td>
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + company.Name + @"</td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Registration No:</strong></td>
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + company.RegistrationNumber + @"</td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Email:</strong></td>
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + company.EmailAddresses.FirstOrDefault()?.EmailAddress + @"</td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Phone:</strong></td>
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + company.ContactNumbers.FirstOrDefault()?.Number + @"</td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Website:</strong></td>
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + company.Website + @"</td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Address:</strong></td>
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + company.Address.Street + @", " + company.Address.City + @", " + company.Address.PostalCode + @"</td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Admin Information -->
                                    <tr>
                                        <td style='padding: 30px 0;'>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #f8f9fa; border-radius: 8px;'>
                                                <tr>
                                                    <td style='padding: 25px;'>
                                                        <h2 style='color: #3B5BA9; margin: 0 0 20px 0; font-family: Arial, sans-serif; font-size: 20px;'>Company Admin Information</h2>
                                                        <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                            <tr>
                                                                <td width='140' style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Name:</strong></td>
                                                                <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + $"{user.FirstName} {user.LastName}" + @"</td>
                                                            </tr>
                                                            <tr>
                                                                <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Email:</strong></td>
                                                                <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + user.Email + @"</td>
                                                            </tr>
                                                            <tr>
                                                                <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Phone:</strong></td>
                                                                <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + user.PhoneNumber + @"</td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Action Required Notice -->
                                    <tr>
                                        <td style='padding-bottom: 30px;'>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 4px;'>
                                                <tr>
                                                    <td style='padding: 20px;'>
                                                        <p style='color: #856404; margin: 0; font-family: Arial, sans-serif; font-weight: bold;'>Action Required</p>
                                                        <p style='color: #856404; margin: 10px 0 0 0; font-family: Arial, sans-serif;'>This registration requires activation by an Administrator</p>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Action Button -->
                                    <tr>
                                        <td>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                <tr>
                                                    <td align='center'>
                                                        <table border='0' cellpadding='0' cellspacing='0'>
                                                            <tr>
                                                                <td align='center' bgcolor='#3B5BA9' class='mobile-button' style='border-radius: 8px;'>
                                                                    <a href='https://roovia.co.za/admin/pending-companies' target='_blank' 
                                                                       style='font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; 
                                                                              text-decoration: none; padding: 15px 30px; display: inline-block; 
                                                                              font-weight: bold;'>Manage Pending Registrations</a>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        
                        <!-- Footer -->
                        <tr>
                            <td bgcolor='#f8f9fa' style='padding: 30px; border-radius: 0 0 4px 4px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <tr>
                                        <td style='text-align: center; padding: 0 0 20px 0;'>
                                            <p style='color: #666666; font-family: Arial, sans-serif; font-size: 14px; margin: 0;'>© 2025 Roovia Estate Management. All rights reserved.</p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: center; padding: 20px 0 0 0; border-top: 1px solid #dddddd;'>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Privacy Policy</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Terms of Service</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Contact Us</a>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>");

                mailMessage.Body = body.ToString();
                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Registration notification email sent successfully for company {company.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending registration notification email for company {company.Name}");
                throw;
            }
        }

        public async Task SendAccountApprovalNotificationAsync(ApplicationUser user)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("info@roovia.co.za", "Roovia Estate Management"),
                    Subject = "Your Roovia Account Has Been Approved",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(user.Email));

                var body = new System.Text.StringBuilder();
                body.AppendLine(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <style type='text/css'>
        /* Reset styles */
        body, #bodyTable { margin: 0; padding: 0; width: 100% !important; }
        img { border: 0; height: auto; line-height: 100%; outline: none; text-decoration: none; }
        table { border-collapse: collapse !important; }
        body { height: 100% !important; margin: 0; padding: 0; width: 100% !important; }
        /* Outlook-specific styles */
        .ReadMsgBody { width: 100%; }
        .ExternalClass { width: 100%; }
        .ExternalClass * { line-height: 100%; }
        /* Mobile-specific styles */
        @media screen and (max-width: 525px) {
            .responsive-table {
                width: 100% !important;
            }
            .mobile-padding {
                padding: 15px 5% !important;
            }
            .mobile-button {
                width: 100% !important;
                padding: 15px 0 !important;
            }
        }
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F5F5F5;'>
    <center>
        <table border='0' cellpadding='0' cellspacing='0' width='100%' id='bodyTable' style='height: 100%; background-color: #F5F5F5;'>
            <tr>
                <td align='center' valign='top'>
                    <table border='0' cellpadding='0' cellspacing='0' width='600' class='responsive-table'>
                        <!-- Main Content -->
                        <tr>
                            <td bgcolor='#ffffff' style='padding: 40px 30px; border-radius: 4px 4px 0 0;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <!-- Header -->
                                    <tr>
                                        <td style='padding: 0 0 30px 0; text-align: center;'>
                                            <h1 style='color: #3B5BA9; margin: 0; font-family: Arial, sans-serif; font-size: 28px; font-weight: bold;'>Account Approved!</h1>
                                            <p style='color: #666666; margin: 15px 0 0 0; font-family: Arial, sans-serif; font-size: 16px;'>Your Roovia Estate Management account has been approved</p>
                                        </td>
                                    </tr>
                                    
                                    <!-- Approval Message -->
                                    <tr>
                                        <td style='background-color: #e8f6ee; border-radius: 8px; padding: 25px; margin-bottom: 30px; border-left: 4px solid #28a745;'>
                                            <h2 style='color: #155724; margin: 0 0 20px 0; font-family: Arial, sans-serif; font-size: 20px;'>Welcome to Roovia!</h2>
                                            <p style='color: #155724; font-family: Arial, sans-serif; margin-bottom: 10px;'>Dear " + $"{user.FirstName} {user.LastName}" + @",</p>
                                            <p style='color: #155724; font-family: Arial, sans-serif; margin-bottom: 10px;'>We're pleased to inform you that your account registration for Roovia Estate Management has been approved. You can now access all features of our platform.</p>
                                            <p style='color: #155724; font-family: Arial, sans-serif; margin-bottom: 0;'>Get started by logging in to your account and setting up your profile and estate agency details.</p>
                                        </td>
                                    </tr>

                                    <!-- Account Details -->
                                    <tr>
                                        <td style='padding: 30px 0;'>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #f8f9fa; border-radius: 8px;'>
                                                <tr>
                                                    <td style='padding: 25px;'>
                                                        <h2 style='color: #3B5BA9; margin: 0 0 20px 0; font-family: Arial, sans-serif; font-size: 20px;'>Your Account Details</h2>
                                                        <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                            <tr>
                                                                <td width='140' style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Name:</strong></td>
                                                                <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + $"{user.FirstName} {user.LastName}" + @"</td>
                                                            </tr>
                                                            <tr>
                                                                <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Email:</strong></td>
                                                                <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + user.Email + @"</td>
                                                            </tr>
                                                            <tr>
                                                                <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Company:</strong></td>
                                                                <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + user.Company?.Name + @"</td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Login Button -->
                                    <tr>
                                        <td>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                <tr>
                                                    <td align='center'>
                                                        <table border='0' cellpadding='0' cellspacing='0'>
                                                            <tr>
                                                                <td align='center' bgcolor='#3B5BA9' class='mobile-button' style='border-radius: 8px;'>
                                                                    <a href='https://roovia.co.za/Account/Login' target='_blank' 
                                                                       style='font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; 
                                                                              text-decoration: none; padding: 15px 30px; display: inline-block; 
                                                                              font-weight: bold;'>Login to Roovia</a>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        
                        <!-- Footer -->
                        <tr>
                            <td bgcolor='#f8f9fa' style='padding: 30px; border-radius: 0 0 4px 4px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <tr>
                                        <td style='text-align: center; padding: 0 0 20px 0;'>
                                            <p style='color: #666666; font-family: Arial, sans-serif; font-size: 14px; margin: 0;'>© 2025 Roovia Estate Management. All rights reserved.</p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: center; padding: 20px 0 0 0; border-top: 1px solid #dddddd;'>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Privacy Policy</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Terms of Service</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Contact Us</a>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>");

                mailMessage.Body = body.ToString();
                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Account approval notification email sent successfully to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending account approval notification to {user.Email}");
                throw;
            }
        }

        public async Task SendAccountRejectionNotificationAsync(ApplicationUser user, string reason)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("info@roovia.co.za", "Roovia Estate Management"),
                    Subject = "Your Roovia Account Application Status",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(user.Email));

                var body = new System.Text.StringBuilder();
                body.AppendLine(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <style type='text/css'>
        /* Reset styles */
        body, #bodyTable { margin: 0; padding: 0; width: 100% !important; }
        img { border: 0; height: auto; line-height: 100%; outline: none; text-decoration: none; }
        table { border-collapse: collapse !important; }
        body { height: 100% !important; margin: 0; padding: 0; width: 100% !important; }
        /* Outlook-specific styles */
        .ReadMsgBody { width: 100%; }
        .ExternalClass { width: 100%; }
        .ExternalClass * { line-height: 100%; }
        /* Mobile-specific styles */
        @media screen and (max-width: 525px) {
            .responsive-table {
                width: 100% !important;
            }
            .mobile-padding {
                padding: 15px 5% !important;
            }
            .mobile-button {
                width: 100% !important;
                padding: 15px 0 !important;
            }
        }
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F5F5F5;'>
    <center>
        <table border='0' cellpadding='0' cellspacing='0' width='100%' id='bodyTable' style='height: 100%; background-color: #F5F5F5;'>
            <tr>
                <td align='center' valign='top'>
                    <table border='0' cellpadding='0' cellspacing='0' width='600' class='responsive-table'>
                        <!-- Main Content -->
                        <tr>
                            <td bgcolor='#ffffff' style='padding: 40px 30px; border-radius: 4px 4px 0 0;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <!-- Header -->
                                    <tr>
                                        <td style='padding: 0 0 30px 0; text-align: center;'>
                                            <h1 style='color: #3B5BA9; margin: 0; font-family: Arial, sans-serif; font-size: 28px; font-weight: bold;'>Account Application Update</h1>
                                            <p style='color: #666666; margin: 15px 0 0 0; font-family: Arial, sans-serif; font-size: 16px;'>Information regarding your Roovia account application</p>
                                        </td>
                                    </tr>
                                    
                                    <!-- Status Message -->
                                    <tr>
                                        <td style='background-color: #FFF4F4; border-radius: 8px; padding: 25px; margin-bottom: 30px; border-left: 4px solid #d9534f;'>
                                            <h2 style='color: #721c24; margin: 0 0 20px 0; font-family: Arial, sans-serif; font-size: 20px;'>Application Status: Needs Additional Information</h2>
                                            <p style='color: #721c24; font-family: Arial, sans-serif; margin-bottom: 10px;'>Dear " + $"{user.FirstName} {user.LastName}" + @",</p>
                                            <p style='color: #721c24; font-family: Arial, sans-serif; margin-bottom: 10px;'>Thank you for your interest in Roovia Estate Management. We've reviewed your application and need additional information before we can proceed with activation.</p>
                                            <p style='color: #721c24; font-family: Arial, sans-serif; margin-bottom: 10px;'><strong>Reason for additional information:</strong></p>
                                            <p style='color: #721c24; font-family: Arial, sans-serif; margin-bottom: 0;'>" + reason + @"</p>
                                        </td>
                                    </tr>

                                    <!-- Next Steps -->
                                    <tr>
                                        <td style='padding: 30px 0;'>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #f8f9fa; border-radius: 8px;'>
                                                <tr>
                                                    <td style='padding: 25px;'>
                                                        <h2 style='color: #3B5BA9; margin: 0 0 20px 0; font-family: Arial, sans-serif; font-size: 20px;'>Next Steps</h2>
                                                        <p style='color: #333333; font-family: Arial, sans-serif; margin-bottom: 15px;'>To complete your application, please:</p>
                                                        <ol style='color: #333333; font-family: Arial, sans-serif; margin-bottom: 15px; padding-left: 20px;'>
                                                            <li style='margin-bottom: 10px;'>Review the reason for additional information above</li>
                                                            <li style='margin-bottom: 10px;'>Submit the requested information by replying to this email</li>
                                                            <li style='margin-bottom: 0;'>Our team will review your application again as soon as possible</li>
                                                        </ol>
                                                        <p style='color: #333333; font-family: Arial, sans-serif; margin-bottom: 0;'>If you have any questions, please don't hesitate to contact our support team at <a href='mailto:support@roovia.co.za' style='color: #3B5BA9; text-decoration: none;'>support@roovia.co.za</a>.</p>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Contact Support Button -->
                                    <tr>
                                        <td>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                <tr>
                                                    <td align='center'>
                                                        <table border='0' cellpadding='0' cellspacing='0'>
                                                            <tr>
                                                                <td align='center' bgcolor='#3B5BA9' class='mobile-button' style='border-radius: 8px;'>
                                                                    <a href='mailto:support@roovia.co.za' target='_blank' 
                                                                       style='font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; 
                                                                              text-decoration: none; padding: 15px 30px; display: inline-block; 
                                                                              font-weight: bold;'>Contact Support</a>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        
                        <!-- Footer -->
                        <tr>
                            <td bgcolor='#f8f9fa' style='padding: 30px; border-radius: 0 0 4px 4px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <tr>
                                        <td style='text-align: center; padding: 0 0 20px 0;'>
                                            <p style='color: #666666; font-family: Arial, sans-serif; font-size: 14px; margin: 0;'>© 2025 Roovia Estate Management. All rights reserved.</p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: center; padding: 20px 0 0 0; border-top: 1px solid #dddddd;'>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Privacy Policy</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Terms of Service</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Contact Us</a>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>");

                mailMessage.Body = body.ToString();
                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Account rejection notification email sent successfully to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending account rejection notification to {user.Email}");
                throw;
            }
        }

        public async Task SendAccountCreationEmailAsync(ApplicationUser user, string passwordResetToken)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("info@roovia.co.za", "Roovia Estate Management"),
                    Subject = "Welcome to Roovia - Account Created",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(user.Email));
                var resetLink = $"https://roovia.co.za/Account/ResetPassword?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(passwordResetToken)}";

                var body = new System.Text.StringBuilder();
                body.AppendLine(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <style type='text/css'>
        /* Reset styles */
        body, #bodyTable { margin: 0; padding: 0; width: 100% !important; }
        img { border: 0; height: auto; line-height: 100%; outline: none; text-decoration: none; }
        table { border-collapse: collapse !important; }
        body { height: 100% !important; margin: 0; padding: 0; width: 100% !important; }
        /* Outlook-specific styles */
        .ReadMsgBody { width: 100%; }
        .ExternalClass { width: 100%; }
        .ExternalClass * { line-height: 100%; }
        /* Mobile-specific styles */
        @media screen and (max-width: 525px) {
            .responsive-table {
                width: 100% !important;
            }
            .mobile-padding {
                padding: 15px 5% !important;
            }
            .mobile-button {
                width: 100% !important;
                padding: 15px 0 !important;
            }
        }
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F5F5F5;'>
    <center>
        <table border='0' cellpadding='0' cellspacing='0' width='100%' id='bodyTable' style='height: 100%; background-color: #F5F5F5;'>
            <tr>
                <td align='center' valign='top'>
                    <table border='0' cellpadding='0' cellspacing='0' width='600' class='responsive-table'>
                       
                        <!-- Main Content -->
                        <tr>
                            <td bgcolor='#ffffff' style='padding: 40px 30px; border-radius: 4px 4px 0 0;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <tr>
                                        <td style='padding: 0 0 30px 0; text-align: center;'>
                                            <h1 style='color: #3B5BA9; margin: 0; font-family: Arial, sans-serif; font-size: 28px; font-weight: bold;'>Welcome to Roovia Estate Management</h1>
                                            <p style='color: #666666; margin: 15px 0 0 0; font-family: Arial, sans-serif; font-size: 16px;'>Your account has been created successfully</p>
                                        </td>
                                    </tr>
                                    
                                    <!-- Account Details -->
                                    <tr>
                                        <td style='background-color: #f8f9fa; border-radius: 8px; padding: 25px; margin-bottom: 30px;'>
                                            <h2 style='color: #3B5BA9; margin: 0 0 20px 0; font-family: Arial, sans-serif; font-size: 20px;'>Account Details</h2>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                <tr>
                                                    <td width='100' style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Name:</strong></td>
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + $"{user.FirstName} {user.LastName}" + @"</td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Email:</strong></td>
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + $"{user.Email}" + @"</td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                    
                                    <!-- Action Required -->
                                    <tr>
                                        <td style='padding: 30px 0;'>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #fff3cd; border-left: 4px solid #ffc107;'>
                                                <tr>
                                                    <td style='padding: 20px;'>
                                                        <p style='color: #856404; margin: 0; font-family: Arial, sans-serif; font-weight: bold;'>Action Required</p>
                                                        <p style='color: #856404; margin: 10px 0 0 0; font-family: Arial, sans-serif;'>Please set your password to activate your account</p>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                    
                                    <!-- Button -->
                                    <tr>
                                        <td style='padding: 0 0 30px 0;'>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                <tr>
                                                    <td align='center'>
                                                        <table border='0' cellpadding='0' cellspacing='0'>
                                                            <tr>
                                                                <td align='center' bgcolor='#3B5BA9' class='mobile-button' style='border-radius: 8px;'>
                                                                    <a href='" + resetLink + @"' target='_blank' style='font-size: 18px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 15px 30px; display: inline-block; font-weight: bold;'>Set Password</a>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                    
                                    <!-- Security Notice -->
                                    <tr>
                                        <td style='background-color: #f8f9fa; border-radius: 8px; padding: 20px; text-align: center;'>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                <tr>
                                                    <td style='text-align: center;'>
                                                        <span style='color: #666666; font-family: Arial, sans-serif; font-size: 14px;'>This link will expire in 24 hours for security reasons.</span>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        
                        <!-- Footer -->
                        <tr>
                            <td bgcolor='#f8f9fa' style='padding: 30px; border-radius: 0 0 4px 4px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <tr>
                                        <td style='text-align: center; padding: 0 0 20px 0;'>
                                            <p style='color: #666666; font-family: Arial, sans-serif; font-size: 14px; margin: 0;'>© 2025 Roovia Estate Management. All rights reserved.</p>
                                            <p style='color: #666666; font-family: Arial, sans-serif; font-size: 12px; margin: 10px 0 0 0;'>
                                                If you did not request this account, please ignore this email or contact our support team.
                                            </p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: center; padding: 20px 0 0 0; border-top: 1px solid #dddddd;'>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Privacy Policy</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Terms of Service</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Contact Us</a>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>");

                mailMessage.Body = body.ToString();
                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Account creation email sent successfully to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending account creation email to {user.Email}");
                throw;
            }
        }

        public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("info@roovia.co.za", "Roovia Estate Management"),
                    Subject = "Reset Your Roovia Password",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(email));

                var body = new System.Text.StringBuilder();
                body.AppendLine(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <style type='text/css'>
        /* Reset styles */
        body, #bodyTable { margin: 0; padding: 0; width: 100% !important; }
        img { border: 0; height: auto; line-height: 100%; outline: none; text-decoration: none; }
        table { border-collapse: collapse !important; }
        body { height: 100% !important; margin: 0; padding: 0; width: 100% !important; }
        /* Outlook-specific styles */
        .ReadMsgBody { width: 100%; }
        .ExternalClass { width: 100%; }
        .ExternalClass * { line-height: 100%; }
        /* Mobile-specific styles */
        @media screen and (max-width: 525px) {
            .responsive-table {
                width: 100% !important;
            }
            .mobile-padding {
                padding: 15px 5% !important;
            }
            .mobile-button {
                width: 100% !important;
                padding: 15px 0 !important;
            }
        }
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F5F5F5;'>
    <center>
        <table border='0' cellpadding='0' cellspacing='0' width='100%' id='bodyTable' style='height: 100%; background-color: #F5F5F5;'>
            <tr>
                <td align='center' valign='top'>
                    <table border='0' cellpadding='0' cellspacing='0' width='600' class='responsive-table'>
                        <tr>
                            <td bgcolor='#ffffff' style='padding: 40px 30px; border-radius: 4px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <tr>
                                        <td style='padding: 0 0 30px 0; text-align: center;'>
                                            <h1 style='color: #3B5BA9; margin: 0; font-family: Arial, sans-serif; font-size: 28px; font-weight: bold;'>Password Reset</h1>
                                            <p style='color: #666666; margin: 15px 0 0 0; font-family: Arial, sans-serif; font-size: 16px;'>You have requested to reset your password for your Roovia account.</p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 20px 0; text-align: center;'>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #f8f9fa; border-radius: 8px;'>
                                                <tr>
                                                    <td style='padding: 25px; text-align: center;'>
                                                        <p style='color: #666666; font-family: Arial, sans-serif; font-size: 16px; margin-bottom: 20px;'>
                                                            Click the button below to set a new password. This link will expire in 24 hours.
                                                        </p>
                                                        <table border='0' cellpadding='0' cellspacing='0' style='margin: 0 auto;'>
                                                            <tr>
                                                                <td align='center' bgcolor='#3B5BA9' style='border-radius: 8px;'>
                                                                    <a href='" + resetLink + @"' target='_blank' 
                                                                       style='font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; 
                                                                              text-decoration: none; padding: 15px 30px; display: inline-block; 
                                                                              font-weight: bold;'>Reset Password</a>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: center;'>
                                            <p style='color: #666666; font-family: Arial, sans-serif; font-size: 14px; margin: 0;'>
                                                If you did not request a password reset, please ignore this email or contact support if you have concerns.
                                            </p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        <tr>
                            <td bgcolor='#f8f9fa' style='padding: 30px; border-radius: 0 0 4px 4px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <tr>
                                        <td style='text-align: center; padding: 0 0 20px 0;'>
                                            <p style='color: #666666; font-family: Arial, sans-serif; font-size: 14px; margin: 0;'>© 2025 Roovia Estate Management. All rights reserved.</p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: center; padding: 20px 0 0 0; border-top: 1px solid #dddddd;'>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Privacy Policy</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Terms of Service</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Contact Us</a>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>");

                mailMessage.Body = body.ToString();
                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Password reset link sent successfully to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset link to {email}");
                throw;
            }
        }

        public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("info@roovia.co.za", "Roovia Estate Management"),
                    Subject = "Confirm Your Roovia Account",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(email));

                var body = new System.Text.StringBuilder();
                body.AppendLine(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <style type='text/css'>
        /* Reset styles */
        body, #bodyTable { margin: 0; padding: 0; width: 100% !important; }
        img { border: 0; height: auto; line-height: 100%; outline: none; text-decoration: none; }
        table { border-collapse: collapse !important; }
        body { height: 100% !important; margin: 0; padding: 0; width: 100% !important; }
        /* Outlook-specific styles */
        .ReadMsgBody { width: 100%; }
        .ExternalClass { width: 100%; }
        .ExternalClass * { line-height: 100%; }
        /* Mobile-specific styles */
        @media screen and (max-width: 525px) {
            .responsive-table {
                width: 100% !important;
            }
            .mobile-padding {
                padding: 15px 5% !important;
            }
            .mobile-button {
                width: 100% !important;
                padding: 15px 0 !important;
            }
        }
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F5F5F5;'>
    <center>
        <table border='0' cellpadding='0' cellspacing='0' width='100%' id='bodyTable' style='height: 100%; background-color: #F5F5F5;'>
            <tr>
                <td align='center' valign='top'>
                    <table border='0' cellpadding='0' cellspacing='0' width='600' class='responsive-table'>
                        <tr>
                            <td bgcolor='#ffffff' style='padding: 40px 30px; border-radius: 4px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <tr>
                                        <td style='padding: 0 0 30px 0; text-align: center;'>
                                            <h1 style='color: #3B5BA9; margin: 0; font-family: Arial, sans-serif; font-size: 28px; font-weight: bold;'>Confirm Your Email</h1>
                                            <p style='color: #666666; margin: 15px 0 0 0; font-family: Arial, sans-serif; font-size: 16px;'>Thank you for registering with Roovia Estate Management</p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 20px 0; text-align: center;'>
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #f8f9fa; border-radius: 8px;'>
                                                <tr>
                                                    <td style='padding: 25px; text-align: center;'>
                                                        <p style='color: #666666; font-family: Arial, sans-serif; font-size: 16px; margin-bottom: 20px;'>
                                                            Please confirm your email address to complete your registration. Click the button below to verify your account.
                                                        </p>
                                                        <table border='0' cellpadding='0' cellspacing='0' style='margin: 0 auto;'>
                                                            <tr>
                                                                <td align='center' bgcolor='#3B5BA9' style='border-radius: 8px;'>
                                                                    <a href='" + confirmationLink + @"' target='_blank' 
                                                                       style='font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; 
                                                                              text-decoration: none; padding: 15px 30px; display: inline-block; 
                                                                              font-weight: bold;'>Confirm Email Address</a>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: center;'>
                                            <p style='color: #666666; font-family: Arial, sans-serif; font-size: 14px; margin: 0;'>
                                                If you did not create an account, you can safely ignore this email.
                                            </p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        <tr>
                            <td bgcolor='#f8f9fa' style='padding: 30px; border-radius: 0 0 4px 4px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                    <tr>
                                        <td style='text-align: center; padding: 0 0 20px 0;'>
                                            <p style='color: #666666; font-family: Arial, sans-serif; font-size: 14px; margin: 0;'>© 2025 Roovia Estate Management. All rights reserved.</p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: center; padding: 20px 0 0 0; border-top: 1px solid #dddddd;'>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Privacy Policy</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Terms of Service</a>
                                            <a href='#' style='color: #3B5BA9; font-family: Arial, sans-serif; font-size: 12px; text-decoration: none; margin: 0 10px;'>Contact Us</a>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>");

                mailMessage.Body = body.ToString();
                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Confirmation link sent successfully to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending confirmation link to {email}");
                throw;
            }
        }

        // Template helpers
        private string GetEmailTemplate(string templateName, object model)
        {
            // In a production environment, this would load templates from files or database
            // For now, using inline templates
            return templateName switch
            {
                "PropertyCreated" => GetPropertyCreatedTemplate(model),
                "TenantWelcome" => GetTenantWelcomeTemplate(model),
                "LeaseExpiryReminder" => GetLeaseExpiryReminderTemplate(model),
                "RentDueReminder" => GetRentDueReminderTemplate(model),
                "MaintenanceTicketCreated" => GetMaintenanceTicketCreatedTemplate(model),
                "MaintenanceCompleted" => GetMaintenanceCompletedTemplate(model),
                "MaintenanceTicketUpdated" => GetMaintenanceTicketUpdatedTemplate(model),
                "VendorAssignment" => GetVendorAssignmentTemplate(model),
                "PaymentReceived" => GetPaymentReceivedTemplate(model),
                "PaymentReceivedOwner" => GetPaymentReceivedOwnerTemplate(model),
                "PaymentReminder" => GetPaymentReminderTemplate(model),
                "BeneficiaryPayment" => GetBeneficiaryPaymentTemplate(model),
                "InspectionScheduled" => GetInspectionScheduledTemplate(model),
                "InspectionCompleted" => GetInspectionCompletedTemplate(model),
                "InspectionReport" => GetInspectionReportTemplate(model),
                _ => GetDefaultTemplate(model)
            };
        }

        // Individual template methods (implement these based on your design requirements)
        private string GetPropertyCreatedTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style type='text/css'>
        /* Your existing styles */
        body, #bodyTable {{ margin: 0; padding: 0; width: 100% !important; }}
        .responsive-table {{ width: 100% !important; }}
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F5F5F5;'>
    <center>
        <table width='600' style='background-color: #ffffff; margin: 20px auto; border-radius: 8px;'>
            <tr>
                <td style='padding: 40px;'>
                    <h1 style='color: #3B5BA9; font-family: Arial, sans-serif;'>New Property Added</h1>
                    <p style='font-family: Arial, sans-serif; color: #666666;'>Dear {model.OwnerName},</p>
                    <p style='font-family: Arial, sans-serif; color: #666666;'>
                        Your property has been successfully added to our system.
                    </p>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #3B5BA9; font-family: Arial, sans-serif;'>Property Details</h3>
                        <p><strong>Name:</strong> {model.PropertyName}</p>
                        <p><strong>Code:</strong> {model.PropertyCode}</p>
                        <p><strong>Address:</strong> {model.Address}</p>
                        <p><strong>Rental Amount:</strong> {model.RentalAmount}</p>
                        <p><strong>Status:</strong> {model.Status}</p>
                    </div>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>";
        }

        private string GetTenantWelcomeTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style type='text/css'>
        body, #bodyTable {{ margin: 0; padding: 0; width: 100% !important; }}
        .responsive-table {{ width: 100% !important; }}
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F5F5F5;'>
    <center>
        <table width='600' style='background-color: #ffffff; margin: 20px auto; border-radius: 8px;'>
            <tr>
                <td style='padding: 40px;'>
                    <h1 style='color: #3B5BA9; font-family: Arial, sans-serif;'>Welcome to Your New Home!</h1>
                    <p style='font-family: Arial, sans-serif; color: #666666;'>Dear {model.TenantName},</p>
                    <p style='font-family: Arial, sans-serif; color: #666666;'>
                        Welcome to {model.PropertyName}! We're excited to have you as our tenant.
                    </p>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #3B5BA9; font-family: Arial, sans-serif;'>Lease Information</h3>
                        <p><strong>Property:</strong> {model.PropertyName}</p>
                        <p><strong>Address:</strong> {model.Address}</p>
                        <p><strong>Lease Start:</strong> {model.LeaseStartDate}</p>
                        <p><strong>Lease End:</strong> {model.LeaseEndDate}</p>
                        <p><strong>Monthly Rent:</strong> {model.RentAmount}</p>
                        <p><strong>Debit Day:</strong> {model.DebitDay}th of each month</p>
                    </div>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>";
        }

        private string GetLeaseExpiryReminderTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style type='text/css'>
        body, #bodyTable {{ margin: 0; padding: 0; width: 100% !important; }}
        .responsive-table {{ width: 100% !important; }}
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F5F5F5;'>
    <center>
        <table width='600' style='background-color: #ffffff; margin: 20px auto; border-radius: 8px;'>
            <tr>
                <td style='padding: 40px;'>
                    <h1 style='color: #3B5BA9; font-family: Arial, sans-serif;'>Lease Expiry Reminder</h1>
                    <p style='font-family: Arial, sans-serif; color: #666666;'>Dear {model.TenantName},</p>
                    <p style='font-family: Arial, sans-serif; color: #666666;'>
                        This is a friendly reminder that your lease for {model.PropertyName} will expire in {model.DaysUntilExpiry} days.
                    </p>
                    <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                        <p><strong>Lease End Date:</strong> {model.LeaseEndDate}</p>
                        <p>Please contact us at {model.ContactEmail} to discuss renewal options.</p>
                    </div>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>";
        }

        private string GetRentDueReminderTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <style type='text/css'>
        body {{ margin: 0; padding: 20px; font-family: Arial, sans-serif; }}
    </style>
</head>
<body>
    <h2>Rent Payment Due</h2>
    <p>Dear {model.TenantName},</p>
    <p>This is a reminder that your rent payment for {model.PropertyName} is due.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <tr><td><strong>Amount Due:</strong></td><td>{model.Amount}</td></tr>
        <tr><td><strong>Due Date:</strong></td><td>{model.DueDate}</td></tr>
        <tr><td><strong>Reference:</strong></td><td>{model.PaymentReference}</td></tr>
    </table>
    <p>Please ensure payment is made on time to avoid late fees.</p>
</body>
</html>";
        }

        private string GetMaintenanceTicketCreatedTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <style type='text/css'>
        body {{ margin: 0; padding: 20px; font-family: Arial, sans-serif; }}
        .priority-high {{ color: #d32f2f; }}
        .priority-medium {{ color: #f57c00; }}
        .priority-low {{ color: #388e3c; }}
    </style>
</head>
<body>
    <h2>Maintenance Ticket Created</h2>
    <p>A new maintenance ticket has been created for {model.PropertyName}.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; width: 100%;'>
        <tr><td><strong>Ticket Number:</strong></td><td>{model.TicketNumber}</td></tr>
        <tr><td><strong>Title:</strong></td><td>{model.Title}</td></tr>
        <tr><td><strong>Description:</strong></td><td>{model.Description}</td></tr>
        <tr><td><strong>Priority:</strong></td><td class='priority-{model.Priority?.ToLower()}'>{model.Priority}</td></tr>
        <tr><td><strong>Category:</strong></td><td>{model.Category}</td></tr>
        <tr><td><strong>Scheduled Date:</strong></td><td>{model.ScheduledDate ?? "To be scheduled"}</td></tr>
    </table>
    <p>We will keep you updated on the progress of this maintenance request.</p>
</body>
</html>";
        }

        private string GetMaintenanceCompletedTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <style type='text/css'>
        body {{ margin: 0; padding: 20px; font-family: Arial, sans-serif; }}
    </style>
</head>
<body>
    <h2>Maintenance Completed</h2>
    <p>The maintenance work for ticket {model.TicketNumber} has been completed.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <tr><td><strong>Property:</strong></td><td>{model.PropertyName}</td></tr>
        <tr><td><strong>Title:</strong></td><td>{model.Title}</td></tr>
        <tr><td><strong>Completed Date:</strong></td><td>{model.CompletedDate}</td></tr>
        <tr><td><strong>Issue Resolved:</strong></td><td>{model.IssueResolved}</td></tr>
        <tr><td><strong>Completion Notes:</strong></td><td>{model.CompletionNotes}</td></tr>
        <tr><td><strong>Cost:</strong></td><td>{model.ActualCost}</td></tr>
    </table>
</body>
</html>";
        }

        private string GetMaintenanceTicketUpdatedTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Maintenance Ticket Updated</h2>
    <p>Ticket {model.TicketNumber} has been updated.</p>
    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <p><strong>Update:</strong> {model.UpdateMessage}</p>
        <p><strong>Status:</strong> {model.Status}</p>
        <p><strong>Updated:</strong> {model.UpdatedDate}</p>
    </div>
</body>
</html>";
        }

        private string GetVendorAssignmentTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>New Maintenance Assignment</h2>
    <p>Dear {model.VendorName},</p>
    <p>You have been assigned a new maintenance ticket.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; width: 100%;'>
        <tr><td><strong>Ticket:</strong></td><td>{model.TicketNumber}</td></tr>
        <tr><td><strong>Title:</strong></td><td>{model.Title}</td></tr>
        <tr><td><strong>Property:</strong></td><td>{model.PropertyName}</td></tr>
        <tr><td><strong>Address:</strong></td><td>{model.PropertyAddress}</td></tr>
        <tr><td><strong>Priority:</strong></td><td>{model.Priority}</td></tr>
        <tr><td><strong>Scheduled:</strong></td><td>{model.ScheduledDate ?? "To be scheduled"}</td></tr>
        <tr><td><strong>Contact:</strong></td><td>{model.ContactPerson}</td></tr>
        <tr><td><strong>Access:</strong></td><td>{model.AccessInstructions ?? "Contact office"}</td></tr>
    </table>
</body>
</html>";
        }

        private string GetPaymentReceivedTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Payment Received</h2>
    <p>Dear {model.TenantName},</p>
    <p>We have received your payment for {model.PropertyName}. Thank you!</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <tr><td><strong>Reference:</strong></td><td>{model.PaymentReference}</td></tr>
        <tr><td><strong>Amount:</strong></td><td>{model.Amount}</td></tr>
        <tr><td><strong>Date:</strong></td><td>{model.PaymentDate}</td></tr>
        <tr><td><strong>Balance:</strong></td><td>{model.Balance}</td></tr>
        <tr><td><strong>Method:</strong></td><td>{model.PaymentMethod}</td></tr>
    </table>
</body>
</html>";
        }

        private string GetPaymentReceivedOwnerTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Payment Received for Your Property</h2>
    <p>Dear {model.OwnerName},</p>
    <p>A payment has been received for {model.PropertyName}.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <tr><td><strong>Tenant:</strong></td><td>{model.TenantName}</td></tr>
        <tr><td><strong>Reference:</strong></td><td>{model.PaymentReference}</td></tr>
        <tr><td><strong>Amount:</strong></td><td>{model.Amount}</td></tr>
        <tr><td><strong>Date:</strong></td><td>{model.PaymentDate}</td></tr>
    </table>
</body>
</html>";
        }

        private string GetPaymentReminderTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Payment Reminder</h2>
    <p>Dear {model.TenantName},</p>
    <p>This is a reminder that your payment for {model.PropertyName} is due.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <tr><td><strong>Amount Due:</strong></td><td>{model.AmountDue}</td></tr>
        <tr><td><strong>Due Day:</strong></td><td>{model.DebitDay}th of the month</td></tr>
        <tr><td><strong>Bank Details:</strong></td><td>{model.BankDetails}</td></tr>
    </table>
</body>
</html>";
        }

        private string GetBeneficiaryPaymentTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Payment Processed</h2>
    <p>Dear {model.BeneficiaryName},</p>
    <p>Your payment has been processed successfully.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <tr><td><strong>Reference:</strong></td><td>{model.PaymentReference}</td></tr>
        <tr><td><strong>Amount:</strong></td><td>{model.Amount}</td></tr>
        <tr><td><strong>Date:</strong></td><td>{model.PaymentDate}</td></tr>
        <tr><td><strong>Property:</strong></td><td>{model.PropertyName}</td></tr>
        <tr><td><strong>Transaction:</strong></td><td>{model.TransactionReference}</td></tr>
        <tr><td><strong>Bank Account:</strong></td><td>{model.BankAccount}</td></tr>
    </table>
</body>
</html>";
        }

        private string GetInspectionScheduledTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Inspection Scheduled</h2>
    <p>An inspection has been scheduled for {model.PropertyName}.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <tr><td><strong>Type:</strong></td><td>{model.InspectionType}</td></tr>
        <tr><td><strong>Date:</strong></td><td>{model.ScheduledDate}</td></tr>
        <tr><td><strong>Inspector:</strong></td><td>{model.InspectorName}</td></tr>
        <tr><td><strong>Note:</strong></td><td>{model.TenantRequired}</td></tr>
    </table>
</body>
</html>";
        }

        private string GetInspectionCompletedTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Inspection Completed</h2>
    <p>Dear {model.OwnerName},</p>
    <p>The inspection for {model.PropertyName} has been completed.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <tr><td><strong>Type:</strong></td><td>{model.InspectionType}</td></tr>
        <tr><td><strong>Date:</strong></td><td>{model.InspectionDate}</td></tr>
        <tr><td><strong>Inspector:</strong></td><td>{model.InspectorName}</td></tr>
        <tr><td><strong>Overall Rating:</strong></td><td>{model.OverallRating}</td></tr>
        <tr><td><strong>Condition:</strong></td><td>{model.OverallCondition}</td></tr>
        <tr><td><strong>Items Needing Maintenance:</strong></td><td>{model.ItemsRequiringMaintenance}</td></tr>
        <tr><td><strong>Next Inspection:</strong></td><td>{model.NextInspectionDue}</td></tr>
    </table>
    <p><strong>Notes:</strong> {model.GeneralNotes}</p>
</body>
</html>";
        }

        private string GetInspectionReportTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Inspection Report Available</h2>
    <p>The inspection report for {model.PropertyName} is now available.</p>
    <table style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        <tr><td><strong>Property:</strong></td><td>{model.PropertyName}</td></tr>
        <tr><td><strong>Address:</strong></td><td>{model.PropertyAddress}</td></tr>
        <tr><td><strong>Inspection Code:</strong></td><td>{model.InspectionCode}</td></tr>
        <tr><td><strong>Type:</strong></td><td>{model.InspectionType}</td></tr>
        <tr><td><strong>Date:</strong></td><td>{model.InspectionDate}</td></tr>
        <tr><td><strong>Inspector:</strong></td><td>{model.InspectorName}</td></tr>
        <tr><td><strong>Overall Rating:</strong></td><td>{model.OverallRating}</td></tr>
        <tr><td><strong>Condition:</strong></td><td>{model.OverallCondition}</td></tr>
    </table>
    <p><a href='{model.ReportUrl}' style='background-color: #3B5BA9; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>View Report</a></p>
</body>
</html>";
        }

        private string GetDefaultTemplate(dynamic model)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Notification from Roovia Estate Management</h2>
    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px;'>
        {Newtonsoft.Json.JsonConvert.SerializeObject(model, Newtonsoft.Json.Formatting.Indented)}
    </div>
</body>
</html>";
        }

        private string GetNewsletterTemplate(string content)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style type='text/css'>
        body {{ margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5; }}
    </style>
</head>
<body>
    <center>
        <table width='600' style='background-color: #ffffff; margin: 20px auto; border-radius: 8px;'>
            <tr>
                <td style='padding: 40px;'>
                    <h1 style='color: #3B5BA9; text-align: center;'>Roovia Estate Management Newsletter</h1>
                    <div style='margin: 30px 0;'>
                        {content}
                    </div>
                    <hr style='border: 1px solid #eee;' />
                    <p style='text-align: center; color: #666; font-size: 12px;'>
                        © 2025 Roovia Estate Management. All rights reserved.<br />
                        You received this email because you are subscribed to our newsletter.
                    </p>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>";
        }
    }
}