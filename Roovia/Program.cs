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
    // Set maximum request body size to 200MB
    options.Limits.MaxRequestBodySize = 209715200; // 200MB

    // Increase request timeout
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// Configure IIS for large file uploads (if using IIS)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 209715200; // 200MB
});
builder.Services.AddMemoryCache();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure FormOptions for larger file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 209715200; // 200MB
    options.ValueLengthLimit = 209715200; // 200MB
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
.ConfigureApiBehaviorOptions(options =>
{
    // Customize model binding error responses
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .Select(e => new { Field = e.Key, Errors = e.Value.Errors.Select(err => err.ErrorMessage) })
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
    var cdnStoragePath = builder.Configuration["CDN:StoragePath"];

    // If we can't access the CDN storage path directly in development, set up local serving
    if (!Directory.Exists(cdnStoragePath))
    {
        // Create local directory for development
        var localCdnPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "cdn");
        Directory.CreateDirectory(localCdnPath);

        // Create category directories (this will be replaced by database-driven categories later)
        using (var scope = app.Services.CreateScope())
        {
            var cdnService = scope.ServiceProvider.GetRequiredService<ICdnService>();
            var categories = await cdnService.GetCategoriesAsync();

            foreach (var category in categories)
            {
                Directory.CreateDirectory(Path.Combine(localCdnPath, category.Name));
            }
        }

        // Set up local static file serving for development
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(localCdnPath),
            RequestPath = "/cdn",
            OnPrepareResponse = ctx =>
            {
                // Set caching headers for better performance
                ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=3600");
            }
        });
    }
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

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

// Optional: Create initial CDN configuration in database if not exists
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Check if any categories exist
        if (!dbContext.Set<CdnCategory>().Any())
        {
            // Create default categories
            var defaultCategories = new List<CdnCategory>
            {
                new CdnCategory { Name = "documents", DisplayName = "Documents", AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.txt", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
                new CdnCategory { Name = "images", DisplayName = "Images", AllowedFileTypes = ".jpg,.jpeg,.png,.gif,.webp,.svg", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "System" },
            };

            dbContext.AddRange(defaultCategories);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Created default CDN categories");
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

            logger.LogInformation("Created default CDN configuration and API key");
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing CDN configuration");
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
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();