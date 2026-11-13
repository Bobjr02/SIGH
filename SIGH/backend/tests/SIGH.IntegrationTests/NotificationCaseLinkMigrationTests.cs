using System.Net.Http.Json;
using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Records;
using SIGH.Application.Notifications.DTOs;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using SIGH.Persistence.Migrations;
using SIGH.Persistence.Seeds;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Parte 0.1: notificações antigas só tinham o processo no Metadata e escapavam do filtro de escopo.
/// A migration de dados LinkLegacyNotificationsToCases preenche DisciplinaryCaseId; este teste roda o SQL dela.
/// </summary>
public class NotificationCaseLinkMigrationTests : IntegrationTestBase
{
    public NotificationCaseLinkMigrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private static IReadOnlyList<string> MigrationSql()
    {
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        typeof(LinkLegacyNotificationsToCases).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(new LinkLegacyNotificationsToCases(), [builder]);
        return builder.Operations.OfType<SqlOperation>().Select(o => o.Sql).ToList();
    }

    [Fact]
    public async Task LegacyNotificationsOfACase_AreLinked_AndNoLongerEscapeTheScope()
    {
        await ReferenceData.LoadEmployeesAsync(Factory);
        var infraction = await ReferenceData.InfractionTypeAsync(Factory);
        var today = BusinessCalendar.Today(Factory.Clock.UtcNow);
        await AccessTestHelpers.CreateUserAsync(Factory, "auxiliar@sigh.test", SystemRoles.HrRegistrar);
        using var registrar = await AccessTestHelpers.LoginAsync(Factory, "auxiliar@sigh.test");
        var r131 = await ReferenceData.RegisterAsync(registrar,
            ReferenceData.Warning(await ReferenceData.EmployeeIdAsync(Factory, "50090006"), infraction, today));

        var readers = await AccessTestHelpers.CreateRoleAsync(Factory, "IntegrationTests.NotificationReaders", PermissionCodes.Notifications.View);
        var g131 = await AccessTestHelpers.CreateUserAsync(Factory, "g131@sigh.test", [new RoleGrant(SystemRoles.Manager), new RoleGrant(readers)], "USGA131");
        var g132 = await AccessTestHelpers.CreateUserAsync(Factory, "g132@sigh.test", [new RoleGrant(SystemRoles.Manager), new RoleGrant(readers)], "USGA132");

        // Notificações gravadas antes do sprint 6: processo só no Metadata, DisciplinaryCaseId nulo.
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            foreach (var (user, title, metadata) in new[]
                     {
                         (g131.Id, "Prazo antigo 131", $"SourceEntity=DisciplinaryCase;SourceId={r131.Summary.Id}"),
                         (g132.Id, "Prazo antigo 131", $"SourceEntity=DisciplinaryCase;SourceId={r131.Summary.Id};EscalationLevel=1"),
                         (g132.Id, "Aviso geral", "Origem=Sistema"),
                     })
            {
                await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO Notifications (Id, CompanyId, UserId, Title, Message, Type, Priority, IsRead, IsExpired, Metadata, DisciplinaryCaseId, CreatedAt, IsDeleted)
VALUES ({Guid.NewGuid()}, {DatabaseSeeder.DefaultCompanyId}, {user}, {title}, {"Mensagem"}, {"Warning"}, {"Medium"}, {false}, {false}, {metadata}, {null}, {DateTimeOffset.UtcNow}, {false})");
            }

            // Antes da migration de dados, o gestor da 132 enxergava a notificação do processo da 131.
            foreach (var sql in MigrationSql())
                await db.Database.ExecuteSqlRawAsync(sql);

            var linked = await db.Notifications.AsNoTracking().Where(n => n.Title == "Prazo antigo 131").ToListAsync();
            linked.Should().HaveCount(2).And.OnlyContain(n => n.DisciplinaryCaseId == r131.Summary.Id);
            (await db.Notifications.SingleAsync(n => n.Title == "Aviso geral")).DisciplinaryCaseId.Should().BeNull();

            // Idempotente: rodar de novo não muda nada.
            foreach (var sql in MigrationSql())
                (await db.Database.ExecuteSqlRawAsync(sql)).Should().Be(0);
        }

        using var client132 = await AccessTestHelpers.LoginAsync(Factory, "g132@sigh.test");
        var seen132 = (await client132.GetFromJsonAsync<Result<PagedResult<NotificationDto>>>("/api/v1/notifications"))!.Data!.Items.Select(n => n.Title).ToList();
        seen132.Should().Equal("Aviso geral");

        using var client131 = await AccessTestHelpers.LoginAsync(Factory, "g131@sigh.test");
        (await client131.GetFromJsonAsync<Result<PagedResult<NotificationDto>>>("/api/v1/notifications"))!.Data!.Items
            .Should().Contain(n => n.Title == "Prazo antigo 131");
    }
}
