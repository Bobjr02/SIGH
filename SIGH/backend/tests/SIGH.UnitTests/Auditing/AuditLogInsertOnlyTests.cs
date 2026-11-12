using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using SIGH.Api.Controllers;
using SIGH.Domain.Entities;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.UnitTests.Auditing;

/// <summary>A trilha de auditoria só recebe inserções.</summary>
public class AuditLogInsertOnlyTests
{
    private static SighDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<SighDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task UpdatingAnAuditLog_Throws()
    {
        await using var context = CreateContext();
        var log = new AuditLog { Action = "Security.LoginFailed", Entity = "User" };
        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();

        log.Action = "Security.LoginSucceeded";
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*somente inserção*");
    }

    [Fact]
    public async Task DeletingAnAuditLog_Throws()
    {
        await using var context = CreateContext();
        var log = new AuditLog { Action = "Security.Logout", Entity = "User" };
        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();

        context.AuditLogs.Remove(log);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void ApplicationCode_NeverUpdatesOrDeletesAuditLogs()
    {
        var srcDir = FindDirectory("SIGH/backend/src");
        var forbidden = new Regex(
            @"AuditLogs\s*\.\s*(Update|UpdateRange|Remove|RemoveRange|Attach|ExecuteUpdate|ExecuteDelete)\b|Set<AuditLog>\(\)\s*\.\s*(Update|Remove|ExecuteUpdate|ExecuteDelete)|(UPDATE|DELETE\s+FROM)\s+\[?(dbo\]?\.\[?)?AuditLogs",
            RegexOptions.IgnoreCase);

        var offenders = Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            .Where(f => forbidden.IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(srcDir, f))
            .ToList();

        offenders.Should().BeEmpty("nenhum código da aplicação pode alterar ou excluir registros de auditoria");
    }

    [Fact]
    public void AuditEndpoints_AreReadOnly()
    {
        var actions = typeof(BaseController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => (Controller: t, Method: m)))
            .Where(x => IsAuditRoute(x.Controller, x.Method))
            .ToList();

        actions.Should().NotBeEmpty();
        actions.SelectMany(x => x.Method.GetCustomAttributes<HttpMethodAttribute>().SelectMany(a => a.HttpMethods))
            .Should().OnlyContain(m => m == "GET");
    }

    private static bool IsAuditRoute(Type controller, MethodInfo method)
    {
        var routes = controller.GetCustomAttributes<RouteAttribute>().Select(r => r.Template)
            .Concat(method.GetCustomAttributes<HttpMethodAttribute>().Select(a => a.Template ?? string.Empty));
        return routes.Any(r => r != null && r.Contains("audit", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindDirectory(string relativePath)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (Directory.Exists(candidate))
                return candidate;
        }
        throw new DirectoryNotFoundException(relativePath);
    }
}
