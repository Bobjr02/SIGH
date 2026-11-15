using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using SIGH.Application.Common.Authorization;
using SIGH.Application.Disciplinary.History;
using SIGH.Application.Disciplinary.Records;
using SIGH.Application.Notifications.Constants;
using SIGH.Application.Security;
using SIGH.Application.Users.Access;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Enums;
using SIGH.Persistence.Auditing;
using Xunit;

namespace SIGH.UnitTests.Localization;

/// <summary>
/// Todos os valores de enum (e de "enums" em constantes de texto) que chegam ao front ficam em
/// SIGH/frontend/src/i18n/enum-values.json. O teste do front (enumLabels.test.ts) falha se algum valor
/// não tiver rótulo em português. Ao criar um valor novo, regenere o arquivo:
///   UPDATE_ENUM_VALUES=1 dotnet test --filter EnumValuesFrontendSyncTests
/// e escreva o rótulo em src/i18n/labels.ts.
/// </summary>
public class EnumValuesFrontendSyncTests
{
    private const string RelativeJsonPath = "SIGH/frontend/src/i18n/enum-values.json";

    private static JsonObject Numeric<TEnum>() where TEnum : struct, Enum
    {
        var obj = new JsonObject();
        foreach (var value in Enum.GetValues<TEnum>())
            obj[value.ToString()] = Convert.ToInt32(value);
        return obj;
    }

    private static JsonObject Strings(params Type[] holders)
    {
        var obj = new JsonObject();
        foreach (var holder in holders)
            foreach (var field in holder.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f is { IsLiteral: true } && f.FieldType == typeof(string)))
            {
                var value = (string)field.GetRawConstantValue()!;
                obj[value] = value;
            }
        return obj;
    }

    private static JsonObject FromList(IEnumerable<string> values)
    {
        var obj = new JsonObject();
        foreach (var value in values)
            obj[value] = value;
        return obj;
    }

    /// <summary>O conteúdo esperado do JSON, a partir dos tipos do backend.</summary>
    public static string Expected()
    {
        var root = new JsonObject
        {
            ["RecordStatus"] = Numeric<RecordStatus>(),
            ["RecordOrigin"] = Numeric<RecordOrigin>(),
            ["RecordActivityKind"] = Numeric<RecordActivityKind>(),
            ["OverviewPeriod"] = Numeric<OverviewPeriod>(),
            ["DisciplinaryMeasureType"] = Numeric<DisciplinaryMeasureType>(),
            ["DisciplinaryMeasureStatus"] = Numeric<DisciplinaryMeasureStatus>(),
            ["DisciplinaryCaseStatus"] = Numeric<DisciplinaryCaseStatus>(),
            ["DecisionStatus"] = Numeric<DecisionStatus>(),
            // Aparecem nos valores da auditoria (JSON com o nome do enum).
            ["DecisionType"] = Numeric<DecisionType>(),
            ["DisciplinaryCaseKind"] = Numeric<DisciplinaryCaseKind>(),
            ["DisciplinaryCasePriority"] = Numeric<DisciplinaryCasePriority>(),
            ["ConfidentialityLevel"] = Numeric<ConfidentialityLevel>(),
            ["InfractionSeverity"] = Numeric<InfractionSeverity>(),
            ["CaseEmployeeRole"] = Numeric<CaseEmployeeRole>(),
            ["EvidenceStatus"] = Numeric<EvidenceStatus>(),
            ["OccurrenceStatus"] = Numeric<OccurrenceStatus>(),
            ["EvidenceType"] = Numeric<EvidenceType>(),
            ["UserStatus"] = Numeric<UserStatus>(),
            ["ValidityState"] = Numeric<ValidityState>(),
            ["EmployeeStatus"] = Numeric<EmployeeStatus>(),
            ["EmployeeGroup"] = Numeric<EmployeeGroup>(),
            ["Role"] = FromList(SystemRoles.All.Select(r => r.Name)),
            ["AuditAction"] = Strings(typeof(AuditActions), typeof(SecurityAuditActions), typeof(HistoryAuditActions)),
            ["AuditEntity"] = FromList(AuditPolicy.AuditedEntityTypes.Select(t => t.Name).Append("AccessControl").Order(StringComparer.Ordinal)),
            ["NotificationType"] = Strings(typeof(NotificationTypes)),
            ["NotificationPriority"] = Strings(typeof(NotificationPriorities)),
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n";
    }

    [Fact]
    public void FrontendEnumValuesJson_MatchesTheBackendEnums()
    {
        var path = FindRepositoryFile(RelativeJsonPath, mustExist: Environment.GetEnvironmentVariable("UPDATE_ENUM_VALUES") != "1");
        var expected = Expected();
        if (Environment.GetEnvironmentVariable("UPDATE_ENUM_VALUES") == "1")
            File.WriteAllText(path, expected);

        File.ReadAllText(path).ReplaceLineEndings("\n").Should().Be(expected,
            $"{RelativeJsonPath} deve refletir os enums do backend (rode com UPDATE_ENUM_VALUES=1 e escreva os rótulos)");
    }

    private static string FindRepositoryFile(string relativePath, bool mustExist)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate) || (!mustExist && Directory.Exists(Path.GetDirectoryName(candidate))))
                return candidate;
        }

        throw new FileNotFoundException($"Arquivo '{relativePath}' não encontrado a partir de {AppContext.BaseDirectory}.");
    }
}
