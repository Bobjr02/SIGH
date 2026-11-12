using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGH.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EvidenceFileUploadAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FileUploadedAt",
                table: "DisciplinaryEvidences",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FileUploadedByUserId",
                table: "DisciplinaryEvidences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemovalReason",
                table: "DisciplinaryEvidences",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileUploadedAt",
                table: "DisciplinaryEvidences");

            migrationBuilder.DropColumn(
                name: "FileUploadedByUserId",
                table: "DisciplinaryEvidences");

            migrationBuilder.DropColumn(
                name: "RemovalReason",
                table: "DisciplinaryEvidences");
        }
    }
}
