using FluentAssertions;
using SIGH.Application.Employees.DecisionSupport;
using SIGH.Domain.Disciplinary.Enums;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class DecisionSupportCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    private static MeasureFact Fact(DisciplinaryMeasureType type, int monthsAgo, string[]? infractions = null, int? suspensionDays = null, string source = "SIGH")
    {
        var date = Now.AddMonths(-monthsAgo);
        return new MeasureFact(source, type, date, date,
            suspensionDays is { } d ? date.AddDays(d - 1) : null,
            infractions ?? ["Atraso"]);
    }

    [Fact]
    public void NoMeasures_IsAnEmptySummary()
    {
        var summary = EmployeeDecisionSupportCalculator.Calculate(Guid.NewGuid(), [], Now);

        summary.TotalMeasures.Should().Be(0);
        summary.LastMeasureAt.Should().BeNull();
        summary.ByType.Should().BeEmpty();
    }

    [Fact]
    public void CountsByTypeWarningsSuspensionDaysAndWindows()
    {
        var facts = new[]
        {
            Fact(DisciplinaryMeasureType.VerbalWarning, 14),
            Fact(DisciplinaryMeasureType.WrittenWarning, 8, ["Atraso", "Falta não justificada"]),
            Fact(DisciplinaryMeasureType.Suspension, 3, ["Falta não justificada"], suspensionDays: 3),
            Fact(DisciplinaryMeasureType.Suspension, 1, ["Insubordinação"], suspensionDays: 5),
            Fact(DisciplinaryMeasureType.Guidance, 0, []),
        };

        var s = EmployeeDecisionSupportCalculator.Calculate(Guid.NewGuid(), facts, Now);

        s.TotalMeasures.Should().Be(5);
        s.Warnings.Should().Be(2);
        s.VerbalWarnings.Should().Be(1);
        s.WrittenWarnings.Should().Be(1);
        s.Suspensions.Should().Be(2);
        s.SuspensionDays.Should().Be(8);
        s.OtherMeasures.Should().Be(1);
        s.MeasuresLast6Months.Should().Be(3);
        s.MeasuresLast12Months.Should().Be(4);
        s.LastMeasureAt.Should().Be(Now);
        s.ByInfractionType.Should().BeEquivalentTo(new[]
        {
            new CountByLabel("Falta não justificada", 2),
            new CountByLabel("Atraso", 2),
            new CountByLabel("Insubordinação", 1),
            new CountByLabel(EmployeeDecisionSupportCalculator.NoInfractionType, 1),
        });
        s.ByType.Should().Contain(new CountByLabel("Suspensão", 2));
    }

    [Fact]
    public void SuspensionWithoutEndDate_DoesNotAddDays()
    {
        var open = new MeasureFact("SIGH", DisciplinaryMeasureType.Suspension, Now, Now, null, ["Atraso"]);

        var s = EmployeeDecisionSupportCalculator.Calculate(Guid.NewGuid(), [open], Now);

        s.Suspensions.Should().Be(1);
        s.SuspensionDays.Should().Be(0);
        s.SuspensionsWithoutPeriod.Should().Be(1);
    }

    [Fact]
    public void SumsEverySource()
    {
        var s = EmployeeDecisionSupportCalculator.Calculate(Guid.NewGuid(),
            [Fact(DisciplinaryMeasureType.WrittenWarning, 2), Fact(DisciplinaryMeasureType.WrittenWarning, 30, source: "Histórico importado")], Now);

        s.WrittenWarnings.Should().Be(2);
        s.Sources.Should().Equal("Histórico importado", "SIGH");
    }

    [Fact]
    public void ADuplicatedInfractionTypeInTheSameCase_CountsOnce()
    {
        var s = EmployeeDecisionSupportCalculator.Calculate(Guid.NewGuid(),
            [Fact(DisciplinaryMeasureType.WrittenWarning, 1, ["Atraso", "Atraso"])], Now);

        s.ByInfractionType.Should().Equal(new CountByLabel("Atraso", 1));
    }
}
