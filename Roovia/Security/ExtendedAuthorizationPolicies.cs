//using Microsoft.AspNetCore.Authorization;
//using Roovia.Models.UserCompanyModels;
//using System.Linq;
//using System.Security.Claims;

//namespace Roovia.Authentication
//{
//    public static class ExtendedAuthorizationPolicies
//    {
//        // Role-based policies (extending your existing GlobalAdminPolicy)
//        public const string CompanyAdminPolicy = "CompanyAdminOnly";
//        public const string BranchManagerPolicy = "BranchManagerOnly";
//        public const string AdminAccessPolicy = "AdminAccess";  // For Global, Company, and Branch admins
//        public const string PropertyManagerPolicy = "PropertyManagerAccess";
//        public const string FinancialOfficerPolicy = "FinancialAccess";
//        public const string TenantOfficerPolicy = "TenantAccess";

//        // Function-based policies (based on permission categories)
//        public const string PropertiesPolicy = "PropertiesAccess";
//        public const string BeneficiariesPolicy = "BeneficiariesAccess";
//        public const string TenantsPolicy = "TenantsAccess";
//        public const string ReportsPolicy = "ReportsAccess";
//        public const string PaymentsPolicy = "PaymentsAccess";
//        public const string SystemSettingsPolicy = "SystemSettingsAccess";

//        // Register all extended policies
//        public static void ConfigureExtendedPolicies(AuthorizationOptions options)
//        {
//            // Role-based policies
//            options.AddPolicy(CompanyAdminPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.CompanyAdmin.ToString())));

//            options.AddPolicy(BranchManagerPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.BranchManager.ToString())));

//            options.AddPolicy(AdminAccessPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    context.User.HasClaim(c => c.Type == "Role" &&
//                        (c.Value == SystemRole.GlobalAdmin.ToString() ||
//                         c.Value == SystemRole.CompanyAdmin.ToString() ||
//                         c.Value == SystemRole.BranchManager.ToString()))));

//            options.AddPolicy(PropertyManagerPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()) ||
//                    context.User.HasClaim(c => c.Type == "CustomRole" && c.Value == "Property Manager")));

//            options.AddPolicy(FinancialOfficerPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()) ||
//                    context.User.HasClaim(c => c.Type == "CustomRole" && c.Value == "Financial Officer")));

//            options.AddPolicy(TenantOfficerPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()) ||
//                    context.User.HasClaim(c => c.Type == "CustomRole" && c.Value == "Tenant Officer")));

//            // Permission category-based policies
//            options.AddPolicy(PropertiesPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    HasPermissionStartingWith(context.User, "properties") ||
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString())));

//            options.AddPolicy(BeneficiariesPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    HasPermissionStartingWith(context.User, "beneficiaries") ||
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString())));

//            options.AddPolicy(TenantsPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    HasPermissionStartingWith(context.User, "tenants") ||
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString())));

//            options.AddPolicy(ReportsPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    HasPermissionStartingWith(context.User, "reports") ||
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString())));

//            options.AddPolicy(PaymentsPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    HasPermissionStartingWith(context.User, "payments") ||
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString())));

//            options.AddPolicy(SystemSettingsPolicy, policy =>
//                policy.RequireAssertion(context =>
//                    HasPermissionStartingWith(context.User, "settings") ||
//                    context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString())));
//        }

//        // Helper method to check if the user has any permissions starting with the given prefix
//        private static bool HasPermissionStartingWith(ClaimsPrincipal user, string prefix)
//        {
//            return user.Claims
//                .Where(c => c.Type == "Permission")
//                .Any(c => c.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
//        }
//    }

//    // Custom handlers for specific policies that need more complex logic
//    public class AdminAccessRequirement : IAuthorizationRequirement { }

//    public class AdminAccessHandler : AuthorizationHandler<AdminAccessRequirement>
//    {
//        protected override Task HandleRequirementAsync(
//            AuthorizationHandlerContext context,
//            AdminAccessRequirement requirement)
//        {
//            var userRole = context.User.FindFirst("Role")?.Value;

//            if (userRole == SystemRole.GlobalAdmin.ToString() ||
//                userRole == SystemRole.CompanyAdmin.ToString() ||
//                userRole == SystemRole.BranchManager.ToString())
//            {
//                context.Succeed(requirement);
//                return Task.CompletedTask;
//            }

//            // Check for user management permission as fallback
//            if (context.User.HasClaim(c => c.Type == "Permission" && c.Value == "settings.users"))
//            {
//                context.Succeed(requirement);
//            }

//            return Task.CompletedTask;
//        }
//    }

//    // Category-based permission requirement for broader authorization
//    public class CategoryPermissionRequirement : IAuthorizationRequirement
//    {
//        public string Category { get; }

//        public CategoryPermissionRequirement(string category)
//        {
//            Category = category;
//        }
//    }

//    public class CategoryPermissionHandler : AuthorizationHandler<CategoryPermissionRequirement>
//    {
//        protected override Task HandleRequirementAsync(
//            AuthorizationHandlerContext context,
//            CategoryPermissionRequirement requirement)
//        {
//            // Always allow Global Admins
//            if (context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()))
//            {
//                context.Succeed(requirement);
//                return Task.CompletedTask;
//            }

//            // Check if the user has any permission in this category
//            var hasPermission = context.User.Claims
//                .Where(c => c.Type == "Permission")
//                .Any(c => c.Value.StartsWith(requirement.Category, StringComparison.OrdinalIgnoreCase));

//            if (hasPermission)
//            {
//                context.Succeed(requirement);
//            }

//            return Task.CompletedTask;
//        }
//    }
//}