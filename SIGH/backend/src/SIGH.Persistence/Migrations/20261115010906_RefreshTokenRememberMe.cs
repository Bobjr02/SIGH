using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGH.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenRememberMe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tokens já emitidos continuam persistentes (comportamento anterior ao "Lembrar-me").
            // O default fica só na migration: no modelo, um HasDefaultValue(true) faria o EF ignorar o false.
            migrationBuilder.AddColumn<bool>(
                name: "IsPersistent",
                table: "RefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPersistent",
                table: "RefreshTokens");
        }
    }
}
