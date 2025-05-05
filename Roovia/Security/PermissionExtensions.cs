using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Roovia.Interfaces;
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

        [Parameter] public string Permission { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public RenderFragment NotAuthorized { get; set; }

        private bool isAuthorized = false;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity.IsAuthenticated)
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    isAuthorized = await PermissionService.UserHasPermission(userId, Permission);
                }
            }
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