using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roovia.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SystemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CDN_AccessLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CDN_AccessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CDN_Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AllowedFileTypes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CDN_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CDN_Configurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaseUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MaxFileSizeMB = table.Column<int>(type: "int", nullable: false),
                    AllowedFileTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EnableCaching = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CDN_Configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_AllocationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_AllocationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_BankNameTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultBranchCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_BankNameTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_BeneficiaryPaymentStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_BeneficiaryPaymentStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_BeneficiaryStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_BeneficiaryStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_BeneficiaryTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_BeneficiaryTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_BranchStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_BranchStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_CommissionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_CommissionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_CommunicationChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_CommunicationChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_CommunicationDirections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_CommunicationDirections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_CompanyStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_CompanyStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_ConditionLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ScoreValue = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_ConditionLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_ContactNumberTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_ContactNumberTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_DocumentAccessLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_DocumentAccessLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_DocumentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_DocumentCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_DocumentRequirementTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_DocumentRequirementTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_DocumentStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_DocumentStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_DocumentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_DocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_EntityTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SystemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_EntityTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_InspectionAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_InspectionAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_InspectionStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_InspectionStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_InspectionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_InspectionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_MaintenanceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_MaintenanceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_MaintenanceImageTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_MaintenanceImageTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_MaintenancePriorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeHours = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_MaintenancePriorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_MaintenanceStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_MaintenanceStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_MediaTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_MediaTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_NoteTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_NoteTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_NotificationChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_NotificationChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_NotificationEventTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SystemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemEvent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_NotificationEventTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_NotificationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_NotificationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PaymentFrequencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DaysInterval = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PaymentFrequencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ProcessingFeePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ProcessingFeeFixed = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PaymentRuleTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PaymentRuleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PaymentStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PaymentStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PaymentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PaymentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PermissionCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PermissionCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PropertyImageTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PropertyImageTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PropertyOwnerStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PropertyOwnerStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PropertyOwnerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PropertyOwnerTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PropertyStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PropertyStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_PropertyTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_PropertyTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_RecurrenceFrequencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DaysMultiplier = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_RecurrenceFrequencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_ReminderStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_ReminderStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_ReminderTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_ReminderTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_RoleTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_RoleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BillingCycleDays = table.Column<int>(type: "int", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: true),
                    MaxProperties = table.Column<int>(type: "int", nullable: true),
                    MaxBranches = table.Column<int>(type: "int", nullable: true),
                    HasTrialPeriod = table.Column<bool>(type: "bit", nullable: false),
                    TrialPeriodDays = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_TenantStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_TenantStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_TenantTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_TenantTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_ThemeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CssVariables = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDarkTheme = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_ThemeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_TwoFactorMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_TwoFactorMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_UserStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_UserStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportFrequencyTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DaysInterval = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportFrequencyTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CDN_Folders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CDN_Folders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CDN_Folders_CDN_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CDN_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CDN_Folders_CDN_Folders_ParentId",
                        column: x => x.ParentId,
                        principalTable: "CDN_Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CDN_UsageStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    FileCount = table.Column<int>(type: "int", nullable: false),
                    StorageUsedBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadCount = table.Column<int>(type: "int", nullable: false),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    DeleteCount = table.Column<int>(type: "int", nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CDN_UsageStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CDN_UsageStatistics_CDN_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CDN_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Contact_Media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MediaTypeId = table.Column<int>(type: "int", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact_Media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_Media_Lookup_MediaTypes_MediaTypeId",
                        column: x => x.MediaTypeId,
                        principalTable: "Lookup_MediaTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationEventTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BodyTemplate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SmsTemplate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_NotificationTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lookup_NotificationTemplates_Lookup_NotificationEventTypes_NotificationEventTypeId",
                        column: x => x.NotificationEventTypeId,
                        principalTable: "Lookup_NotificationEventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CDN_FileMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    FolderId = table.Column<int>(type: "int", nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastAccessDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessCount = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasBase64Backup = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CDN_FileMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CDN_FileMetadata_CDN_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CDN_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CDN_FileMetadata_CDN_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "CDN_Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AspNetCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VatNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MainLogoId = table.Column<int>(type: "int", nullable: true),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_UnitNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_ComplexName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_BuildingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Floor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Suburb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_GateCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_IsResidential = table.Column<bool>(type: "bit", nullable: false),
                    Address_Latitude = table.Column<double>(type: "float", nullable: true),
                    Address_Longitude = table.Column<double>(type: "float", nullable: true),
                    Address_DeliveryInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BankAccount_AccountType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccount_AccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankAccount_BankNameId = table.Column<int>(type: "int", nullable: true),
                    BankAccount_BranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "int", nullable: true),
                    SubscriptionStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsTrialPeriod = table.Column<bool>(type: "bit", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: true),
                    MaxProperties = table.Column<int>(type: "int", nullable: true),
                    MaxBranches = table.Column<int>(type: "int", nullable: true),
                    Settings = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    RemovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetCompanies_CDN_FileMetadata_MainLogoId",
                        column: x => x.MainLogoId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AspNetCompanies_Lookup_BankNameTypes_BankAccount_BankNameId",
                        column: x => x.BankAccount_BankNameId,
                        principalTable: "Lookup_BankNameTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetCompanies_Lookup_CompanyStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_CompanyStatusTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetCompanies_Lookup_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "Lookup_SubscriptionPlans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CDN_Base64Storage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileMetadataId = table.Column<int>(type: "int", nullable: false),
                    Base64Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CDN_Base64Storage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CDN_Base64Storage_CDN_FileMetadata_FileMetadataId",
                        column: x => x.FileMetadataId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MainLogoId = table.Column<int>(type: "int", nullable: true),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_UnitNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_ComplexName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_BuildingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Floor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Suburb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_GateCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_IsResidential = table.Column<bool>(type: "bit", nullable: false),
                    Address_Latitude = table.Column<double>(type: "float", nullable: true),
                    Address_Longitude = table.Column<double>(type: "float", nullable: true),
                    Address_DeliveryInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BankAccount_AccountType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccount_AccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankAccount_BankNameId = table.Column<int>(type: "int", nullable: true),
                    BankAccount_BranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsHeadOffice = table.Column<bool>(type: "bit", nullable: false),
                    Settings = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MaxUsers = table.Column<int>(type: "int", nullable: true),
                    MaxProperties = table.Column<int>(type: "int", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    RemovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetBranches_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetBranches_CDN_FileMetadata_MainLogoId",
                        column: x => x.MainLogoId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AspNetBranches_Lookup_BankNameTypes_BankAccount_BankNameId",
                        column: x => x.BankAccount_BankNameId,
                        principalTable: "Lookup_BankNameTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetBranches_Lookup_BranchStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_BranchStatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Data_PropertyOwners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PropertyOwnerTypeId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VatNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_UnitNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_ComplexName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_BuildingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Floor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Suburb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_GateCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_IsResidential = table.Column<bool>(type: "bit", nullable: false),
                    Address_Latitude = table.Column<double>(type: "float", nullable: true),
                    Address_Longitude = table.Column<double>(type: "float", nullable: true),
                    Address_DeliveryInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BankAccount_AccountType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccount_AccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankAccount_BankNameId = table.Column<int>(type: "int", nullable: true),
                    BankAccount_BranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CustomerRef = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    RemovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_PropertyOwners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_PropertyOwners_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Data_PropertyOwners_Lookup_BankNameTypes_BankAccount_BankNameId",
                        column: x => x.BankAccount_BankNameId,
                        principalTable: "Lookup_BankNameTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Data_PropertyOwners_Lookup_PropertyOwnerStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_PropertyOwnerStatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Data_PropertyOwners_Lookup_PropertyOwnerTypes_PropertyOwnerTypeId",
                        column: x => x.PropertyOwnerTypeId,
                        principalTable: "Lookup_PropertyOwnerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Finance_PaymentRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RuleTypeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: true),
                    LateFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LateFeePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CompoundLateFees = table.Column<bool>(type: "bit", nullable: false),
                    SendReminders = table.Column<bool>(type: "bit", nullable: false),
                    FirstReminderDays = table.Column<int>(type: "int", nullable: true),
                    SecondReminderDays = table.Column<int>(type: "int", nullable: true),
                    FinalReminderDays = table.Column<int>(type: "int", nullable: true),
                    AutoAllocate = table.Column<bool>(type: "bit", nullable: false),
                    AllocationOrder = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Finance_PaymentRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Finance_PaymentRules_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Finance_PaymentRules_Lookup_PaymentRuleTypes_RuleTypeId",
                        column: x => x.RuleTypeId,
                        principalTable: "Lookup_PaymentRuleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lookup_EntityDocumentRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityTypeId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    DocumentRequirementTypeId = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookup_EntityDocumentRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lookup_EntityDocumentRequirements_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Lookup_EntityDocumentRequirements_Lookup_DocumentRequirementTypes_DocumentRequirementTypeId",
                        column: x => x.DocumentRequirementTypeId,
                        principalTable: "Lookup_DocumentRequirementTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lookup_EntityDocumentRequirements_Lookup_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "Lookup_DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lookup_EntityDocumentRequirements_Lookup_EntityTypes_EntityTypeId",
                        column: x => x.EntityTypeId,
                        principalTable: "Lookup_EntityTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Maint_Vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_UnitNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_ComplexName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_BuildingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Floor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Suburb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_GateCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_IsResidential = table.Column<bool>(type: "bit", nullable: false),
                    Address_Latitude = table.Column<double>(type: "float", nullable: true),
                    Address_Longitude = table.Column<double>(type: "float", nullable: true),
                    Address_DeliveryInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Specializations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    TotalJobs = table.Column<int>(type: "int", nullable: true),
                    BankAccount_AccountType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccount_AccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankAccount_BankNameId = table.Column<int>(type: "int", nullable: true),
                    BankAccount_BranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HasInsurance = table.Column<bool>(type: "bit", nullable: false),
                    InsurancePolicyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InsuranceExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maint_Vendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maint_Vendors_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Maint_Vendors_Lookup_BankNameTypes_BankAccount_BankNameId",
                        column: x => x.BankAccount_BankNameId,
                        principalTable: "Lookup_BankNameTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetCustomRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    IsPreset = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetCustomRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetCustomRoles_AspNetBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AspNetBranches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetCustomRoles_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    ProfilePictureId = table.Column<int>(type: "int", nullable: true),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Role = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequireChangePasswordOnLogin = table.Column<bool>(type: "bit", nullable: false),
                    UserPreferences = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsTwoFactorRequired = table.Column<bool>(type: "bit", nullable: false),
                    PreferredTwoFactorMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoginFailureCount = table.Column<int>(type: "int", nullable: false),
                    LastLoginFailureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginIpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    RemovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_AspNetBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AspNetBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_CDN_FileMetadata_ProfilePictureId",
                        column: x => x.ProfilePictureId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Lookup_UserStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_UserStatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Data_Properties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    PropertyTypeId = table.Column<int>(type: "int", nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PropertyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerRef = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RentalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PropertyAccountBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    ServiceLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HasTenant = table.Column<bool>(type: "bit", nullable: false),
                    LeaseOriginalStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentLeaseStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentTenantId = table.Column<int>(type: "int", nullable: true),
                    CommissionTypeId = table.Column<int>(type: "int", nullable: false),
                    CommissionValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PaymentsVerify = table.Column<bool>(type: "bit", nullable: false),
                    MainImageId = table.Column<int>(type: "int", nullable: true),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_UnitNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_ComplexName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_BuildingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Floor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Suburb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_GateCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_IsResidential = table.Column<bool>(type: "bit", nullable: false),
                    Address_Latitude = table.Column<double>(type: "float", nullable: true),
                    Address_Longitude = table.Column<double>(type: "float", nullable: true),
                    Address_DeliveryInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    RemovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_Properties_AspNetBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AspNetBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Data_Properties_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Data_Properties_CDN_FileMetadata_MainImageId",
                        column: x => x.MainImageId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Data_Properties_Data_PropertyOwners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Data_PropertyOwners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Data_Properties_Lookup_CommissionTypes_CommissionTypeId",
                        column: x => x.CommissionTypeId,
                        principalTable: "Lookup_CommissionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Data_Properties_Lookup_PropertyStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_PropertyStatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Data_Properties_Lookup_PropertyTypes_PropertyTypeId",
                        column: x => x.PropertyTypeId,
                        principalTable: "Lookup_PropertyTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRolePermissions_AspNetCustomRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetCustomRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetRolePermissions_AspNetPermissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "AspNetPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserPermissionOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserPermissionOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserPermissionOverrides_AspNetPermissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "AspNetPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserPermissionOverrides_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoleAssignments_AspNetCustomRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetCustomRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoleAssignments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contact_Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationEventTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    RecipientUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    SmsSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SmsSentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_Notifications_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notifications_Lookup_NotificationEventTypes_NotificationEventTypeId",
                        column: x => x.NotificationEventTypeId,
                        principalTable: "Lookup_NotificationEventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Query = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomReports_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomReports_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomReports_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReportDashboards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    LayoutColumns = table.Column<int>(type: "int", nullable: false),
                    Configuration = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDashboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportDashboards_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportDashboards_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportDashboards_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Data_PropertyBeneficiaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_UnitNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_ComplexName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_BuildingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Floor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Suburb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_GateCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_IsResidential = table.Column<bool>(type: "bit", nullable: false),
                    Address_Latitude = table.Column<double>(type: "float", nullable: true),
                    Address_Longitude = table.Column<double>(type: "float", nullable: true),
                    Address_DeliveryInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BenTypeId = table.Column<int>(type: "int", nullable: false),
                    CommissionTypeId = table.Column<int>(type: "int", nullable: false),
                    CommissionValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PropertyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BenStatusId = table.Column<int>(type: "int", nullable: false),
                    CustomerRefBeneficiary = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerRefProperty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Agent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BankAccount_AccountType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccount_AccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankAccount_BankNameId = table.Column<int>(type: "int", nullable: true),
                    BankAccount_BranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_PropertyBeneficiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_PropertyBeneficiaries_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Data_PropertyBeneficiaries_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Data_PropertyBeneficiaries_Lookup_BankNameTypes_BankAccount_BankNameId",
                        column: x => x.BankAccount_BankNameId,
                        principalTable: "Lookup_BankNameTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Data_PropertyBeneficiaries_Lookup_BeneficiaryStatusTypes_BenStatusId",
                        column: x => x.BenStatusId,
                        principalTable: "Lookup_BeneficiaryStatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Data_PropertyBeneficiaries_Lookup_BeneficiaryTypes_BenTypeId",
                        column: x => x.BenTypeId,
                        principalTable: "Lookup_BeneficiaryTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Data_PropertyBeneficiaries_Lookup_CommissionTypes_CommissionTypeId",
                        column: x => x.CommissionTypeId,
                        principalTable: "Lookup_CommissionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inspect_PropertyInspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    InspectionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InspectionTypeId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InspectorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InspectorUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GeneralNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OverallRating = table.Column<int>(type: "int", nullable: true),
                    OverallConditionId = table.Column<int>(type: "int", nullable: true),
                    TenantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantPresent = table.Column<bool>(type: "bit", nullable: true),
                    ReportDocumentId = table.Column<int>(type: "int", nullable: true),
                    NextInspectionDue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspect_PropertyInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspect_PropertyInspections_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspect_PropertyInspections_CDN_FileMetadata_ReportDocumentId",
                        column: x => x.ReportDocumentId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Inspect_PropertyInspections_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspect_PropertyInspections_Lookup_ConditionLevels_OverallConditionId",
                        column: x => x.OverallConditionId,
                        principalTable: "Lookup_ConditionLevels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Inspect_PropertyInspections_Lookup_InspectionStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_InspectionStatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inspect_PropertyInspections_Lookup_InspectionTypes_InspectionTypeId",
                        column: x => x.InspectionTypeId,
                        principalTable: "Lookup_InspectionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomReportId = table.Column<int>(type: "int", nullable: true),
                    StandardReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FrequencyTypeId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    ExecutionTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    RecipientEmails = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExportFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastRunDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextRunDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportSchedules_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportSchedules_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportSchedules_CustomReports_CustomReportId",
                        column: x => x.CustomReportId,
                        principalTable: "CustomReports",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportSchedules_ReportFrequencyTypes_FrequencyTypeId",
                        column: x => x.FrequencyTypeId,
                        principalTable: "ReportFrequencyTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportDashboardWidgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DashboardId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WidgetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomReportId = table.Column<int>(type: "int", nullable: true),
                    StandardReportType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VisualizationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GridColumn = table.Column<int>(type: "int", nullable: false),
                    GridRow = table.Column<int>(type: "int", nullable: false),
                    GridWidth = table.Column<int>(type: "int", nullable: false),
                    GridHeight = table.Column<int>(type: "int", nullable: false),
                    Configuration = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AutoRefresh = table.Column<bool>(type: "bit", nullable: false),
                    RefreshInterval = table.Column<int>(type: "int", nullable: true),
                    LastRefreshed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDashboardWidgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportDashboardWidgets_CustomReports_CustomReportId",
                        column: x => x.CustomReportId,
                        principalTable: "CustomReports",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportDashboardWidgets_ReportDashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "ReportDashboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Data_PropertyTenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TenantTypeId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VatNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LeaseStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaseEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DebitDayOfMonth = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LastPaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastInvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReminderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BankAccount_AccountType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccount_AccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankAccount_BankNameId = table.Column<int>(type: "int", nullable: true),
                    BankAccount_BranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_UnitNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_ComplexName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_BuildingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Floor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Suburb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address_Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_GateCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_IsResidential = table.Column<bool>(type: "bit", nullable: false),
                    Address_Latitude = table.Column<double>(type: "float", nullable: true),
                    Address_Longitude = table.Column<double>(type: "float", nullable: true),
                    Address_DeliveryInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EmergencyContactRelationship = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerRefTenant = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerRefProperty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponsibleAgent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResponsibleUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LeaseDocumentId = table.Column<int>(type: "int", nullable: true),
                    MoveInDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MoveOutDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MoveInInspectionCompleted = table.Column<bool>(type: "bit", nullable: false),
                    MoveInInspectionId = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    RemovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_PropertyTenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_PropertyTenants_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Data_PropertyTenants_CDN_FileMetadata_LeaseDocumentId",
                        column: x => x.LeaseDocumentId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Data_PropertyTenants_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Data_PropertyTenants_Inspect_PropertyInspections_MoveInInspectionId",
                        column: x => x.MoveInInspectionId,
                        principalTable: "Inspect_PropertyInspections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Data_PropertyTenants_Lookup_BankNameTypes_BankAccount_BankNameId",
                        column: x => x.BankAccount_BankNameId,
                        principalTable: "Lookup_BankNameTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Data_PropertyTenants_Lookup_TenantStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_TenantStatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Data_PropertyTenants_Lookup_TenantTypes_TenantTypeId",
                        column: x => x.TenantTypeId,
                        principalTable: "Lookup_TenantTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inspect_InspectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InspectionId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    ConditionId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequiresMaintenance = table.Column<bool>(type: "bit", nullable: false),
                    MaintenancePriorityId = table.Column<int>(type: "int", nullable: true),
                    MaintenanceNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspect_InspectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspect_InspectionItems_CDN_FileMetadata_ImageId",
                        column: x => x.ImageId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Inspect_InspectionItems_Inspect_PropertyInspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspect_PropertyInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inspect_InspectionItems_Lookup_ConditionLevels_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "Lookup_ConditionLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inspect_InspectionItems_Lookup_InspectionAreas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Lookup_InspectionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inspect_InspectionItems_Lookup_MaintenancePriorities_MaintenancePriorityId",
                        column: x => x.MaintenancePriorityId,
                        principalTable: "Lookup_MaintenancePriorities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReportExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportScheduleId = table.Column<int>(type: "int", nullable: true),
                    ReportType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExecutionStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutionEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowCount = table.Column<int>(type: "int", nullable: true),
                    OutputFilePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CdnFileMetadataId = table.Column<int>(type: "int", nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    RecipientEmails = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ExecutedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportExecutionLogs_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportExecutionLogs_AspNetUsers_ExecutedBy",
                        column: x => x.ExecutedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportExecutionLogs_ReportSchedules_ReportScheduleId",
                        column: x => x.ReportScheduleId,
                        principalTable: "ReportSchedules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Contact_Communications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CommunicationChannelId = table.Column<int>(type: "int", nullable: false),
                    CommunicationDirectionId = table.Column<int>(type: "int", nullable: false),
                    FromEmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ToEmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FromPhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ToPhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityStringId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedPropertyId = table.Column<int>(type: "int", nullable: true),
                    RelatedOwnerId = table.Column<int>(type: "int", nullable: true),
                    RelatedTenantId = table.Column<int>(type: "int", nullable: true),
                    RelatedBeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    RelatedVendorId = table.Column<int>(type: "int", nullable: true),
                    AttachmentId = table.Column<int>(type: "int", nullable: true),
                    CommunicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PropertyBeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    PropertyId = table.Column<int>(type: "int", nullable: true),
                    PropertyOwnerId = table.Column<int>(type: "int", nullable: true),
                    PropertyTenantId = table.Column<int>(type: "int", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact_Communications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_Communications_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Communications_Data_PropertyBeneficiaries_PropertyBeneficiaryId",
                        column: x => x.PropertyBeneficiaryId,
                        principalTable: "Data_PropertyBeneficiaries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Communications_Data_PropertyOwners_PropertyOwnerId",
                        column: x => x.PropertyOwnerId,
                        principalTable: "Data_PropertyOwners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Communications_Data_PropertyTenants_PropertyTenantId",
                        column: x => x.PropertyTenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Communications_Lookup_CommunicationChannels_CommunicationChannelId",
                        column: x => x.CommunicationChannelId,
                        principalTable: "Lookup_CommunicationChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_Communications_Lookup_CommunicationDirections_CommunicationDirectionId",
                        column: x => x.CommunicationDirectionId,
                        principalTable: "Lookup_CommunicationDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_Communications_Maint_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Maint_Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Contact_ContactNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactNumberTypeId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityStringId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    PropertyOwnerId = table.Column<int>(type: "int", nullable: true),
                    PropertyTenantId = table.Column<int>(type: "int", nullable: true),
                    PropertyBeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact_ContactNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_ContactNumbers_AspNetBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AspNetBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contact_ContactNumbers_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_ContactNumbers_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contact_ContactNumbers_Data_PropertyBeneficiaries_PropertyBeneficiaryId",
                        column: x => x.PropertyBeneficiaryId,
                        principalTable: "Data_PropertyBeneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_ContactNumbers_Data_PropertyOwners_PropertyOwnerId",
                        column: x => x.PropertyOwnerId,
                        principalTable: "Data_PropertyOwners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_ContactNumbers_Data_PropertyTenants_PropertyTenantId",
                        column: x => x.PropertyTenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_ContactNumbers_Lookup_ContactNumberTypes_ContactNumberTypeId",
                        column: x => x.ContactNumberTypeId,
                        principalTable: "Lookup_ContactNumberTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_ContactNumbers_Maint_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Maint_Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contact_Emails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityStringId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    PropertyOwnerId = table.Column<int>(type: "int", nullable: true),
                    PropertyTenantId = table.Column<int>(type: "int", nullable: true),
                    PropertyBeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact_Emails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_Emails_AspNetBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AspNetBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contact_Emails_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_Emails_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contact_Emails_Data_PropertyBeneficiaries_PropertyBeneficiaryId",
                        column: x => x.PropertyBeneficiaryId,
                        principalTable: "Data_PropertyBeneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_Emails_Data_PropertyOwners_PropertyOwnerId",
                        column: x => x.PropertyOwnerId,
                        principalTable: "Data_PropertyOwners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_Emails_Data_PropertyTenants_PropertyTenantId",
                        column: x => x.PropertyTenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_Emails_Maint_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Maint_Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contact_NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityStringId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NotificationEventTypeId = table.Column<int>(type: "int", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PushEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WebEnabled = table.Column<bool>(type: "bit", nullable: false),
                    OnlyDuringBusinessHours = table.Column<bool>(type: "bit", nullable: false),
                    PreferredTimeOfDay = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    PropertyBeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    PropertyOwnerId = table.Column<int>(type: "int", nullable: true),
                    PropertyTenantId = table.Column<int>(type: "int", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_NotificationPreferences_AspNetBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AspNetBranches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_NotificationPreferences_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_NotificationPreferences_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_NotificationPreferences_Data_PropertyBeneficiaries_PropertyBeneficiaryId",
                        column: x => x.PropertyBeneficiaryId,
                        principalTable: "Data_PropertyBeneficiaries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_NotificationPreferences_Data_PropertyOwners_PropertyOwnerId",
                        column: x => x.PropertyOwnerId,
                        principalTable: "Data_PropertyOwners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_NotificationPreferences_Data_PropertyTenants_PropertyTenantId",
                        column: x => x.PropertyTenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_NotificationPreferences_Lookup_NotificationEventTypes_NotificationEventTypeId",
                        column: x => x.NotificationEventTypeId,
                        principalTable: "Lookup_NotificationEventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_NotificationPreferences_Maint_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Maint_Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Finance_PaymentSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ScheduleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FrequencyId = table.Column<int>(type: "int", nullable: false),
                    DayOfMonth = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AutoGenerate = table.Column<bool>(type: "bit", nullable: false),
                    DaysBeforeDue = table.Column<int>(type: "int", nullable: false),
                    LastGeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Finance_PaymentSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Finance_PaymentSchedules_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Finance_PaymentSchedules_Data_PropertyTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Finance_PaymentSchedules_Lookup_PaymentFrequencies_FrequencyId",
                        column: x => x.FrequencyId,
                        principalTable: "Lookup_PaymentFrequencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Finance_PropertyPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsLate = table.Column<bool>(type: "bit", nullable: false),
                    DaysLate = table.Column<int>(type: "int", nullable: true),
                    LateFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProcessingFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsAllocated = table.Column<bool>(type: "bit", nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AllocatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceiptDocumentId = table.Column<int>(type: "int", nullable: true),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Finance_PropertyPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Finance_PropertyPayments_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Finance_PropertyPayments_CDN_FileMetadata_ReceiptDocumentId",
                        column: x => x.ReceiptDocumentId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Finance_PropertyPayments_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Finance_PropertyPayments_Data_PropertyTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Finance_PropertyPayments_Lookup_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "Lookup_PaymentMethods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Finance_PropertyPayments_Lookup_PaymentStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_PaymentStatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Finance_PropertyPayments_Lookup_PaymentTypes_PaymentTypeId",
                        column: x => x.PaymentTypeId,
                        principalTable: "Lookup_PaymentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Maint_MaintenanceTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    InspectionId = table.Column<int>(type: "int", nullable: true),
                    TicketNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    PriorityId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignedToName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedDuration = table.Column<TimeSpan>(type: "time", nullable: true),
                    ActualDuration = table.Column<TimeSpan>(type: "time", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TenantResponsible = table.Column<bool>(type: "bit", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresTenantAccess = table.Column<bool>(type: "bit", nullable: false),
                    TenantNotified = table.Column<bool>(type: "bit", nullable: true),
                    TenantNotificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IssueResolved = table.Column<bool>(type: "bit", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maint_MaintenanceTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceTickets_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceTickets_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceTickets_Data_PropertyTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceTickets_Inspect_PropertyInspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspect_PropertyInspections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceTickets_Lookup_MaintenanceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Lookup_MaintenanceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceTickets_Lookup_MaintenancePriorities_PriorityId",
                        column: x => x.PriorityId,
                        principalTable: "Lookup_MaintenancePriorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceTickets_Lookup_MaintenanceStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_MaintenanceStatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceTickets_Maint_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Maint_Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Finance_PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    BeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    AllocationTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AllocationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllocatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Finance_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Finance_PaymentAllocations_Data_PropertyBeneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Data_PropertyBeneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Finance_PaymentAllocations_Finance_PropertyPayments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Finance_PropertyPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Finance_PaymentAllocations_Lookup_AllocationTypes_AllocationTypeId",
                        column: x => x.AllocationTypeId,
                        principalTable: "Lookup_AllocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contact_EntityDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    CdnFileMetadataId = table.Column<int>(type: "int", nullable: true),
                    DocumentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    MaintenanceTicketId = table.Column<int>(type: "int", nullable: true),
                    PropertyBeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    PropertyId = table.Column<int>(type: "int", nullable: true),
                    PropertyInspectionId = table.Column<int>(type: "int", nullable: true),
                    PropertyOwnerId = table.Column<int>(type: "int", nullable: true),
                    PropertyPaymentId = table.Column<int>(type: "int", nullable: true),
                    PropertyTenantId = table.Column<int>(type: "int", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact_EntityDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_AspNetBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AspNetBranches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Data_PropertyBeneficiaries_PropertyBeneficiaryId",
                        column: x => x.PropertyBeneficiaryId,
                        principalTable: "Data_PropertyBeneficiaries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Data_PropertyOwners_PropertyOwnerId",
                        column: x => x.PropertyOwnerId,
                        principalTable: "Data_PropertyOwners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Data_PropertyTenants_PropertyTenantId",
                        column: x => x.PropertyTenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Finance_PropertyPayments_PropertyPaymentId",
                        column: x => x.PropertyPaymentId,
                        principalTable: "Finance_PropertyPayments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Inspect_PropertyInspections_PropertyInspectionId",
                        column: x => x.PropertyInspectionId,
                        principalTable: "Inspect_PropertyInspections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Lookup_DocumentStatuses_DocumentStatusId",
                        column: x => x.DocumentStatusId,
                        principalTable: "Lookup_DocumentStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Lookup_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "Lookup_DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Maint_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "Maint_MaintenanceTickets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_EntityDocuments_Maint_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Maint_Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Contact_Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    NoteTypeId = table.Column<int>(type: "int", nullable: false),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityStringId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    MaintenanceTicketId = table.Column<int>(type: "int", nullable: true),
                    PropertyBeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    PropertyId = table.Column<int>(type: "int", nullable: true),
                    PropertyInspectionId = table.Column<int>(type: "int", nullable: true),
                    PropertyOwnerId = table.Column<int>(type: "int", nullable: true),
                    PropertyPaymentId = table.Column<int>(type: "int", nullable: true),
                    PropertyTenantId = table.Column<int>(type: "int", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_Notes_AspNetBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AspNetBranches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_AspNetCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "AspNetCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_Data_PropertyBeneficiaries_PropertyBeneficiaryId",
                        column: x => x.PropertyBeneficiaryId,
                        principalTable: "Data_PropertyBeneficiaries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_Data_PropertyOwners_PropertyOwnerId",
                        column: x => x.PropertyOwnerId,
                        principalTable: "Data_PropertyOwners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_Data_PropertyTenants_PropertyTenantId",
                        column: x => x.PropertyTenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_Finance_PropertyPayments_PropertyPaymentId",
                        column: x => x.PropertyPaymentId,
                        principalTable: "Finance_PropertyPayments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_Inspect_PropertyInspections_PropertyInspectionId",
                        column: x => x.PropertyInspectionId,
                        principalTable: "Inspect_PropertyInspections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_Lookup_NoteTypes_NoteTypeId",
                        column: x => x.NoteTypeId,
                        principalTable: "Lookup_NoteTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_Notes_Maint_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "Maint_MaintenanceTickets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Notes_Maint_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Maint_Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Contact_Reminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReminderTypeId = table.Column<int>(type: "int", nullable: false),
                    ReminderStatusId = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrenceFrequencyId = table.Column<int>(type: "int", nullable: true),
                    RecurrenceInterval = table.Column<int>(type: "int", nullable: true),
                    RecurrenceEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SendNotification = table.Column<bool>(type: "bit", nullable: false),
                    NotifyDaysBefore = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityStringId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    MaintenanceTicketId = table.Column<int>(type: "int", nullable: true),
                    PropertyBeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    PropertyId = table.Column<int>(type: "int", nullable: true),
                    PropertyOwnerId = table.Column<int>(type: "int", nullable: true),
                    PropertyTenantId = table.Column<int>(type: "int", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact_Reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_Data_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Data_Properties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_Data_PropertyBeneficiaries_PropertyBeneficiaryId",
                        column: x => x.PropertyBeneficiaryId,
                        principalTable: "Data_PropertyBeneficiaries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_Data_PropertyOwners_PropertyOwnerId",
                        column: x => x.PropertyOwnerId,
                        principalTable: "Data_PropertyOwners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_Data_PropertyTenants_PropertyTenantId",
                        column: x => x.PropertyTenantId,
                        principalTable: "Data_PropertyTenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_Lookup_RecurrenceFrequencies_RecurrenceFrequencyId",
                        column: x => x.RecurrenceFrequencyId,
                        principalTable: "Lookup_RecurrenceFrequencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_Lookup_ReminderStatuses_ReminderStatusId",
                        column: x => x.ReminderStatusId,
                        principalTable: "Lookup_ReminderStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_Lookup_ReminderTypes_ReminderTypeId",
                        column: x => x.ReminderTypeId,
                        principalTable: "Lookup_ReminderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_Maint_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "Maint_MaintenanceTickets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contact_Reminders_Maint_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Maint_Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Maint_MaintenanceComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenanceTicketId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maint_MaintenanceComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceComments_Maint_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "Maint_MaintenanceTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Maint_MaintenanceExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenanceTicketId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceiptDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maint_MaintenanceExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceExpenses_CDN_FileMetadata_ReceiptDocumentId",
                        column: x => x.ReceiptDocumentId,
                        principalTable: "CDN_FileMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceExpenses_Lookup_ExpenseCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Lookup_ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceExpenses_Maint_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "Maint_MaintenanceTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Maint_MaintenanceExpenses_Maint_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Maint_Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Finance_BeneficiaryPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeneficiaryId = table.Column<int>(type: "int", nullable: false),
                    PaymentAllocationId = table.Column<int>(type: "int", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Finance_BeneficiaryPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Finance_BeneficiaryPayments_Data_PropertyBeneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Data_PropertyBeneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Finance_BeneficiaryPayments_Finance_PaymentAllocations_PaymentAllocationId",
                        column: x => x.PaymentAllocationId,
                        principalTable: "Finance_PaymentAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Finance_BeneficiaryPayments_Lookup_BeneficiaryPaymentStatusTypes_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Lookup_BeneficiaryPaymentStatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetBranches_BankAccount_BankNameId",
                table: "AspNetBranches",
                column: "BankAccount_BankNameId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetBranches_CompanyId",
                table: "AspNetBranches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetBranches_MainLogoId",
                table: "AspNetBranches",
                column: "MainLogoId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetBranches_StatusId",
                table: "AspNetBranches",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetCompanies_BankAccount_BankNameId",
                table: "AspNetCompanies",
                column: "BankAccount_BankNameId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetCompanies_MainLogoId",
                table: "AspNetCompanies",
                column: "MainLogoId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetCompanies_StatusId",
                table: "AspNetCompanies",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetCompanies_SubscriptionPlanId",
                table: "AspNetCompanies",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetCustomRoles_BranchId",
                table: "AspNetCustomRoles",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetCustomRoles_CompanyId",
                table: "AspNetCustomRoles",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRolePermissions_PermissionId",
                table: "AspNetRolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRolePermissions_RoleId",
                table: "AspNetRolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserPermissionOverrides_PermissionId",
                table: "AspNetUserPermissionOverrides",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserPermissionOverrides_UserId",
                table: "AspNetUserPermissionOverrides",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoleAssignments_RoleId",
                table: "AspNetUserRoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoleAssignments_UserId",
                table: "AspNetUserRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BranchId",
                table: "AspNetUsers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyId",
                table: "AspNetUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ProfilePictureId",
                table: "AspNetUsers",
                column: "ProfilePictureId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StatusId",
                table: "AspNetUsers",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CDN_AccessLogs_ActionType",
                table: "CDN_AccessLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_CDN_AccessLogs_Timestamp",
                table: "CDN_AccessLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CDN_Base64Storage_FileMetadataId",
                table: "CDN_Base64Storage",
                column: "FileMetadataId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CDN_Categories_Name",
                table: "CDN_Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CDN_FileMetadata_CategoryId_FolderId",
                table: "CDN_FileMetadata",
                columns: new[] { "CategoryId", "FolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_CDN_FileMetadata_FolderId",
                table: "CDN_FileMetadata",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_CDN_FileMetadata_IsDeleted",
                table: "CDN_FileMetadata",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CDN_FileMetadata_Url",
                table: "CDN_FileMetadata",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_CDN_Folders_CategoryId_Path",
                table: "CDN_Folders",
                columns: new[] { "CategoryId", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CDN_Folders_ParentId",
                table: "CDN_Folders",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_CDN_UsageStatistics_CategoryId",
                table: "CDN_UsageStatistics",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CDN_UsageStatistics_Date_CategoryId",
                table: "CDN_UsageStatistics",
                columns: new[] { "Date", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Communications_CommunicationChannelId",
                table: "Contact_Communications",
                column: "CommunicationChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Communications_CommunicationDirectionId",
                table: "Contact_Communications",
                column: "CommunicationDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Communications_PropertyBeneficiaryId",
                table: "Contact_Communications",
                column: "PropertyBeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Communications_PropertyId",
                table: "Contact_Communications",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Communications_PropertyOwnerId",
                table: "Contact_Communications",
                column: "PropertyOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Communications_PropertyTenantId",
                table: "Contact_Communications",
                column: "PropertyTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Communications_VendorId",
                table: "Contact_Communications",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_ApplicationUserId",
                table: "Contact_ContactNumbers",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_BranchId",
                table: "Contact_ContactNumbers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_CompanyId",
                table: "Contact_ContactNumbers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_ContactNumberTypeId",
                table: "Contact_ContactNumbers",
                column: "ContactNumberTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_PropertyBeneficiaryId",
                table: "Contact_ContactNumbers",
                column: "PropertyBeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_PropertyOwnerId",
                table: "Contact_ContactNumbers",
                column: "PropertyOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_PropertyTenantId",
                table: "Contact_ContactNumbers",
                column: "PropertyTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_RelatedEntityType_RelatedEntityId_IsPrimary",
                table: "Contact_ContactNumbers",
                columns: new[] { "RelatedEntityType", "RelatedEntityId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1 AND [RelatedEntityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_RelatedEntityType_RelatedEntityStringId_IsPrimary",
                table: "Contact_ContactNumbers",
                columns: new[] { "RelatedEntityType", "RelatedEntityStringId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1 AND [RelatedEntityStringId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_ContactNumbers_VendorId",
                table: "Contact_ContactNumbers",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Emails_ApplicationUserId",
                table: "Contact_Emails",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Emails_BranchId",
                table: "Contact_Emails",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Emails_CompanyId",
                table: "Contact_Emails",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Emails_PropertyBeneficiaryId",
                table: "Contact_Emails",
                column: "PropertyBeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Emails_PropertyOwnerId",
                table: "Contact_Emails",
                column: "PropertyOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Emails_PropertyTenantId",
                table: "Contact_Emails",
                column: "PropertyTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Emails_RelatedEntityType_RelatedEntityId_IsPrimary",
                table: "Contact_Emails",
                columns: new[] { "RelatedEntityType", "RelatedEntityId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1 AND [RelatedEntityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Emails_RelatedEntityType_RelatedEntityStringId_IsPrimary",
                table: "Contact_Emails",
                columns: new[] { "RelatedEntityType", "RelatedEntityStringId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1 AND [RelatedEntityStringId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Emails_VendorId",
                table: "Contact_Emails",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_ApplicationUserId",
                table: "Contact_EntityDocuments",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_BranchId",
                table: "Contact_EntityDocuments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_CompanyId",
                table: "Contact_EntityDocuments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_DocumentStatusId",
                table: "Contact_EntityDocuments",
                column: "DocumentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_DocumentTypeId",
                table: "Contact_EntityDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_MaintenanceTicketId",
                table: "Contact_EntityDocuments",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_PropertyBeneficiaryId",
                table: "Contact_EntityDocuments",
                column: "PropertyBeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_PropertyId",
                table: "Contact_EntityDocuments",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_PropertyInspectionId",
                table: "Contact_EntityDocuments",
                column: "PropertyInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_PropertyOwnerId",
                table: "Contact_EntityDocuments",
                column: "PropertyOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_PropertyPaymentId",
                table: "Contact_EntityDocuments",
                column: "PropertyPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_PropertyTenantId",
                table: "Contact_EntityDocuments",
                column: "PropertyTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_EntityDocuments_VendorId",
                table: "Contact_EntityDocuments",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Media_MediaTypeId",
                table: "Contact_Media",
                column: "MediaTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_ApplicationUserId",
                table: "Contact_Notes",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_BranchId",
                table: "Contact_Notes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_CompanyId",
                table: "Contact_Notes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_MaintenanceTicketId",
                table: "Contact_Notes",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_NoteTypeId",
                table: "Contact_Notes",
                column: "NoteTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_PropertyBeneficiaryId",
                table: "Contact_Notes",
                column: "PropertyBeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_PropertyId",
                table: "Contact_Notes",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_PropertyInspectionId",
                table: "Contact_Notes",
                column: "PropertyInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_PropertyOwnerId",
                table: "Contact_Notes",
                column: "PropertyOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_PropertyPaymentId",
                table: "Contact_Notes",
                column: "PropertyPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_PropertyTenantId",
                table: "Contact_Notes",
                column: "PropertyTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notes_VendorId",
                table: "Contact_Notes",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_NotificationPreferences_ApplicationUserId",
                table: "Contact_NotificationPreferences",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_NotificationPreferences_BranchId",
                table: "Contact_NotificationPreferences",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_NotificationPreferences_CompanyId",
                table: "Contact_NotificationPreferences",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_NotificationPreferences_NotificationEventTypeId",
                table: "Contact_NotificationPreferences",
                column: "NotificationEventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_NotificationPreferences_PropertyBeneficiaryId",
                table: "Contact_NotificationPreferences",
                column: "PropertyBeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_NotificationPreferences_PropertyOwnerId",
                table: "Contact_NotificationPreferences",
                column: "PropertyOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_NotificationPreferences_PropertyTenantId",
                table: "Contact_NotificationPreferences",
                column: "PropertyTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_NotificationPreferences_VendorId",
                table: "Contact_NotificationPreferences",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notifications_ApplicationUserId",
                table: "Contact_Notifications",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Notifications_NotificationEventTypeId",
                table: "Contact_Notifications",
                column: "NotificationEventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_ApplicationUserId",
                table: "Contact_Reminders",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_MaintenanceTicketId",
                table: "Contact_Reminders",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_PropertyBeneficiaryId",
                table: "Contact_Reminders",
                column: "PropertyBeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_PropertyId",
                table: "Contact_Reminders",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_PropertyOwnerId",
                table: "Contact_Reminders",
                column: "PropertyOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_PropertyTenantId",
                table: "Contact_Reminders",
                column: "PropertyTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_RecurrenceFrequencyId",
                table: "Contact_Reminders",
                column: "RecurrenceFrequencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_ReminderStatusId",
                table: "Contact_Reminders",
                column: "ReminderStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_ReminderTypeId",
                table: "Contact_Reminders",
                column: "ReminderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_Reminders_VendorId",
                table: "Contact_Reminders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomReports_CompanyId",
                table: "CustomReports",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomReports_CreatedBy",
                table: "CustomReports",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CustomReports_UpdatedBy",
                table: "CustomReports",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Data_Properties_BranchId",
                table: "Data_Properties",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_Properties_CommissionTypeId",
                table: "Data_Properties",
                column: "CommissionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_Properties_CompanyId",
                table: "Data_Properties",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_Properties_MainImageId",
                table: "Data_Properties",
                column: "MainImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_Properties_OwnerId",
                table: "Data_Properties",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_Properties_PropertyTypeId",
                table: "Data_Properties",
                column: "PropertyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_Properties_StatusId",
                table: "Data_Properties",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyBeneficiaries_BankAccount_BankNameId",
                table: "Data_PropertyBeneficiaries",
                column: "BankAccount_BankNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyBeneficiaries_BenStatusId",
                table: "Data_PropertyBeneficiaries",
                column: "BenStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyBeneficiaries_BenTypeId",
                table: "Data_PropertyBeneficiaries",
                column: "BenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyBeneficiaries_CommissionTypeId",
                table: "Data_PropertyBeneficiaries",
                column: "CommissionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyBeneficiaries_CompanyId",
                table: "Data_PropertyBeneficiaries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyBeneficiaries_PropertyId",
                table: "Data_PropertyBeneficiaries",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyOwners_BankAccount_BankNameId",
                table: "Data_PropertyOwners",
                column: "BankAccount_BankNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyOwners_CompanyId",
                table: "Data_PropertyOwners",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyOwners_PropertyOwnerTypeId",
                table: "Data_PropertyOwners",
                column: "PropertyOwnerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyOwners_StatusId",
                table: "Data_PropertyOwners",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyTenants_BankAccount_BankNameId",
                table: "Data_PropertyTenants",
                column: "BankAccount_BankNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyTenants_CompanyId",
                table: "Data_PropertyTenants",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyTenants_LeaseDocumentId",
                table: "Data_PropertyTenants",
                column: "LeaseDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyTenants_MoveInInspectionId",
                table: "Data_PropertyTenants",
                column: "MoveInInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyTenants_PropertyId",
                table: "Data_PropertyTenants",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyTenants_StatusId",
                table: "Data_PropertyTenants",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PropertyTenants_TenantTypeId",
                table: "Data_PropertyTenants",
                column: "TenantTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_BeneficiaryPayments_BeneficiaryId",
                table: "Finance_BeneficiaryPayments",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_BeneficiaryPayments_PaymentAllocationId",
                table: "Finance_BeneficiaryPayments",
                column: "PaymentAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_BeneficiaryPayments_StatusId",
                table: "Finance_BeneficiaryPayments",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PaymentAllocations_AllocationTypeId",
                table: "Finance_PaymentAllocations",
                column: "AllocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PaymentAllocations_BeneficiaryId",
                table: "Finance_PaymentAllocations",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PaymentAllocations_PaymentId",
                table: "Finance_PaymentAllocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PaymentRules_CompanyId",
                table: "Finance_PaymentRules",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PaymentRules_RuleTypeId",
                table: "Finance_PaymentRules",
                column: "RuleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PaymentSchedules_FrequencyId",
                table: "Finance_PaymentSchedules",
                column: "FrequencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PaymentSchedules_PropertyId",
                table: "Finance_PaymentSchedules",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PaymentSchedules_TenantId",
                table: "Finance_PaymentSchedules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PropertyPayments_CompanyId",
                table: "Finance_PropertyPayments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PropertyPayments_PaymentMethodId",
                table: "Finance_PropertyPayments",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PropertyPayments_PaymentTypeId",
                table: "Finance_PropertyPayments",
                column: "PaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PropertyPayments_PropertyId",
                table: "Finance_PropertyPayments",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PropertyPayments_ReceiptDocumentId",
                table: "Finance_PropertyPayments",
                column: "ReceiptDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PropertyPayments_StatusId",
                table: "Finance_PropertyPayments",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Finance_PropertyPayments_TenantId",
                table: "Finance_PropertyPayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_InspectionItems_AreaId",
                table: "Inspect_InspectionItems",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_InspectionItems_ConditionId",
                table: "Inspect_InspectionItems",
                column: "ConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_InspectionItems_ImageId",
                table: "Inspect_InspectionItems",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_InspectionItems_InspectionId",
                table: "Inspect_InspectionItems",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_InspectionItems_MaintenancePriorityId",
                table: "Inspect_InspectionItems",
                column: "MaintenancePriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_PropertyInspections_CompanyId",
                table: "Inspect_PropertyInspections",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_PropertyInspections_InspectionTypeId",
                table: "Inspect_PropertyInspections",
                column: "InspectionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_PropertyInspections_OverallConditionId",
                table: "Inspect_PropertyInspections",
                column: "OverallConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_PropertyInspections_PropertyId",
                table: "Inspect_PropertyInspections",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_PropertyInspections_ReportDocumentId",
                table: "Inspect_PropertyInspections",
                column: "ReportDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspect_PropertyInspections_StatusId",
                table: "Inspect_PropertyInspections",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_AllocationTypes_Name",
                table: "Lookup_AllocationTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_BankNameTypes_Name",
                table: "Lookup_BankNameTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_BeneficiaryPaymentStatusTypes_Name",
                table: "Lookup_BeneficiaryPaymentStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_BeneficiaryStatusTypes_Name",
                table: "Lookup_BeneficiaryStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_BeneficiaryTypes_Name",
                table: "Lookup_BeneficiaryTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_BranchStatusTypes_Name",
                table: "Lookup_BranchStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_CommissionTypes_Name",
                table: "Lookup_CommissionTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_CommunicationChannels_Name",
                table: "Lookup_CommunicationChannels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_CommunicationDirections_Name",
                table: "Lookup_CommunicationDirections",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_CompanyStatusTypes_Name",
                table: "Lookup_CompanyStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_ConditionLevels_Name",
                table: "Lookup_ConditionLevels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_ContactNumberTypes_Name",
                table: "Lookup_ContactNumberTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_DocumentAccessLevels_Name",
                table: "Lookup_DocumentAccessLevels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_DocumentCategories_Name",
                table: "Lookup_DocumentCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_DocumentRequirementTypes_Name",
                table: "Lookup_DocumentRequirementTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_DocumentStatuses_Name",
                table: "Lookup_DocumentStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_DocumentTypes_Name",
                table: "Lookup_DocumentTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_EntityDocumentRequirements_CompanyId",
                table: "Lookup_EntityDocumentRequirements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_EntityDocumentRequirements_DocumentRequirementTypeId",
                table: "Lookup_EntityDocumentRequirements",
                column: "DocumentRequirementTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_EntityDocumentRequirements_DocumentTypeId",
                table: "Lookup_EntityDocumentRequirements",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_EntityDocumentRequirements_EntityTypeId_DocumentTypeId_CompanyId",
                table: "Lookup_EntityDocumentRequirements",
                columns: new[] { "EntityTypeId", "DocumentTypeId", "CompanyId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_EntityTypes_Name",
                table: "Lookup_EntityTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_EntityTypes_SystemName",
                table: "Lookup_EntityTypes",
                column: "SystemName",
                unique: true,
                filter: "[SystemName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_ExpenseCategories_Name",
                table: "Lookup_ExpenseCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_InspectionAreas_Name",
                table: "Lookup_InspectionAreas",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_InspectionStatusTypes_Name",
                table: "Lookup_InspectionStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_InspectionTypes_Name",
                table: "Lookup_InspectionTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_MaintenanceCategories_Name",
                table: "Lookup_MaintenanceCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_MaintenanceImageTypes_Name",
                table: "Lookup_MaintenanceImageTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_MaintenancePriorities_Name",
                table: "Lookup_MaintenancePriorities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_MaintenanceStatusTypes_Name",
                table: "Lookup_MaintenanceStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_MediaTypes_Name",
                table: "Lookup_MediaTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_NoteTypes_Name",
                table: "Lookup_NoteTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_NotificationChannels_Name",
                table: "Lookup_NotificationChannels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_NotificationEventTypes_Name",
                table: "Lookup_NotificationEventTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_NotificationEventTypes_SystemName",
                table: "Lookup_NotificationEventTypes",
                column: "SystemName",
                unique: true,
                filter: "[SystemName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_NotificationTemplates_NotificationEventTypeId",
                table: "Lookup_NotificationTemplates",
                column: "NotificationEventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_NotificationTypes_Name",
                table: "Lookup_NotificationTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PaymentFrequencies_Name",
                table: "Lookup_PaymentFrequencies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PaymentMethods_Name",
                table: "Lookup_PaymentMethods",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PaymentRuleTypes_Name",
                table: "Lookup_PaymentRuleTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PaymentStatusTypes_Name",
                table: "Lookup_PaymentStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PaymentTypes_Name",
                table: "Lookup_PaymentTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PermissionCategories_Name",
                table: "Lookup_PermissionCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PropertyImageTypes_Name",
                table: "Lookup_PropertyImageTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PropertyOwnerStatusTypes_Name",
                table: "Lookup_PropertyOwnerStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PropertyOwnerTypes_Name",
                table: "Lookup_PropertyOwnerTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PropertyStatusTypes_Name",
                table: "Lookup_PropertyStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_PropertyTypes_Name",
                table: "Lookup_PropertyTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_RecurrenceFrequencies_Name",
                table: "Lookup_RecurrenceFrequencies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_ReminderStatuses_Name",
                table: "Lookup_ReminderStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_ReminderTypes_Name",
                table: "Lookup_ReminderTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_RoleTypes_Name",
                table: "Lookup_RoleTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_SubscriptionPlans_Name",
                table: "Lookup_SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_TenantStatusTypes_Name",
                table: "Lookup_TenantStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_TenantTypes_Name",
                table: "Lookup_TenantTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_ThemeTypes_Name",
                table: "Lookup_ThemeTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_TwoFactorMethods_Name",
                table: "Lookup_TwoFactorMethods",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lookup_UserStatusTypes_Name",
                table: "Lookup_UserStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceComments_MaintenanceTicketId",
                table: "Maint_MaintenanceComments",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceExpenses_CategoryId",
                table: "Maint_MaintenanceExpenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceExpenses_MaintenanceTicketId",
                table: "Maint_MaintenanceExpenses",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceExpenses_ReceiptDocumentId",
                table: "Maint_MaintenanceExpenses",
                column: "ReceiptDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceExpenses_VendorId",
                table: "Maint_MaintenanceExpenses",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceTickets_CategoryId",
                table: "Maint_MaintenanceTickets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceTickets_CompanyId",
                table: "Maint_MaintenanceTickets",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceTickets_InspectionId",
                table: "Maint_MaintenanceTickets",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceTickets_PriorityId",
                table: "Maint_MaintenanceTickets",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceTickets_PropertyId",
                table: "Maint_MaintenanceTickets",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceTickets_StatusId",
                table: "Maint_MaintenanceTickets",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceTickets_TenantId",
                table: "Maint_MaintenanceTickets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_MaintenanceTickets_VendorId",
                table: "Maint_MaintenanceTickets",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_Vendors_BankAccount_BankNameId",
                table: "Maint_Vendors",
                column: "BankAccount_BankNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Maint_Vendors_CompanyId",
                table: "Maint_Vendors",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDashboards_CompanyId",
                table: "ReportDashboards",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDashboards_CreatedBy",
                table: "ReportDashboards",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDashboards_UserId",
                table: "ReportDashboards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDashboardWidgets_CustomReportId",
                table: "ReportDashboardWidgets",
                column: "CustomReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDashboardWidgets_DashboardId",
                table: "ReportDashboardWidgets",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutionLogs_CompanyId",
                table: "ReportExecutionLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutionLogs_ExecutedBy",
                table: "ReportExecutionLogs",
                column: "ExecutedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutionLogs_ReportScheduleId",
                table: "ReportExecutionLogs",
                column: "ReportScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_CompanyId",
                table: "ReportSchedules",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_CreatedBy",
                table: "ReportSchedules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_CustomReportId",
                table: "ReportSchedules",
                column: "CustomReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_FrequencyTypeId",
                table: "ReportSchedules",
                column: "FrequencyTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetRolePermissions");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserPermissionOverrides");

            migrationBuilder.DropTable(
                name: "AspNetUserRoleAssignments");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CDN_AccessLogs");

            migrationBuilder.DropTable(
                name: "CDN_Base64Storage");

            migrationBuilder.DropTable(
                name: "CDN_Configurations");

            migrationBuilder.DropTable(
                name: "CDN_UsageStatistics");

            migrationBuilder.DropTable(
                name: "Contact_Communications");

            migrationBuilder.DropTable(
                name: "Contact_ContactNumbers");

            migrationBuilder.DropTable(
                name: "Contact_Emails");

            migrationBuilder.DropTable(
                name: "Contact_EntityDocuments");

            migrationBuilder.DropTable(
                name: "Contact_Media");

            migrationBuilder.DropTable(
                name: "Contact_Notes");

            migrationBuilder.DropTable(
                name: "Contact_NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Contact_Notifications");

            migrationBuilder.DropTable(
                name: "Contact_Reminders");

            migrationBuilder.DropTable(
                name: "Finance_BeneficiaryPayments");

            migrationBuilder.DropTable(
                name: "Finance_PaymentRules");

            migrationBuilder.DropTable(
                name: "Finance_PaymentSchedules");

            migrationBuilder.DropTable(
                name: "Inspect_InspectionItems");

            migrationBuilder.DropTable(
                name: "Lookup_DocumentAccessLevels");

            migrationBuilder.DropTable(
                name: "Lookup_DocumentCategories");

            migrationBuilder.DropTable(
                name: "Lookup_EntityDocumentRequirements");

            migrationBuilder.DropTable(
                name: "Lookup_MaintenanceImageTypes");

            migrationBuilder.DropTable(
                name: "Lookup_NotificationChannels");

            migrationBuilder.DropTable(
                name: "Lookup_NotificationTemplates");

            migrationBuilder.DropTable(
                name: "Lookup_NotificationTypes");

            migrationBuilder.DropTable(
                name: "Lookup_PermissionCategories");

            migrationBuilder.DropTable(
                name: "Lookup_PropertyImageTypes");

            migrationBuilder.DropTable(
                name: "Lookup_RoleTypes");

            migrationBuilder.DropTable(
                name: "Lookup_ThemeTypes");

            migrationBuilder.DropTable(
                name: "Lookup_TwoFactorMethods");

            migrationBuilder.DropTable(
                name: "Maint_MaintenanceComments");

            migrationBuilder.DropTable(
                name: "Maint_MaintenanceExpenses");

            migrationBuilder.DropTable(
                name: "ReportDashboardWidgets");

            migrationBuilder.DropTable(
                name: "ReportExecutionLogs");

            migrationBuilder.DropTable(
                name: "AspNetPermissions");

            migrationBuilder.DropTable(
                name: "AspNetCustomRoles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Lookup_CommunicationChannels");

            migrationBuilder.DropTable(
                name: "Lookup_CommunicationDirections");

            migrationBuilder.DropTable(
                name: "Lookup_ContactNumberTypes");

            migrationBuilder.DropTable(
                name: "Lookup_DocumentStatuses");

            migrationBuilder.DropTable(
                name: "Lookup_MediaTypes");

            migrationBuilder.DropTable(
                name: "Lookup_NoteTypes");

            migrationBuilder.DropTable(
                name: "Lookup_RecurrenceFrequencies");

            migrationBuilder.DropTable(
                name: "Lookup_ReminderStatuses");

            migrationBuilder.DropTable(
                name: "Lookup_ReminderTypes");

            migrationBuilder.DropTable(
                name: "Finance_PaymentAllocations");

            migrationBuilder.DropTable(
                name: "Lookup_BeneficiaryPaymentStatusTypes");

            migrationBuilder.DropTable(
                name: "Lookup_PaymentRuleTypes");

            migrationBuilder.DropTable(
                name: "Lookup_PaymentFrequencies");

            migrationBuilder.DropTable(
                name: "Lookup_InspectionAreas");

            migrationBuilder.DropTable(
                name: "Lookup_DocumentRequirementTypes");

            migrationBuilder.DropTable(
                name: "Lookup_DocumentTypes");

            migrationBuilder.DropTable(
                name: "Lookup_EntityTypes");

            migrationBuilder.DropTable(
                name: "Lookup_NotificationEventTypes");

            migrationBuilder.DropTable(
                name: "Lookup_ExpenseCategories");

            migrationBuilder.DropTable(
                name: "Maint_MaintenanceTickets");

            migrationBuilder.DropTable(
                name: "ReportDashboards");

            migrationBuilder.DropTable(
                name: "ReportSchedules");

            migrationBuilder.DropTable(
                name: "Data_PropertyBeneficiaries");

            migrationBuilder.DropTable(
                name: "Finance_PropertyPayments");

            migrationBuilder.DropTable(
                name: "Lookup_AllocationTypes");

            migrationBuilder.DropTable(
                name: "Lookup_MaintenanceCategories");

            migrationBuilder.DropTable(
                name: "Lookup_MaintenancePriorities");

            migrationBuilder.DropTable(
                name: "Lookup_MaintenanceStatusTypes");

            migrationBuilder.DropTable(
                name: "Maint_Vendors");

            migrationBuilder.DropTable(
                name: "CustomReports");

            migrationBuilder.DropTable(
                name: "ReportFrequencyTypes");

            migrationBuilder.DropTable(
                name: "Lookup_BeneficiaryStatusTypes");

            migrationBuilder.DropTable(
                name: "Lookup_BeneficiaryTypes");

            migrationBuilder.DropTable(
                name: "Data_PropertyTenants");

            migrationBuilder.DropTable(
                name: "Lookup_PaymentMethods");

            migrationBuilder.DropTable(
                name: "Lookup_PaymentStatusTypes");

            migrationBuilder.DropTable(
                name: "Lookup_PaymentTypes");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Inspect_PropertyInspections");

            migrationBuilder.DropTable(
                name: "Lookup_TenantStatusTypes");

            migrationBuilder.DropTable(
                name: "Lookup_TenantTypes");

            migrationBuilder.DropTable(
                name: "Lookup_UserStatusTypes");

            migrationBuilder.DropTable(
                name: "Data_Properties");

            migrationBuilder.DropTable(
                name: "Lookup_ConditionLevels");

            migrationBuilder.DropTable(
                name: "Lookup_InspectionStatusTypes");

            migrationBuilder.DropTable(
                name: "Lookup_InspectionTypes");

            migrationBuilder.DropTable(
                name: "AspNetBranches");

            migrationBuilder.DropTable(
                name: "Data_PropertyOwners");

            migrationBuilder.DropTable(
                name: "Lookup_CommissionTypes");

            migrationBuilder.DropTable(
                name: "Lookup_PropertyStatusTypes");

            migrationBuilder.DropTable(
                name: "Lookup_PropertyTypes");

            migrationBuilder.DropTable(
                name: "Lookup_BranchStatusTypes");

            migrationBuilder.DropTable(
                name: "AspNetCompanies");

            migrationBuilder.DropTable(
                name: "Lookup_PropertyOwnerStatusTypes");

            migrationBuilder.DropTable(
                name: "Lookup_PropertyOwnerTypes");

            migrationBuilder.DropTable(
                name: "CDN_FileMetadata");

            migrationBuilder.DropTable(
                name: "Lookup_BankNameTypes");

            migrationBuilder.DropTable(
                name: "Lookup_CompanyStatusTypes");

            migrationBuilder.DropTable(
                name: "Lookup_SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "CDN_Folders");

            migrationBuilder.DropTable(
                name: "CDN_Categories");
        }
    }
}
