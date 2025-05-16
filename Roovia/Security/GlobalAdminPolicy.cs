using Microsoft.AspNetCore.Authorization;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Authentication
{
    public static class AuthorizationPolicies
    {
        public const string GlobalAdminPolicy = "GlobalAdminOnly";
    }

    public class GlobalAdminRequirement : IAuthorizationRequirement
    { }

    public class GlobalAdminHandler : AuthorizationHandler<GlobalAdminRequirement>
    {
        private static readonly AsyncLocal<bool> _isCheckingPermission = new AsyncLocal<bool>();

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            GlobalAdminRequirement requirement)
        {
            // Prevent recursive permission checks
            if (_isCheckingPermission.Value)
            {
                // If we're already checking permissions, bypass to prevent infinite loop
                return Task.CompletedTask;
            }

            try
            {
                _isCheckingPermission.Value = true;

                var userRole = context.User.FindFirst("Role")?.Value;

                if (userRole == SystemRole.SystemAdministrator.ToString())
                {
                    context.Succeed(requirement);
                }
            }
            finally
            {
                _isCheckingPermission.Value = false;
            }

            return Task.CompletedTask;
        }
    }
}