using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Roovia.Authentication;
using Roovia.Components;
using Roovia.Components.Account;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Security;
using Roovia.Services;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using Roovia.Models.UserCompanyModels;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for large file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    // Remove all request size limits
    options.Limits.MaxRequestBodySize = null; // No limit
    options.Limits.MaxRequestBufferSize = null; // No limit
    options.Limits.MinRequestBodyDataRate = null; // No minimum rate

    // Increase request timeout for large file uploads
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// Configure IIS for large file uploads (if using IIS)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = null; // No limit
});

builder.Services.AddMemoryCache();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure FormOptions for larger file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue; // No limit
    options.ValueLengthLimit = int.MaxValue; // No limit
    options.MultipartHeadersLengthLimit = 32768; // 32KB
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Register Email Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSender>();
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
//builder.Services.AddScoped<ITenant, TenantService>();
//builder.Services.AddScoped<IProperty, PropertyService>();
//builder.Services.AddScoped<IPropertyOwner, PropertyOwnerService>();
builder.Services.AddScoped<IUser, UserService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Register utility services
builder.Services.AddScoped<ToastService>();

// Register CDN service as singleton for better performance
builder.Services.AddSingleton<ICdnService, CdnService>();

// Register the seed data service
builder.Services.AddScoped<ISeedDataService, SeedDataService>();

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

// Register authorization handler providers
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, GlobalAdminHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
        RequestPath = "",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=3600");
        }
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Add CDN static file serving
var cdnStoragePath = builder.Configuration["CDN:StoragePath"];
if (!string.IsNullOrEmpty(cdnStoragePath) && Directory.Exists(cdnStoragePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(cdnStoragePath),
        RequestPath = "/cdn",
        OnPrepareResponse = ctx =>
        {
            // Set cache control for CDN files
            var headers = ctx.Context.Response.Headers;
            headers.Add("Cache-Control", "public, max-age=31536000"); // 1 year
            headers.Add("Access-Control-Allow-Origin", "*");
        }
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Create and ensure permissions for CDN storage directory
var logger = app.Services.GetRequiredService<ILogger<Program>>();

if (string.IsNullOrEmpty(cdnStoragePath))
{
    logger.LogError("CDN:StoragePath is not configured in appsettings.json");
}
else
{
    try
    {
        // Ensure the main CDN directory exists
        if (!Directory.Exists(cdnStoragePath))
        {
            logger.LogInformation("Creating CDN storage directory: {Path}", cdnStoragePath);
            Directory.CreateDirectory(cdnStoragePath);
        }

        // Create standard category directories
        var standardCategories = new[] { "documents", "images", "logos", "profiles"};
        foreach (var category in standardCategories)
        {
            var categoryPath = Path.Combine(cdnStoragePath, category);
            if (!Directory.Exists(categoryPath))
            {
                logger.LogInformation("Creating category directory: {Path}", categoryPath);
                Directory.CreateDirectory(categoryPath);
            }
        }

        // Test write permissions
        var testFile = Path.Combine(cdnStoragePath, $"test_{Guid.NewGuid()}.txt");
        try
        {
            File.WriteAllText(testFile, "Test write permissions");
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
                logger.LogInformation("Successfully verified write permissions to CDN storage: {Path}", cdnStoragePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write to CDN storage directory: {Path}. Error: {Error}",
                cdnStoragePath, ex.Message);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error setting up CDN storage directory: {Path}", cdnStoragePath);
    }
}

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // First, ensure the database exists and is migrated
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        // Seed all lookup tables
        var seedDataService = services.GetRequiredService<ISeedDataService>();
        await seedDataService.InitializeAsync();

        // Seed permissions and roles
        await PermissionSeeder.SeedPermissionsAndRoles(services);

        logger.LogInformation("Database initialization and seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        // In production, you might want to handle this differently
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

app.Run();