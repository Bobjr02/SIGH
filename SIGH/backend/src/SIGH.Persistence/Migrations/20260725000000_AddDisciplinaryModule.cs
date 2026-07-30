using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGH.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDisciplinaryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    table.ForeignKey(
                        name: "FK_DisciplinaryCases_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
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
                    IsPrimarySubject = table.Column<bool>(type: "bit", nullable: false),
                    Statement = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StatementRecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                name: "DisciplinaryDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisciplinaryCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionType = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Reasoning = table.Column<string>(type: "nvarchar(8000)", maxLength: 8000, nullable: false),
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

            // Indexes for InfractionTypes
            migrationBuilder.CreateIndex(name: "IX_InfractionTypes_DefaultSeverity", table: "InfractionTypes", column: "DefaultSeverity");
            migrationBuilder.CreateIndex(name: "IX_InfractionTypes_IsActive", table: "InfractionTypes", column: "IsActive");
            migrationBuilder.CreateIndex(name: "IX_InfractionTypes_IsDeleted", table: "InfractionTypes", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_InfractionTypes_Name", table: "InfractionTypes", column: "Name");
            migrationBuilder.CreateIndex(name: "UX_InfractionTypes_Code", table: "InfractionTypes", column: "Code", unique: true, filter: "[IsDeleted] = 0");

            // Indexes for DisciplinaryCases
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_CompanyId", table: "DisciplinaryCases", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_CompanyId_IsDeleted", table: "DisciplinaryCases", columns: new[] { "CompanyId", "IsDeleted" });
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_CompanyId_OpenedAt", table: "DisciplinaryCases", columns: new[] { "CompanyId", "OpenedAt" });
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_CompanyId_Status", table: "DisciplinaryCases", columns: new[] { "CompanyId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_DueDate", table: "DisciplinaryCases", column: "DueDate");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_IsDeleted", table: "DisciplinaryCases", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_OpenedAt", table: "DisciplinaryCases", column: "OpenedAt");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_Priority", table: "DisciplinaryCases", column: "Priority");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_ResponsibleEmployeeId", table: "DisciplinaryCases", column: "ResponsibleEmployeeId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCases_Status", table: "DisciplinaryCases", column: "Status");
            migrationBuilder.CreateIndex(name: "UX_DisciplinaryCases_CompanyId_CaseNumber", table: "DisciplinaryCases", columns: new[] { "CompanyId", "CaseNumber" }, unique: true, filter: "[IsDeleted] = 0");

            // Indexes for DisciplinaryCaseEmployees
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCaseEmployees_DisciplinaryCaseId", table: "DisciplinaryCaseEmployees", column: "DisciplinaryCaseId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCaseEmployees_EmployeeId", table: "DisciplinaryCaseEmployees", column: "EmployeeId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCaseEmployees_IsDeleted", table: "DisciplinaryCaseEmployees", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCaseEmployees_IsPrimarySubject", table: "DisciplinaryCaseEmployees", column: "IsPrimarySubject");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryCaseEmployees_Role", table: "DisciplinaryCaseEmployees", column: "Role");
            migrationBuilder.CreateIndex(name: "UX_DisciplinaryCaseEmployees_Case_Emp_Role", table: "DisciplinaryCaseEmployees", columns: new[] { "DisciplinaryCaseId", "EmployeeId", "Role" }, unique: true, filter: "[IsDeleted] = 0");

            // Indexes for DisciplinaryDecisions
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryDecisions_ApprovedAt", table: "DisciplinaryDecisions", column: "ApprovedAt");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryDecisions_DecidedAt", table: "DisciplinaryDecisions", column: "DecidedAt");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryDecisions_DecisionType", table: "DisciplinaryDecisions", column: "DecisionType");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryDecisions_DisciplinaryCaseId", table: "DisciplinaryDecisions", column: "DisciplinaryCaseId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryDecisions_DisciplinaryCaseId_Status", table: "DisciplinaryDecisions", columns: new[] { "DisciplinaryCaseId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryDecisions_IsDeleted", table: "DisciplinaryDecisions", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryDecisions_Status", table: "DisciplinaryDecisions", column: "Status");

            // Indexes for DisciplinaryEvidences
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryEvidences_CollectedAt", table: "DisciplinaryEvidences", column: "CollectedAt");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryEvidences_DisciplinaryCaseId", table: "DisciplinaryEvidences", column: "DisciplinaryCaseId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryEvidences_DisciplinaryCaseId_Status", table: "DisciplinaryEvidences", columns: new[] { "DisciplinaryCaseId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryEvidences_DisciplinaryOccurrenceId", table: "DisciplinaryEvidences", column: "DisciplinaryOccurrenceId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryEvidences_EvidenceType", table: "DisciplinaryEvidences", column: "EvidenceType");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryEvidences_IsDeleted", table: "DisciplinaryEvidences", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryEvidences_Status", table: "DisciplinaryEvidences", column: "Status");

            // Indexes for DisciplinaryOccurrences
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_ConfidentialityLevel", table: "DisciplinaryOccurrences", column: "ConfidentialityLevel");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_DisciplinaryCaseId", table: "DisciplinaryOccurrences", column: "DisciplinaryCaseId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_DisciplinaryCaseId_OccurrenceDate", table: "DisciplinaryOccurrences", columns: new[] { "DisciplinaryCaseId", "OccurrenceDate" });
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_DisciplinaryCaseId_Status", table: "DisciplinaryOccurrences", columns: new[] { "DisciplinaryCaseId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_InfractionTypeId", table: "DisciplinaryOccurrences", column: "InfractionTypeId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_IsDeleted", table: "DisciplinaryOccurrences", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_OccurrenceDate", table: "DisciplinaryOccurrences", column: "OccurrenceDate");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_ReportedAt", table: "DisciplinaryOccurrences", column: "ReportedAt");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_Severity", table: "DisciplinaryOccurrences", column: "Severity");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryOccurrences_Status", table: "DisciplinaryOccurrences", column: "Status");

            // Indexes for DisciplinaryMeasures
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryMeasures_AppliedAt", table: "DisciplinaryMeasures", column: "AppliedAt");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryMeasures_DisciplinaryCaseId", table: "DisciplinaryMeasures", column: "DisciplinaryCaseId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryMeasures_DisciplinaryCaseId_Status", table: "DisciplinaryMeasures", columns: new[] { "DisciplinaryCaseId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryMeasures_DisciplinaryDecisionId", table: "DisciplinaryMeasures", column: "DisciplinaryDecisionId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryMeasures_DisciplinaryDecisionId_Status", table: "DisciplinaryMeasures", columns: new[] { "DisciplinaryDecisionId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryMeasures_EmployeeId", table: "DisciplinaryMeasures", column: "EmployeeId");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryMeasures_IsDeleted", table: "DisciplinaryMeasures", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryMeasures_MeasureType", table: "DisciplinaryMeasures", column: "MeasureType");
            migrationBuilder.CreateIndex(name: "IX_DisciplinaryMeasures_Status", table: "DisciplinaryMeasures", column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DisciplinaryCaseEmployees");
            migrationBuilder.DropTable(name: "DisciplinaryEvidences");
            migrationBuilder.DropTable(name: "DisciplinaryMeasures");
            migrationBuilder.DropTable(name: "DisciplinaryOccurrences");
            migrationBuilder.DropTable(name: "DisciplinaryDecisions");
            migrationBuilder.DropTable(name: "DisciplinaryCases");
            migrationBuilder.DropTable(name: "InfractionTypes");
        }
    }
}
