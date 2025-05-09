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
using Roovia.Middleware;
using Roovia.Models.Users;
using Roovia.Security;
using Roovia.Services;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Roovia.Extensions;
using Roovia.Models.CDN;
using System.Collections.Concurrent;

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

// Add custom HTTP client for CDN operations with optimized settings
builder.Services.AddHttpClient("CdnClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10); // 10 minute timeout for large file uploads
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
builder.Services.AddScoped<ITenant, TenantService>();
builder.Services.AddScoped<IProperty, PropertyService>();
builder.Services.AddScoped<IPropertyOwner, PropertyOwnerService>();
builder.Services.AddScoped<IUser, UserService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Register utility services
builder.Services.AddScoped<ToastService>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Register CDN service with HttpClientFactory - Updated to use singleton for better caching
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICdnService, CdnService>();

// Register API controllers for CDN endpoints with increased body size limit
builder.Services.AddControllers(options =>
{
    options.MaxModelBindingCollectionSize = 10000; // Allow up to 10,000 items in collections
})
.AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.WriteIndented = true;
})
.ConfigureApiBehaviorOptions(options =>
{
    // Customize model binding error responses
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .Select(e => new { Field = e.Key, Errors = e.Value?.Errors.Select(err => err.ErrorMessage) })
            .ToList();

        return new BadRequestObjectResult(new
        {
            success = false,
            message = "Validation errors occurred",
            errors = errors
        });
    };
});

// Register authorization configuration
// [Authorization configuration code can stay the same]
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
    // Only in development, set up static file serving for local debugging
    // But don't interfere with actual CDN storage
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

// Add global exception handler
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Log the exception
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception in middleware pipeline: {Message}", ex.Message);

        // Return JSON error for API requests
        if (!context.Response.HasStarted && context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync($"{{\"success\":false,\"error\":\"Server error: {ex.Message}\"}}");
        }
        else if (!context.Response.HasStarted)
        {
            // Re-throw for non-API requests to be handled by the error page
            throw;
        }
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Use API key validation middleware for CDN endpoints
app.UseApiKeyValidation();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Map controllers for CDN API endpoints
app.MapControllers();

// Create and ensure permissions for CDN storage directory
// This is now used in BOTH development and production
var cdnStoragePath = builder.Configuration["CDN:StoragePath"];
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
        var standardCategories = new[] { "documents", "images", "test-uploads" };
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

// Optional: Create initial CDN configuration in database if not exists
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Check if any categories exist
        if (!dbContext.Set<CdnCategory>().Any())
        {
            // Create default categories
            var defaultCategories = new List<CdnCategory>
            {
                new CdnCategory { Name = "documents", DisplayName = "Documents", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.txt", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                new CdnCategory { Name = "images", DisplayName = "Images", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.webp,.svg", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                new CdnCategory { Name = "test-uploads", DisplayName = "Test Uploads", AllowedFileTypes = "*", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
            };

            dbContext.AddRange(defaultCategories);
            await dbContext.SaveChangesAsync();

            dbLogger.LogInformation("Created default CDN categories");
        }

        // Check if configuration exists
        if (!dbContext.Set<CdnConfiguration>().Any())
        {
            // Create default configuration
            var config = new CdnConfiguration
            {
                BaseUrl = "https://cdn.roovia.co.za",
                StoragePath = "/var/www/cdn",
                ApiKey = Guid.NewGuid().ToString("N").Substring(0, 24),
                MaxFileSizeMB = 200,
                AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.mp4,.mp3,.zip",
                EnforceAuthentication = true,
                AllowDirectAccess = true,
                EnableCaching = true,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true
            };

            dbContext.Add(config);

            // Create default API key
            var apiKey = new CdnApiKey
            {
                Key = config.ApiKey,
                Name = "Default API Key",
                Description = "Default API key for CDN operations",
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = "System"
            };

            dbContext.Add(apiKey);
            await dbContext.SaveChangesAsync();

            dbLogger.LogInformation("Created default CDN configuration and API key");
        }
    }
    catch (Exception ex)
    {
        var scopeLogger = app.Services.GetRequiredService<ILogger<Program>>();
        scopeLogger.LogError(ex, "An error occurred while initializing CDN configuration");
    }
}

// Optional: Seed permissions and roles
if (app.Environment.IsDevelopment())
{
    try
    {
        await PermissionSeeder.SeedPermissionsAndRoles(app.Services);
    }
    catch (Exception ex)
    {
        var permLogger = app.Services.GetRequiredService<ILogger<Program>>();
        permLogger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();