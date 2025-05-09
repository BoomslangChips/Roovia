using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Roovia.Models.Users;
using Roovia.Services;

namespace Roovia.Services
{
    public class IdentityEmailSender : IEmailSender<ApplicationUser>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<IdentityEmailSender> _logger;

        public IdentityEmailSender(IEmailService emailService, ILogger<IdentityEmailSender> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            try
            {
                await _emailService.SendConfirmationLinkAsync(user, email, confirmationLink);
                _logger.LogInformation($"Confirmation link sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending confirmation link to {email}");
                throw;
            }
        }

        public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            try
            {
                await _emailService.SendPasswordResetLinkAsync(user, email, resetLink);
                _logger.LogInformation($"Password reset link sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset link to {email}");
                throw;
            }
        }

        public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            // This is not implemented as we're using links rather than codes
            _logger.LogWarning($"SendPasswordResetCodeAsync was called for {email} but is not implemented");
            await Task.CompletedTask;
        }
    }
}