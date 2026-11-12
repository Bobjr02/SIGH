using System.Text.Json;
using FluentAssertions;
using SIGH.Application.Common.Authorization;
using Xunit;

namespace SIGH.UnitTests.Authorization;

/// <summary>
/// O frontend espelha PermissionCodes.All em SIGH/frontend/src/auth/permission-codes.json
/// (e o teste do front compara as constantes TypeScript com esse arquivo).
/// Ao adicionar, remover ou renomear uma permissão, atualize o JSON na mesma alteração.
/// </summary>
public class PermissionCodesFrontendSyncTests
{
    private const string RelativeJsonPath = "SIGH/frontend/src/auth/permission-codes.json";

    [Fact]
    public void FrontendPermissionCodesJson_ShouldMatchPermissionCodesAll()
    {
        var jsonPath = FindRepositoryFile(RelativeJsonPath);
        var frontendCodes = JsonSerializer.Deserialize<string[]>(File.ReadAllText(jsonPath))!;

        frontendCodes.Should().Equal(
            PermissionCodes.All.Select(p => p.Code),
            $"{RelativeJsonPath} deve listar exatamente PermissionCodes.All, na mesma ordem");
    }

    private static string FindRepositoryFile(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Arquivo '{relativePath}' não encontrado a partir de {AppContext.BaseDirectory}.");
    }
}
