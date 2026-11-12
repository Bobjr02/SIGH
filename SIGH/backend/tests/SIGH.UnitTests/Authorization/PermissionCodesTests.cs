using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SIGH.Api.Controllers;
using SIGH.Application.Common.Authorization;
using SIGH.Infrastructure.Authorization;
using Xunit;

namespace SIGH.UnitTests.Authorization;

/// <summary>
/// Impede que os códigos exigidos pelos controllers divirjam do catálogo semeado no banco.
/// </summary>
public class PermissionCodesTests
{
    private static IReadOnlyList<(string Location, string Code)> GetControllerPermissions()
    {
        var controllerTypes = typeof(BaseController).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(t));

        var result = new List<(string, string)>();
        foreach (var type in controllerTypes)
        {
            result.AddRange(type.GetCustomAttributes<PermissionAttribute>(inherit: true)
                .Select(a => (type.Name, a.Permission)));

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                result.AddRange(method.GetCustomAttributes<PermissionAttribute>(inherit: true)
                    .Select(a => ($"{type.Name}.{method.Name}", a.Permission)));
            }
        }

        return result;
    }

    [Fact]
    public void EveryControllerPermission_ShouldExistInPermissionCodesAll()
    {
        var known = PermissionCodes.All.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        var used = GetControllerPermissions();

        used.Should().NotBeEmpty("os controllers devem declarar permissões via [Permission]");

        var missing = used.Where(u => !known.Contains(u.Code))
            .Select(u => $"{u.Location} -> {u.Code}")
            .ToList();

        missing.Should().BeEmpty("todo código usado em [Permission] deve estar em PermissionCodes.All");
    }

    [Fact]
    public void PermissionCodesAll_ShouldHaveUniqueCodesAndPortugueseMetadata()
    {
        PermissionCodes.All.Select(p => p.Code).Should().OnlyHaveUniqueItems();
        PermissionCodes.All.Should().AllSatisfy(p =>
        {
            p.Code.Should().NotBeNullOrWhiteSpace();
            p.Name.Should().NotBeNullOrWhiteSpace();
            p.Description.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void PermissionCodesAll_ShouldOnlyContainCodesUsedByControllers()
    {
        // Evita permissões órfãs no seed (códigos que nenhum endpoint exige).
        var used = GetControllerPermissions().Select(u => u.Code).ToHashSet(StringComparer.Ordinal);

        PermissionCodes.All.Select(p => p.Code).Should().BeSubsetOf(used);
    }
}
