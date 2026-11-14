using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGH.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HistoryImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "DisciplinaryDecisionId",
                table: "DisciplinaryMeasures",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "SuspensionDays",
                table: "DisciplinaryMeasures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HistoryImportBatchId",
                table: "DisciplinaryCases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoryKey",
                table: "DisciplinaryCases",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HistoryUpdatedByBatchId",
                table: "DisciplinaryCases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DisciplinaryHistoryImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileSha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PreviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ImportedCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedCount = table.Column<int>(type: "int", nullable: false),
                    ExistingCount = table.Column<int>(type: "int", nullable: false),
                    RejectedCount = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    NewInfractionTypeCount = table.Column<int>(type: "int", nullable: false),
                    AbsentCount = table.Column<int>(type: "int", nullable: false),
                    PlanFingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    IssuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinaryHistoryImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoryImportStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_HistoryImportStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_HistoryImportBatchId",
                table: "DisciplinaryCases",
                column: "HistoryImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_HistoryKey",
                table: "DisciplinaryCases",
                column: "HistoryKey",
                unique: true,
                filter: "[HistoryKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryHistoryImportBatches_FileSha256",
                table: "DisciplinaryHistoryImportBatches",
                column: "FileSha256");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryHistoryImportBatches_PreviewedAt",
                table: "DisciplinaryHistoryImportBatches",
                column: "PreviewedAt");
        
            // Suspensões já aplicadas no SIGH: dias = período (os dois extremos), no fuso da usina (UTC-3).
            migrationBuilder.Sql(@"
UPDATE [DisciplinaryMeasures]
SET [SuspensionDays] = DATEDIFF(day, CAST(SWITCHOFFSET([EffectiveFrom], '-03:00') AS date), CAST(SWITCHOFFSET([EffectiveUntil], '-03:00') AS date)) + 1
WHERE [MeasureType] = 4 AND [EffectiveUntil] IS NOT NULL AND [SuspensionDays] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisciplinaryHistoryImportBatches");

            migrationBuilder.DropTable(
                name: "HistoryImportStates");

            migrationBuilder.DropIndex(
                name: "IX_DisciplinaryCases_HistoryImportBatchId",
                table: "DisciplinaryCases");

            migrationBuilder.DropIndex(
                name: "IX_DisciplinaryCases_HistoryKey",
                table: "DisciplinaryCases");

            migrationBuilder.DropColumn(
                name: "SuspensionDays",
                table: "DisciplinaryMeasures");

            migrationBuilder.DropColumn(
                name: "HistoryImportBatchId",
                table: "DisciplinaryCases");

            migrationBuilder.DropColumn(
                name: "HistoryKey",
                table: "DisciplinaryCases");

            migrationBuilder.DropColumn(
                name: "HistoryUpdatedByBatchId",
                table: "DisciplinaryCases");

            migrationBuilder.AlterColumn<Guid>(
                name: "DisciplinaryDecisionId",
                table: "DisciplinaryMeasures",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
