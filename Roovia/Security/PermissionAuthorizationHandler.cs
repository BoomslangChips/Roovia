using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Roovia.Interfaces;
using System.Security.Claims;

namespace Roovia.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissionService;
        private readonly AuthenticationStateProvider _authStateProvider;

        public PermissionAuthorizationHandler(
            IPermissionService permissionService,
            AuthenticationStateProvider authStateProvider)
        {
            _permissionService = permissionService;
            _authStateProvider = authStateProvider;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Get current user
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity.IsAuthenticated)
            {
                return; // Not authenticated, deny access
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return; // No user ID, deny access
            }

            // Check if user has the required permission
            var hasPermission = await _permissionService.UserHasPermission(userId, requirement.Permission);
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}