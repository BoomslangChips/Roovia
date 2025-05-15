using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Roovia.Interfaces;
using Roovia.Models.UserCompanyModels;
using System.Security.Claims;

namespace Roovia.Security
{
    public static class PermissionExtensions
    {
        public static AuthorizationPolicy GetPolicy(string permission)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return policy;
        }

        public static string GetPermissionPolicy(string permission)
        {
            return $"{PermissionPolicyProvider.POLICY_PREFIX}{permission}";
        }
    }

    public class PermissionAuthorizeView : ComponentBase
    {
        [Inject] private IPermissionService PermissionService { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }

        [Parameter] public string? Permission { get; set; }
        [Parameter] public string? Category { get; set; }  // New parameter for category-based auth
        [Parameter] public string? Role { get; set; }      // New parameter for role-based auth
        [Parameter] public string? CustomRole { get; set; }  // New parameter for custom role auth
        [Parameter] public bool RequireAll { get; set; } = false;  // New parameter to require all conditions
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public RenderFragment? NotAuthorized { get; set; }

        private bool isAuthorized = false;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity.IsAuthenticated)
            {
                isAuthorized = false;
                return;
            }

            // Global Admins always have access
            if (user.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()))
            {
                isAuthorized = true;
                return;
            }

            // Build a list of authorization checks
            var checks = new List<bool>();

            // Check for specific permission (original functionality)
            if (!string.IsNullOrEmpty(Permission))
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    checks.Add(await PermissionService.UserHasPermission(userId, Permission));
                }
            }

            // Check for category permissions
            if (!string.IsNullOrEmpty(Category))
            {
                var categoryPermission = user.Claims
                    .Where(c => c.Type == "Permission")
                    .Any(c => c.Value.StartsWith(Category, StringComparison.OrdinalIgnoreCase));

                checks.Add(categoryPermission);
            }

            // Check for system role
            if (!string.IsNullOrEmpty(Role))
            {
                checks.Add(user.HasClaim(c => c.Type == "Role" && c.Value == Role));
            }

            // Check for custom role
            if (!string.IsNullOrEmpty(CustomRole))
            {
                checks.Add(user.HasClaim(c => c.Type == "CustomRole" && c.Value == CustomRole));
            }

            // If no checks were added, default to not authorized
            if (checks.Count == 0)
            {
                isAuthorized = false;
                return;
            }

            // Determine authorization based on RequireAll parameter
            isAuthorized = RequireAll
                ? checks.All(check => check)    // All checks must pass
                : checks.Any(check => check);   // Any check can pass
        }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            if (isAuthorized)
            {
                builder.AddContent(0, ChildContent);
            }
            else if (NotAuthorized != null)
            {
                builder.AddContent(1, NotAuthorized);
            }
        }
    }
}