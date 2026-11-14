using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGH.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkLegacyNotificationsToCases : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// Migration de dados: preenche Notifications.DisciplinaryCaseId das notificações antigas a partir do
        /// Metadata ("SourceEntity=DisciplinaryCase;SourceId=&lt;guid&gt;" ou "DisciplinaryCaseId=&lt;guid&gt;"), para que
        /// nenhuma notificação vinculada a processo escape do filtro de escopo. Idempotente: só toca linhas sem vínculo.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE n SET n.[DisciplinaryCaseId] = TRY_CONVERT(uniqueidentifier, SUBSTRING(n.[Metadata], CHARINDEX('SourceId=', n.[Metadata]) + 9, 36))
FROM [Notifications] n
WHERE n.[DisciplinaryCaseId] IS NULL
  AND n.[Metadata] LIKE '%SourceEntity=DisciplinaryCase%'
  AND CHARINDEX('SourceId=', n.[Metadata]) > 0;");

            migrationBuilder.Sql(@"
UPDATE n SET n.[DisciplinaryCaseId] = TRY_CONVERT(uniqueidentifier, SUBSTRING(n.[Metadata], CHARINDEX('DisciplinaryCaseId=', n.[Metadata]) + 19, 36))
FROM [Notifications] n
WHERE n.[DisciplinaryCaseId] IS NULL
  AND CHARINDEX('DisciplinaryCaseId=', n.[Metadata]) > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sem volta: o vínculo é derivado do Metadata, que continua intacto.
        }
    }
}
