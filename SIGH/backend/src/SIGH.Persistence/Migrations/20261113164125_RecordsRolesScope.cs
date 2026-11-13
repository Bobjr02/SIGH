using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGH.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecordsRolesScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles");

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidFrom",
                table: "UserRoles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidUntil",
                table: "UserRoles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DisciplinaryCaseId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "MeasureDate",
                table: "DisciplinaryDecisions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedMeasureType",
                table: "DisciplinaryDecisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectedAt",
                table: "DisciplinaryDecisions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedByUserId",
                table: "DisciplinaryDecisions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "DisciplinaryDecisions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SuspensionEnd",
                table: "DisciplinaryDecisions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SuspensionStart",
                table: "DisciplinaryDecisions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "DisciplinaryCases",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagementUnitIdAtTime",
                table: "DisciplinaryCaseEmployees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserManagementUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagementUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValidUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_UserManagementUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserManagementUnits_ManagementUnits_ManagementUnitId",
                        column: x => x.ManagementUnitId,
                        principalTable: "ManagementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserManagementUnits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DisciplinaryCaseId",
                table: "Notifications",
                column: "DisciplinaryCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryDecisions_ProposedMeasureType_Status",
                table: "DisciplinaryDecisions",
                columns: new[] { "ProposedMeasureType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCases_Kind_Status",
                table: "DisciplinaryCases",
                columns: new[] { "Kind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryCaseEmployees_ManagementUnitIdAtTime",
                table: "DisciplinaryCaseEmployees",
                column: "ManagementUnitIdAtTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserManagementUnits_ManagementUnitId",
                table: "UserManagementUnits",
                column: "ManagementUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UserManagementUnits_UserId_ManagementUnitId",
                table: "UserManagementUnits",
                columns: new[] { "UserId", "ManagementUnitId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            // Usuários existentes: stamp aleatório (tokens emitidos antes desta versão passam a receber 401
            // e o front renova a sessão pelo refresh token).
            migrationBuilder.Sql("UPDATE [Users] SET [SecurityStamp] = REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '') WHERE [SecurityStamp] = '';");

            // Dados existentes: o perfil Administrator deixa de ter TODAS as permissões. Fica só com a
            // administração (usuários, catálogos, auditoria, consulta e relatórios); perde as do processo em
            // etapas e qualquer outra fora da lista. Exclusão lógica: a história fica. O DatabaseSeeder
            // aplica a mesma lista (SystemRoles) e acrescenta as permissões novas deste sprint.
            migrationBuilder.Sql(@"
UPDATE rp SET rp.[IsDeleted] = 1, rp.[DeletedAt] = SYSDATETIMEOFFSET()
FROM [RolePermissions] rp
JOIN [Roles] r ON r.[Id] = rp.[RoleId]
JOIN [Permissions] p ON p.[Id] = rp.[PermissionId]
WHERE r.[Name] = 'Administrator' AND rp.[IsDeleted] = 0
  AND p.[Code] NOT IN (
    'Users.View', 'Users.Create', 'Users.ChangeStatus', 'Users.Unlock',
    'Employees.View', 'Employees.Import',
    'Disciplinary.InfractionTypes.View', 'Disciplinary.InfractionTypes.Create', 'Disciplinary.InfractionTypes.Update',
    'Disciplinary.InfractionTypes.Activate', 'Disciplinary.InfractionTypes.Deactivate',
    'Audit.View',
    'Disciplinary.Reports.ViewDashboard', 'Disciplinary.Reports.ViewCases', 'Disciplinary.Reports.ViewMeasures',
    'Disciplinary.Reports.ViewEmployeeHistory', 'Disciplinary.Reports.Export',
    'Notifications.View', 'Notifications.Read');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserManagementUnits");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_DisciplinaryCaseId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_DisciplinaryDecisions_ProposedMeasureType_Status",
                table: "DisciplinaryDecisions");

            migrationBuilder.DropIndex(
                name: "IX_DisciplinaryCases_Kind_Status",
                table: "DisciplinaryCases");

            migrationBuilder.DropIndex(
                name: "IX_DisciplinaryCaseEmployees_ManagementUnitIdAtTime",
                table: "DisciplinaryCaseEmployees");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "DisciplinaryCaseId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "MeasureDate",
                table: "DisciplinaryDecisions");

            migrationBuilder.DropColumn(
                name: "ProposedMeasureType",
                table: "DisciplinaryDecisions");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "DisciplinaryDecisions");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "DisciplinaryDecisions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "DisciplinaryDecisions");

            migrationBuilder.DropColumn(
                name: "SuspensionEnd",
                table: "DisciplinaryDecisions");

            migrationBuilder.DropColumn(
                name: "SuspensionStart",
                table: "DisciplinaryDecisions");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "DisciplinaryCases");

            migrationBuilder.DropColumn(
                name: "ManagementUnitIdAtTime",
                table: "DisciplinaryCaseEmployees");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);
        }
    }
}
