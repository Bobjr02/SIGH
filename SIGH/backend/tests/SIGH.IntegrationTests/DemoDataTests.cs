using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SIGH.Api.Demo;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Records;
using SIGH.Application.Interfaces;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>Parte C: dados de demonstração pelos serviços reais, idempotentes, e nunca fora de Development/Demo.</summary>
public class DemoDataTests : IntegrationTestBase
{
    private const string DemoPassword = "Demo#Senha2026ok";

    public DemoDataTests(CustomWebApplicationFactory factory) : base(factory) { }

    private static string ReferenceFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "SIGH", "docs", "referencias");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("SIGH/docs/referencias não encontrado.");
    }

    private DemoDataSeeder Seeder() =>
        new(Factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Factory.Services.GetRequiredService<IConfiguration>(),
            Options.Create(new DemoDataOptions { DemoData = true, DemoPassword = DemoPassword, DemoDataPath = ReferenceFolder() }),
            Factory.Services.GetRequiredService<IDateTimeProvider>(),
            NullLogger<DemoDataSeeder>.Instance);

    [Fact]
    public async Task DemoData_UsesTheRealServices_CoversEveryState_AndIsIdempotent()
    {
        await Seeder().SeedAsync();
        await Seeder().SeedAsync(); // subir de novo não duplica nada

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        (await db.Employees.CountAsync()).Should().Be(40);
        (await db.EmployeeImportBatches.CountAsync()).Should().Be(1, "a carga entrou pelo importador, uma vez só");
        var demoUsers = await db.Users.Where(u => u.Email.EndsWith("@demo.local")).ToListAsync();
        demoUsers.Should().HaveCount(6).And.OnlyContain(u => !u.MustChangePassword && u.Status == SIGH.Domain.Enums.UserStatus.Active);

        // b. histórico fictício pelo importador do histórico, uma vez só.
        (await db.DisciplinaryCases.CountAsync(c => c.Kind == DisciplinaryCaseKind.Historical)).Should().Be(14);
        (await db.DisciplinaryHistoryImportBatches.CountAsync()).Should().Be(1);
        (await db.InfractionTypes.CountAsync(i => i.Name == "USO DE CELULAR EM SERVIÇO")).Should().Be(1);

        var records = await db.DisciplinaryCases.Include(c => c.Decisions).Include(c => c.Evidences)
            .Where(c => c.Kind == DisciplinaryCaseKind.Record).ToListAsync();
        records.Should().HaveCount(8);
        records.Count(c => c.Status == DisciplinaryCaseStatus.Completed).Should().Be(4, "3 aprovadas + 1 suspensão aprovada");
        records.Count(c => c.Status == DisciplinaryCaseStatus.Cancelled).Should().Be(1);
        records.Count(c => c.Status == DisciplinaryCaseStatus.AwaitingDecision && c.Decision != null).Should().Be(2, "pendentes");
        records.Count(c => c.Status == DisciplinaryCaseStatus.AwaitingDecision && c.Decision == null).Should().Be(1, "rejeitada aguardando correção");
        records.Should().ContainSingle(c => c.Evidences.Any(e => e.HasFile && e.OriginalFileName == "carta-assinada-ficticia.pdf"));
        var recordIds = records.Select(r => r.Id).ToList();
        (await db.DisciplinaryMeasures.CountAsync(m => recordIds.Contains(m.DisciplinaryCaseId)
            && m.MeasureType == DisciplinaryMeasureType.Suspension && m.Status == DisciplinaryMeasureStatus.Applied)).Should().Be(1, "a suspensão aprovada no SIGH");

        // Os usuários entram com a senha de demonstração, sem troca obrigatória, e o escopo vale.
        var client = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(client, "gestor.industrial@demo.local", DemoPassword);
        login.MustChangePassword.Should().BeFalse();
        client.DefaultRequestHeaders.Authorization = new("Bearer", login.AccessToken);
        var seen = (await client.GetFromJsonAsync<Result<PagedResult<RecordSummaryDto>>>("/api/v1/disciplinary-records?pageSize=50"))!.Data!.Items;
        seen.Should().NotBeEmpty().And.OnlyContain(r => r.ManagementUnitCode == "USGA131");
        seen.Should().Contain(r => r.Origin == RecordOrigin.Historical, "o gestor vê o histórico da gerência dele");
    }

    [Fact]
    public void DemoData_InProduction_PreventsTheApiFromStarting()
    {
        using var production = Factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Seed:DemoData", "true");
            builder.UseSetting("Seed:DemoPassword", DemoPassword);
        });

        var previousExitCode = Environment.ExitCode;
        try
        {
            var start = () => production.CreateClient();

            // O Program valida as opções (ValidateOnStart) antes das migrations: a falha de Seed:DemoData é
            // registrada como erro fatal de configuração, o processo sai com código 1 e o servidor nunca sobe.
            start.Should().Throw<InvalidOperationException>().WithMessage("*not been started*");
            Environment.ExitCode.Should().Be(1);
        }
        finally
        {
            Environment.ExitCode = previousExitCode;
        }
    }
}
