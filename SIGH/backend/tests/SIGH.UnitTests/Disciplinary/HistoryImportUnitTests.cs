using FluentAssertions;
using SIGH.Application.Disciplinary.History;
using SIGH.Application.Employees.Import;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

public class HistoryRowParserTests
{
    private static SheetRow Row(params (string Key, string Text, bool Number)[] cells) =>
        new(2, cells.ToDictionary(c => c.Key, c => new HrCell(c.Text, c.Number)));

    private static SheetRow Suspension(string period = "", string days = "", string end = "") => Row(
        (HistoryColumns.EmployeeNumber, "50090007", true), (HistoryColumns.Date, "02/06/2025", false),
        (HistoryColumns.TypeText, "SUSPENSÃO 1", false), (HistoryColumns.Infraction, "SUSPENSÃO", false),
        (HistoryColumns.Reason, "FALTAS", false), (HistoryColumns.Period, period, false), (HistoryColumns.Days, days, true),
        (HistoryColumns.EndDate, end, false));

    [Theory]
    [InlineData("02/06/2025 À 04/06/2025")]
    [InlineData("02/06/2025 à 04/06/2025")]
    [InlineData("02/06/2025 A 04/06/2025")]
    [InlineData("02/06/2025 a 04/06/2025")]
    [InlineData("02/06/2025 até 04/06/2025")]
    public void Period_AcceptsEveryConnector(string period)
    {
        var (row, error) = HistoryRowParser.Parse(Suspension(period, "3"));
        error.Should().BeNull();
        row!.PeriodStart.Should().Be(new DateOnly(2025, 6, 2));
        row.PeriodEnd.Should().Be(new DateOnly(2025, 6, 4));
        row.SuspensionDays.Should().Be(3);
        row.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Period_FallsBackToDataUntilDataFim_ThenToUnknownDays()
    {
        HistoryRowParser.Parse(Suspension(end: "04/06/2025")).Row!.SuspensionDays.Should().Be(3);

        var (row, _) = HistoryRowParser.Parse(Suspension());
        row!.SuspensionDays.Should().BeNull("dias desconhecidos, nunca zero");
        row.DaysUnknown.Should().BeTrue();
        row.Warnings.Should().ContainSingle().Which.Should().Contain("sem período");
    }

    [Fact]
    public void DaysDivergingFromThePeriod_ThePeriodWins_WithAWarning()
    {
        var (row, _) = HistoryRowParser.Parse(Suspension("10/03/2026 À 14/03/2026", "3"));
        row!.SuspensionDays.Should().Be(5);
        row.Warnings.Should().ContainSingle().Which.Should().Contain("vale o período");
    }

    [Theory]
    [InlineData("ADVERTÊNCIA", "ADVERTÊNCIA ESCRITA", DisciplinaryMeasureType.WrittenWarning)]
    [InlineData("ADVERTÊNCIA", "ADVERTÊNCIA VERBAL", DisciplinaryMeasureType.VerbalWarning)]
    [InlineData("SUSPENSÃO", "SUSPENSÃO 1", DisciplinaryMeasureType.Suspension)]
    [InlineData("JUSTA CAUSA", "DEMISSÃO POR JUSTA CAUSA", DisciplinaryMeasureType.JustCauseTermination)]
    [InlineData("ADVERTÊNCIA", "DEMISSÃO POR JUSTA CAUSA", DisciplinaryMeasureType.JustCauseTermination)]
    [InlineData("", "SUSPENSÃO 1", DisciplinaryMeasureType.Suspension)]
    public void MeasureType_FollowsTheHistoricoMapping(string infraction, string type, DisciplinaryMeasureType expected) =>
        HistoryRowParser.MeasureTypeOf(infraction, type).Should().Be(expected);

    [Fact]
    public void Dates_AcceptExcelSerialAndText_AndTheNumberAsMatricula()
    {
        HistoryRowParser.ParseDate(new HrCell("45726", true)).Should().Be(new DateOnly(2025, 3, 10));
        HistoryRowParser.ParseDate(new HrCell("15/02/2026", false)).Should().Be(new DateOnly(2026, 2, 15));
        HistoryRowParser.ParseDate(new HrCell("ontem", false)).Should().BeNull();

        var (row, error) = HistoryRowParser.Parse(Row(
            (HistoryColumns.EmployeeNumber, "50090003", true), (HistoryColumns.Date, "45936", true),
            (HistoryColumns.Infraction, "ADVERTÊNCIA", false), (HistoryColumns.TypeText, "ADVERTÊNCIA ESCRITA", false),
            (HistoryColumns.Reason, "FALTAS", false), (HistoryColumns.Days, "1", true)));
        error.Should().BeNull();
        row!.MeasureType.Should().Be(DisciplinaryMeasureType.WrittenWarning);
        row.SuspensionDays.Should().BeNull();
        row.Warnings.Should().ContainSingle().Which.Should().Contain("dias foram ignorados");
    }

    [Fact]
    public void TheKey_IsNumberDateTypeNormalizedReasonAndPeriodStart()
    {
        var (a, _) = HistoryRowParser.Parse(Suspension("02/06/2025 À 04/06/2025"));
        a!.Key.Should().Be("50090007|2025-06-02|4|faltas|2025-06-02");
        HistoryKeys.Normalize("Falta de Utilização do EPI").Should().Be(HistoryKeys.Normalize("FALTA DE UTILIZAÇÃO DO EPI"));
    }

    [Fact]
    public void Headers_AreExactlyThe14OfTheLeiame()
    {
        HistoryColumns.All.Select(c => c.Header).Should().Equal(
            "MATRÍCULA", "DATA", "TIPO DA INFRAÇÃO", "MOTIVO DA INFRAÇÃO", "DIAS DE SUSPENSÃO", "PERÍODO DA SUSPENSÃO",
            "NOME DO FUNCIONÁRIO", "CARGO", "SETOR", "DATA FIM DA SUSPENSÃO", "GERÊNCIA", "INFRAÇÃO", "Nº ADVERTÊNCIA", "Nº SUSPENSÃO");
        HistoryColumns.All.Where(c => !c.ReadValues).Select(c => c.Header).Should().Equal("NOME DO FUNCIONÁRIO", "CARGO", "SETOR");
    }
}

public class HistoricalCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private static DisciplinaryCase Historical(DisciplinaryMeasureType type = DisciplinaryMeasureType.Suspension, int? days = null) =>
        DisciplinaryCase.CreateHistorical("HIST-2025-1", Guid.NewGuid(), "Histórico", "Importado", "k", Guid.NewGuid(), Guid.NewGuid(), Now,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InfractionSeverity.Moderate, type, Now.AddYears(-1), Now.AddYears(-1),
            days is null ? null : Now.AddYears(-1).AddDays(days.Value - 1), days);

