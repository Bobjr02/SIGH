using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGH.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisciplinaryCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConclusionSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinaryCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InfractionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DefaultSeverity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequiresFormalInvestigation = table.Column<bool>(type: "bit", nullable: false),
                    AllowsTerminationRecommendation = table.Column<bool>(type: "bit", nullable: false),
                    LegalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfractionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEntity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RemindersSentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EscalationLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Info"),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsExpired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTitles_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManagementUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ParentManagementUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagementUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagementUnits_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagementUnits_ManagementUnits_ParentManagementUnitId",
                        column: x => x.ParentManagementUnitId,
                        principalTable: "ManagementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisciplinaryDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisciplinaryCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionType = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Reasoning = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinaryDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisciplinaryDecisions_DisciplinaryCases_DisciplinaryCaseId",
                        column: x => x.DisciplinaryCaseId,
                        principalTable: "DisciplinaryCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisciplinaryOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisciplinaryCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InfractionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConfidentialityLevel = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinaryOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisciplinaryOccurrences_DisciplinaryCases_DisciplinaryCaseId",
                        column: x => x.DisciplinaryCaseId,
                        principalTable: "DisciplinaryCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisciplinaryOccurrences_InfractionTypes_InfractionTypeId",
                        column: x => x.InfractionTypeId,
                        principalTable: "InfractionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Entity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReasonRevoked = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastActivity = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagementUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departments_ManagementUnits_ManagementUnitId",
                        column: x => x.ManagementUnitId,
                        principalTable: "ManagementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisciplinaryEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisciplinaryCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisciplinaryOccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EvidenceType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StorageReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    CollectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CollectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrityHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinaryEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisciplinaryEvidences_DisciplinaryCases_DisciplinaryCaseId",
                        column: x => x.DisciplinaryCaseId,
                        principalTable: "DisciplinaryCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisciplinaryEvidences_DisciplinaryOccurrences_DisciplinaryOccurrenceId",
                        column: x => x.DisciplinaryOccurrenceId,
                        principalTable: "DisciplinaryOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SocialName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    AdmissionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobTitleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagementUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupervisorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorporateEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PersonalEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TerminationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TerminationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Employees_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_ManagementUnits_ManagementUnitId",
                        column: x => x.ManagementUnitId,
                        principalTable: "ManagementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisciplinaryCaseEmployees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisciplinaryCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Statement = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StatementRecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsPrimarySubject = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinaryCaseEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisciplinaryCaseEmployees_DisciplinaryCases_DisciplinaryCaseId",
                        column: x => x.DisciplinaryCaseId,
                        principalTable: "DisciplinaryCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisciplinaryCaseEmployees_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisciplinaryMeasures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisciplinaryCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisciplinaryDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasureType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinaryMeasures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisciplinaryMeasures_DisciplinaryCases_DisciplinaryCaseId",
                        column: x => x.DisciplinaryCaseId,
                        principalTable: "DisciplinaryCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisciplinaryMeasures_DisciplinaryDecisions_DisciplinaryDecisionId",
                        column: x => x.DisciplinaryDecisionId,
                        principalTable: "DisciplinaryDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisciplinaryMeasures_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_RegistrationNumber",
                table: "Companies",
                column: "RegistrationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId",
                table: "Departments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId_Name",
                table: "Departments",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagementUnitId",
                table: "Departments",
                column: "ManagementUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagementUnitId_Name",
                table: "Departments",
                columns: new[] { "ManagementUnitId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCaseEmployees_DisciplinaryCaseId",
                table: "DisciplinaryCaseEmployees",
                column: "DisciplinaryCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCaseEmployees_EmployeeId",
                table: "DisciplinaryCaseEmployees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCaseEmployees_IsDeleted",
                table: "DisciplinaryCaseEmployees",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCaseEmployees_IsPrimarySubject",
                table: "DisciplinaryCaseEmployees",
                column: "IsPrimarySubject");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCaseEmployees_Role",
                table: "DisciplinaryCaseEmployees",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "UX_DisciplinaryCaseEmployees_Case_Employee_Role",
                table: "DisciplinaryCaseEmployees",
                columns: new[] { "DisciplinaryCaseId", "EmployeeId", "Role" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_CompanyId",
                table: "DisciplinaryCases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_CompanyId_IsDeleted",
                table: "DisciplinaryCases",
                columns: new[] { "CompanyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_CompanyId_OpenedAt",
                table: "DisciplinaryCases",
                columns: new[] { "CompanyId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_CompanyId_Status",
                table: "DisciplinaryCases",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_DueDate",
                table: "DisciplinaryCases",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_IsDeleted",
                table: "DisciplinaryCases",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_OpenedAt",
                table: "DisciplinaryCases",
                column: "OpenedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_Priority",
                table: "DisciplinaryCases",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_ResponsibleEmployeeId",
                table: "DisciplinaryCases",
                column: "ResponsibleEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_Status",
                table: "DisciplinaryCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_DisciplinaryCases_Company_CaseNumber",
                table: "DisciplinaryCases",
                columns: new[] { "CompanyId", "CaseNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryDecisions_ApprovedAt",
                table: "DisciplinaryDecisions",
                column: "ApprovedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryDecisions_DecidedAt",
                table: "DisciplinaryDecisions",
                column: "DecidedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryDecisions_DecisionType",
                table: "DisciplinaryDecisions",
                column: "DecisionType");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryDecisions_DisciplinaryCaseId",
                table: "DisciplinaryDecisions",
                column: "DisciplinaryCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryDecisions_DisciplinaryCaseId_Status",
                table: "DisciplinaryDecisions",
                columns: new[] { "DisciplinaryCaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryDecisions_IsDeleted",
                table: "DisciplinaryDecisions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryDecisions_Status",
                table: "DisciplinaryDecisions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryEvidences_CollectedAt",
                table: "DisciplinaryEvidences",
                column: "CollectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryEvidences_DisciplinaryCaseId",
                table: "DisciplinaryEvidences",
                column: "DisciplinaryCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryEvidences_DisciplinaryCaseId_Status",
                table: "DisciplinaryEvidences",
                columns: new[] { "DisciplinaryCaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryEvidences_DisciplinaryOccurrenceId",
                table: "DisciplinaryEvidences",
                column: "DisciplinaryOccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryEvidences_EvidenceType",
                table: "DisciplinaryEvidences",
                column: "EvidenceType");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryEvidences_IsDeleted",
                table: "DisciplinaryEvidences",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryEvidences_Status",
                table: "DisciplinaryEvidences",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryMeasures_AppliedAt",
                table: "DisciplinaryMeasures",
                column: "AppliedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryMeasures_DisciplinaryCaseId",
                table: "DisciplinaryMeasures",
                column: "DisciplinaryCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryMeasures_DisciplinaryCaseId_Status",
                table: "DisciplinaryMeasures",
                columns: new[] { "DisciplinaryCaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryMeasures_DisciplinaryDecisionId",
                table: "DisciplinaryMeasures",
                column: "DisciplinaryDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryMeasures_DisciplinaryDecisionId_Status",
                table: "DisciplinaryMeasures",
                columns: new[] { "DisciplinaryDecisionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryMeasures_EmployeeId",
                table: "DisciplinaryMeasures",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryMeasures_IsDeleted",
                table: "DisciplinaryMeasures",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryMeasures_MeasureType",
                table: "DisciplinaryMeasures",
                column: "MeasureType");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryMeasures_Status",
                table: "DisciplinaryMeasures",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_ConfidentialityLevel",
                table: "DisciplinaryOccurrences",
                column: "ConfidentialityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_DisciplinaryCaseId",
                table: "DisciplinaryOccurrences",
                column: "DisciplinaryCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_DisciplinaryCaseId_OccurrenceDate",
                table: "DisciplinaryOccurrences",
                columns: new[] { "DisciplinaryCaseId", "OccurrenceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_DisciplinaryCaseId_Status",
                table: "DisciplinaryOccurrences",
                columns: new[] { "DisciplinaryCaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_InfractionTypeId",
                table: "DisciplinaryOccurrences",
                column: "InfractionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_IsDeleted",
                table: "DisciplinaryOccurrences",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_OccurrenceDate",
                table: "DisciplinaryOccurrences",
                column: "OccurrenceDate");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_ReportedAt",
                table: "DisciplinaryOccurrences",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_Severity",
                table: "DisciplinaryOccurrences",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryOccurrences_Status",
                table: "DisciplinaryOccurrences",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_AdmissionDate",
                table: "Employees",
                column: "AdmissionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId",
                table: "Employees",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_EmployeeNumber",
                table: "Employees",
                columns: new[] { "CompanyId", "EmployeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_FullName",
                table: "Employees",
                columns: new[] { "CompanyId", "FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_Status",
                table: "Employees",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CorporateEmail",
                table: "Employees",
                column: "CorporateEmail",
                unique: true,
                filter: "[CorporateEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Cpf",
                table: "Employees",
                column: "Cpf",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FullName",
                table: "Employees",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_JobTitleId",
                table: "Employees",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ManagementUnitId",
                table: "Employees",
                column: "ManagementUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ManagementUnitId_Status",
                table: "Employees",
                columns: new[] { "ManagementUnitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SocialName",
                table: "Employees",
                column: "SocialName");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Status",
                table: "Employees",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SupervisorId",
                table: "Employees",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InfractionTypes_DefaultSeverity",
                table: "InfractionTypes",
                column: "DefaultSeverity");

            migrationBuilder.CreateIndex(
                name: "IX_InfractionTypes_IsActive",
                table: "InfractionTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InfractionTypes_IsDeleted",
                table: "InfractionTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InfractionTypes_Name",
                table: "InfractionTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "UX_InfractionTypes_Code",
                table: "InfractionTypes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_CompanyId",
                table: "JobTitles",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_CompanyId_Code",
                table: "JobTitles",
                columns: new[] { "CompanyId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_CompanyId_Name",
                table: "JobTitles",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementUnits_CompanyId",
                table: "ManagementUnits",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementUnits_CompanyId_Code",
                table: "ManagementUnits",
                columns: new[] { "CompanyId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementUnits_CompanyId_Name",
                table: "ManagementUnits",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementUnits_ParentManagementUnitId",
                table: "ManagementUnits",
                column: "ParentManagementUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_CompanyId_LastSentAt",
                table: "NotificationLogs",
                columns: new[] { "CompanyId", "LastSentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_CompanyId_SourceEntity_SourceEntityId_RecipientUserId_EventType",
                table: "NotificationLogs",
                columns: new[] { "CompanyId", "SourceEntity", "SourceEntityId", "RecipientUserId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CompanyId_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "CompanyId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CompanyId_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "CompanyId", "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId",
                table: "PasswordHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Cpf",
                table: "Users",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DisciplinaryCaseEmployees");

            migrationBuilder.DropTable(
                name: "DisciplinaryEvidences");

            migrationBuilder.DropTable(
                name: "DisciplinaryMeasures");

            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PasswordHistories");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "DisciplinaryOccurrences");

            migrationBuilder.DropTable(
                name: "DisciplinaryDecisions");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "InfractionTypes");

            migrationBuilder.DropTable(
                name: "DisciplinaryCases");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "JobTitles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ManagementUnits");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
