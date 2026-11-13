using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGH.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmployeesImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_Cpf",
                table: "Employees");

            migrationBuilder.AddColumn<int>(
                name: "Group",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Team",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileSha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    CreatedCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedCount = table.Column<int>(type: "int", nullable: false),
                    TerminatedCount = table.Column<int>(type: "int", nullable: false),
                    TerminatedByAbsenceCount = table.Column<int>(type: "int", nullable: false),
                    ReactivatedCount = table.Column<int>(type: "int", nullable: false),
                    UnchangedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    MassTerminationConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PlanFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IssuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeImportBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Cpf",
                table: "Employees",
                column: "Cpf",
                unique: true,
                filter: "[IsDeleted] = 0 AND [Status] <> 5");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportBatches_FileSha256",
                table: "EmployeeImportBatches",
                column: "FileSha256");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportBatches_PreviewedAt",
                table: "EmployeeImportBatches",
                column: "PreviewedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Cpf",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Team",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Cpf",
                table: "Employees",
                column: "Cpf",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
