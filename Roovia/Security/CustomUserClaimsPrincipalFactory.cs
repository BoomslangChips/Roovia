using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Roovia.Interfaces;
using Roovia.Models.UserCompanyModels;
using System.Security.Claims;

public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly IPermissionService _permissionService;

    public CustomUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor,
        IPermissionService permissionService)
        : base(userManager, optionsAccessor)
    {
        _permissionService = permissionService;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Add system role claim
        identity.AddClaim(new Claim("Role", user.Role.ToString()));

        // Get custom roles from database
        var userRolesResponse = await _permissionService.GetUserRoles(user.Id);
        if (userRolesResponse.ResponseInfo.Success && userRolesResponse.Response is List<UserRoleAssignment> userRoles)
        {
            foreach (var userRole in userRoles)
            {
                identity.AddClaim(new Claim("CustomRole", userRole.Role?.Name ?? ""));
            }
        }

        // Get permissions and add them as claims - this now includes overrides
        var permissions = await _permissionService.GetUserPermissions(user.Id);
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("Permission", permission));
        }

        return identity;
    }
}