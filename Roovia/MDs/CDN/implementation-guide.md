# CDN System Implementation Guide

This guide provides step-by-step instructions to implement the enhanced CDN system for your Roovia application.

## Prerequisites

- .NET 9 development environment
- SQL Server instance
- Existing Blazor application structure
- Administrative access to deploy and configure the system

## Step 1: Add Database Models and Run SQL Scripts

1. Add the `Models/CDN` directory to your project
2. Place the `CdnConfiguration.cs` model file in this directory
3. Update your `ApplicationDbContext.cs` to include the new CDN models:

```csharp
// Add to ApplicationDbContext.cs
public DbSet<CdnConfiguration> CdnConfigurations { get; set; }
public DbSet<CdnApiKey> CdnApiKeys { get; set; }
public DbSet<CdnUsageStatistic> CdnUsageStatistics { get; set; }
public DbSet<CdnFileMetadata> CdnFileMetadata { get; set; }
public DbSet<CdnAccessLog> CdnAccessLogs { get; set; }
```

4. Run the SQL script (`Create_CDN_Tables.sql`) to create the required tables in your database

## Step 2: Deploy the CDN Middleware and Extensions

1. Create a `Middleware` directory in your project
2. Add the `ApiKeyMiddleware.cs` file
3. Create an `Extensions` directory in your project
4. Add the `ApiKeyMiddlewareExtensions.cs` file
5. Update your `Program.cs` to register and use the middleware as shown in the provided code

## Step 3: Replace the CDN Service

1. Replace your existing `CdnService.cs` with the enhanced version
2. Make sure the service is registered properly in your DI container

## Step 4: Update CDN Controllers

1. Add the new `ExtendedCdnController.cs` to your Controllers directory
2. Ensure all controller route mappings are correctly registered

## Step 5: Add JavaScript Resources

1. Create or update the `/wwwroot/js/` directory
2. Add the `cdnHelpers.js` file
3. Add reference to this script in your `_Layout.cshtml` or appropriate place:

```html
<script src="~/js/cdnHelpers.js"></script>
```

## Step 6: Update Razor Components

1. Replace the existing `CdnDashboard.razor` with the enhanced version
2. Replace the existing `CdnConfigAdmin.razor` with the enhanced version
3. Ensure all component dependencies are properly referenced
4. Verify that the components are correctly registered in your routing system

## Step 7: First-Time Configuration

1. Navigate to `/admin/cdn-config` in your application
2. Configure the initial CDN settings:
   - Base URL
   - Storage Path
   - API Key
   - File Size Limits
   - Allowed File Types
   - Categories
3. Test the connection using the "Test Connection" button
4. Save the configuration to persist it to the database

## Step 8: Verify Component Integration

1. Navigate to `/cdn-dashboard` to verify the dashboard component works
2. Test file uploads and management features
3. Verify that folders can be created, navigated, and managed
4. Confirm that the API key validation is working

## Step 9: Security Considerations

1. Ensure your API keys are properly secured
2. Configure proper permissions for the CDN storage path
3. Set up appropriate user authentication for CDN administrative pages
4. Consider adding HTTPS enforcement for all CDN operations

## Step 10: Schedule Maintenance Tasks

1. Consider adding a background service to run the following tasks regularly:
   - Clean orphaned files
   - Optimize storage
   - Update usage statistics
   - Check for expired API keys

## Troubleshooting

### Common Issues

1. **File Access Permission Problems**
   - Ensure the application has read/write access to the CDN storage path
   - Verify folder permissions match the application's execution context

2. **API Key Validation Failures**
   - Check that the API key is correctly set in the database
   - Verify the middleware is correctly registering in the pipeline

3. **Database Connection Issues**
   - Confirm the connection string is correct
   - Ensure the database user has appropriate permissions

4. **Missing JavaScript Functionality**
   - Verify that the CDN helper JavaScript is correctly loaded
   - Check browser console for any JavaScript errors

### Logging

The system includes comprehensive logging. Check the logs for:

1. File upload/download operations
2. API key validation issues
3. Storage optimization operations
4. Configuration changes

## Next Steps

After successful implementation, consider these enhancements:

1. Set up a CDN provider (like Azure CDN, CloudFlare, etc.) for better performance
2. Implement file thumbnailing for image previews
3. Add more sophisticated file compression and optimization
4. Implement role-based access controls for different categories
5. Add detailed analytics and reporting features