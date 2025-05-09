using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using System.Net.Mime;
using Roovia.Models.Users;

namespace Roovia.Services
{
    public interface IEmailService
    {
        Task SendRegistrationNotificationAsync(Company company, ApplicationUser user);
        Task SendAccountApprovalNotificationAsync(ApplicationUser user);
        Task SendAccountRejectionNotificationAsync(ApplicationUser user, string reason);
        Task SendAccountCreationEmailAsync(ApplicationUser user, string passwordResetToken);
        Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink);
        Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpClient _smtpClient;
        private readonly ILogger<EmailService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;

        private readonly string[] _adminEmails = new[] { "jean@roovia.co.za", "chris@roovia.co.za" };

        public EmailService(ILogger<EmailService> logger, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _smtpClient = new SmtpClient("smtp.office365.com")
            {
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential("info@roovia.co.za", "fqlwdjlbklwcncrc")
            };
            _logger = logger;
            _userManager = userManager;
            _contentTypeProvider = new FileExtensionContentTypeProvider();
        }

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
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + company.GetPrimaryEmail() + @"</td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 8px 0; color: #666666; font-family: Arial, sans-serif;'><strong>Phone:</strong></td>
                                                    <td style='padding: 8px 0; color: #333333; font-family: Arial, sans-serif;'>" + company.GetPrimaryContactNumber() + @"</td>
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
    }
}