    [Fact]
    public void AHistoricalRecord_HasNoDecision_NoApprover_AndIsCompleted()
    {
        var record = Historical(days: null);
        record.Kind.Should().Be(DisciplinaryCaseKind.Historical);
        record.Status.Should().Be(DisciplinaryCaseStatus.Completed);
        record.Decisions.Should().BeEmpty();
        record.Decision.Should().BeNull();
        var measure = record.Measures.Single();
        measure.DisciplinaryDecisionId.Should().BeNull();
        measure.AppliedByUserId.Should().BeNull();
        measure.Status.Should().Be(DisciplinaryMeasureStatus.Applied);
        measure.SuspensionDays.Should().BeNull("dias desconhecidos");
    }

    [Fact]
    public void EveryDomainOperation_RefusesAHistoricalRecord_ExceptEvidenceAndTheReimportCorrection()
    {
        var record = Historical(days: 3);
        var operations = new Action[]
        {
            () => record.UpdateDetails("x", "y"),
            () => record.ChangePriority(DisciplinaryCasePriority.High),
            () => record.ApproveDecision(Guid.NewGuid(), Now),
            () => record.RejectDecision(Guid.NewGuid(), "x", Now),
            () => record.InvalidateDecision("x"),
            () => record.Cancel("x", Now),
            () => record.Complete("x", Now),
            () => record.CancelMeasure(record.Measures.Single().Id, "x"),
            () => record.ApplyMeasure(record.Measures.Single().Id, Now, Guid.NewGuid()),
            () => record.RemoveEmployee(record.Employees.Single().Id),
        };
        foreach (var operation in operations)
            operation.Should().Throw<BusinessRuleValidationException>();

        var evidence = DisciplinaryEvidence.CreatePendingFileEvidence(record.Id, EvidenceType.Document, "Carta assinada antiga", Now, Guid.NewGuid());
        record.Invoking(r => r.AddEvidence(evidence)).Should().NotThrow("a carta assinada antiga pode ser anexada");

        var batch = Guid.NewGuid();
        record.ApplyHistoricalCorrection(Guid.NewGuid(), Now.AddYears(-1), Now.AddYears(-1).AddDays(1), 2, batch);
        record.Measures.Single().SuspensionDays.Should().Be(2);
        record.HistoryUpdatedByBatchId.Should().Be(batch);
    }

    [Fact]
    public void TheReimportCorrection_IsOnlyForHistoricalRecords()
    {
        var normal = DisciplinaryCase.Create("MED-1", Guid.NewGuid(), "T", "D", Now, Guid.NewGuid(), kind: DisciplinaryCaseKind.Record);
        normal.Invoking(c => c.ApplyHistoricalCorrection(Guid.NewGuid(), Now, null, null, Guid.NewGuid()))
            .Should().Throw<BusinessRuleValidationException>();
    }
}
