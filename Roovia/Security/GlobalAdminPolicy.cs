using Microsoft.AspNetCore.Authorization;
using Roovia.Models.UserCompanyModels;
using Roovia.Security;

namespace Roovia.Authentication
{
    public static class AuthorizationPolicies
    {
        public const string GlobalAdminPolicy = "GlobalAdminOnly";
    }

    public class GlobalAdminRequirement : IAuthorizationRequirement { }

    public class GlobalAdminHandler : AuthorizationHandler<GlobalAdminRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            GlobalAdminRequirement requirement)
        {
            var userRole = context.User.FindFirst("Role")?.Value;

            if (userRole == SystemRole.GlobalAdmin.ToString())
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}