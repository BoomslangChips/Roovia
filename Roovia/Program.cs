using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roovia.Authentication;
using Roovia.Components;
using Roovia.Components.Account;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.Users;
using Roovia.Security;
using Roovia.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register DbContext as scoped (default)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register DbContextFactory with the SAME LIFETIME (scoped)
builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString),
    lifetime: ServiceLifetime.Scoped);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

// Register domain services
builder.Services.AddScoped<ITenant, TenantService>();
builder.Services.AddScoped<IProperty, PropertyService>();
builder.Services.AddScoped<IPropertyOwner, PropertyOwnerService>();
builder.Services.AddScoped<IUser, UserService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Register utility services
builder.Services.AddScoped<ToastService>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Register authorization configuration
builder.Services.AddAuthorization(options =>
{
    // Register pre-defined policies
    options.AddPolicy(AuthorizationPolicies.GlobalAdminPolicy, policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString())));

    // Add AdminAccess policy
    options.AddPolicy("AdminAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "Role" &&
                (c.Value == SystemRole.GlobalAdmin.ToString() ||
                 c.Value == SystemRole.CompanyAdmin.ToString() ||
                 c.Value == SystemRole.BranchManager.ToString())) ||
            context.User.HasClaim(c => c.Type == "Permission" && c.Value == "settings.users")));

    // Add functional area policies
    options.AddPolicy("PropertiesAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()) ||
            context.User.FindAll("Permission").Any(c => c.Value.StartsWith("properties"))));

    options.AddPolicy("TenantsAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()) ||
            context.User.FindAll("Permission").Any(c => c.Value.StartsWith("tenants"))));

    options.AddPolicy("BeneficiariesAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()) ||
            context.User.FindAll("Permission").Any(c => c.Value.StartsWith("beneficiaries"))));

    options.AddPolicy("ReportsAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()) ||
            context.User.FindAll("Permission").Any(c => c.Value.StartsWith("reports"))));

    options.AddPolicy("PaymentsAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()) ||
            context.User.FindAll("Permission").Any(c => c.Value.StartsWith("payments"))));

    options.AddPolicy("SystemSettingsAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "Role" && c.Value == SystemRole.GlobalAdmin.ToString()) ||
            context.User.FindAll("Permission").Any(c => c.Value.StartsWith("settings"))));
});
builder.Services.AddScoped<ICdnService, CdnService>();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
// Register authorization handler providers - IMPORTANT: Fixed lifetime issues
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

// Register simple authorization handlers that don't use scoped services as singletons
builder.Services.AddSingleton<IAuthorizationHandler, GlobalAdminHandler>();

// Register handlers that need scoped services as scoped services - THIS FIXES THE ERRORS
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
app.MapControllers();
// Optional: Seed permissions and roles
if (app.Environment.IsDevelopment())
{
    try
    {
        await PermissionSeeder.SeedPermissionsAndRoles(app.Services);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();