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
        private static readonly AsyncLocal<bool> _isCheckingPermission = new AsyncLocal<bool>();

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
            // Prevent recursive permission checks
            if (_isCheckingPermission.Value)
            {
                // If we're already checking permissions, bypass to prevent infinite loop
                // Don't succeed or fail, just return to let other handlers decide
                return;
            }

            try
            {
                _isCheckingPermission.Value = true;

                // For database-related resources, bypass permission check to prevent circular dependency
                if (context.Resource != null && IsDatabaseResource(context.Resource))
                {
                    // Don't succeed here, just return to avoid interfering with other handlers
                    return;
                }

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
            catch (Exception)
            {
                // Log exception if you have a logger
                // Don't fail the requirement, just return
                return;
            }
            finally
            {
                _isCheckingPermission.Value = false;
            }
        }

        private bool IsDatabaseResource(object resource)
        {
            // Check if the resource is a database-related resource
            if (resource is string resourceString)
            {
                return resourceString.StartsWith("Database") ||
                       resourceString.StartsWith("DbContext") ||
                       resourceString.Contains("ApplicationDbContext");
            }

            var resourceType = resource.GetType();
            return resourceType.Name.Contains("DbContext") ||
                   resourceType.Namespace?.Contains("EntityFramework") == true ||
                   resourceType.Namespace?.Contains("Identity") == true;
        }
    }
}