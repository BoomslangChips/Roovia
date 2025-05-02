# Install EF Core Tools
dotnet tool install --global dotnet-ef

# Update EF Core Tools
dotnet tool update --global dotnet-ef

# Create a New Migration
dotnet ef migrations add InitialMigration --project Roovia

# Create a Migration (specifying DbContext)
dotnet ef migrations add InitialMigration --context ApplicationDbContext --project Roovia

# Apply Migration to Database
dotnet ef database update --project Roovia

# Generate SQL Script (without applying)
dotnet ef migrations script --project Roovia

# List Existing Migrations
dotnet ef migrations list --project Roovia

# Remove the Last Migration (if not applied to database)
dotnet ef migrations remove --project Roovia