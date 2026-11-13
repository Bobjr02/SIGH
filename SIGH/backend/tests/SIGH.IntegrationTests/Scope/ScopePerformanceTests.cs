using System.Diagnostics;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.GetEmployees;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Context;
using SIGH.Persistence.Seeds;
using Xunit;

namespace SIGH.IntegrationTests.Scope;

/// <summary>
/// Parte C.5: o gestor das gerências 137 e 141 responde por ~3.100 funcionários. O filtro do escopo vai
/// para o SQL (índice em Employees.ManagementUnitId) e a lista é paginada no banco.
/// </summary>
public class ScopePerformanceTests : IntegrationTestBase
{
    public ScopePerformanceTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ManagerOfUsga137AndUsga141_With3100Employees_GetsADatabasePagedList()
    {
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            var units = await db.ManagementUnits.Where(m => m.Code == "USGA137" || m.Code == "USGA141" || m.Code == "USGA131").ToDictionaryAsync(m => m.Code!);
            var job = JobTitle.Create(DatabaseSeeder.DefaultCompanyId, "Operador", "70000001");
            db.JobTitles.Add(job);

            // 1.500 na 137, 1.600 na 141 (3.100 no escopo) e 400 na 131 (fora).
            var plan = new[] { ("USGA137", 1500), ("USGA141", 1600), ("USGA131", 400) };
            var n = 0;
            foreach (var (code, count) in plan)
            {
                for (var i = 0; i < count; i++, n++)
                {
                    db.Employees.Add(Employee.Create(DatabaseSeeder.DefaultCompanyId, $"7{n:D7}", $"FUNCIONARIO FICTICIO {n}",
                        SyntheticCpf(n), new DateOnly(2020, 1, 1), job.Id, units[code].Id, EmployeeStatus.Active));
                }
            }
            await db.SaveChangesAsync();

            // O índice que sustenta o filtro do escopo existe no banco.
            var indexes = await db.Database.SqlQueryRaw<string>(
                "SELECT i.name AS Value FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id " +
                "JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id " +
                "WHERE i.object_id = OBJECT_ID('Employees') AND c.name = 'ManagementUnitId' AND ic.key_ordinal = 1").ToListAsync();
            indexes.Should().Contain("IX_Employees_ManagementUnitId");
        }

        await AccessTestHelpers.CreateUserAsync(Factory, "gestor137e141@sigh.test", SystemRoles.Manager, "USGA137", "USGA141");
        using var manager = await AccessTestHelpers.LoginAsync(Factory, "gestor137e141@sigh.test");

        var watch = Stopwatch.StartNew();
        var page = (await manager.GetFromJsonAsync<Result<PagedResult<EmployeeListItemResponse>>>("/api/v1/employees?pageSize=50&pageNumber=3"))!.Data!;
        watch.Stop();

        page.TotalCount.Should().Be(3100);
        page.Items.Should().HaveCount(50, "só a página volta do banco");
        page.TotalPages.Should().Be(62);
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    private static string SyntheticCpf(int seed)
    {
        var digits = (100_000_000 + seed).ToString().Select(c => c - '0').ToList();
        for (var round = 0; round < 2; round++)
        {
            var weight = digits.Count + 1;
            var rest = digits.Sum(d => d * weight--) % 11;
            digits.Add(rest < 2 ? 0 : 11 - rest);
        }
        return string.Concat(digits);
    }
}
