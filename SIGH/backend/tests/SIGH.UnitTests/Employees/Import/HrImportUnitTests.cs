using FluentAssertions;
using SIGH.Application.Employees.Import;
using SIGH.Domain.Employees.Enums;
using SIGH.Infrastructure.Import;
using SIGH.IntegrationTests.Infrastructure;
using Xunit;
using R = SIGH.IntegrationTests.Infrastructure.HrXlsxBuilder.Row;

namespace SIGH.UnitTests.Employees.Import;

public class HrImportColumnsTests
{
    [Theory]
    [InlineData("Descr.Unid.Org.", "descrunidorg")]
    [InlineData("  DESCR. UNID. ORG ", "descrunidorg")]
    [InlineData("Data admissão", "dataadmissao")]
    [InlineData("data   ADMISSAO", "dataadmissao")]
    [InlineData("Chave Organização", "chaveorganizacao")]
    [InlineData("Matrícula", "matricula")]
    public void Normalize_IgnoresAccentsCaseSpacesAndPunctuation(string header, string expected)
    {
        HrImportColumns.Normalize(header).Should().Be(expected);
    }
}

public class HrRowValidatorTests
{
    private static HrRawRow Raw(params (HrField Field, string Text, bool Number)[] cells)
    {
        var defaults = new Dictionary<HrField, HrCell>
        {
            [HrField.EmployeeNumber] = new("50090001", false),
            [HrField.FullName] = new("Pessoa Sintética", false),
            [HrField.Cpf] = new(HrXlsxBuilder.Cpf(1), false),
            [HrField.AdmissionDate] = new("01/02/2020", false),
            [HrField.Status] = new("Ativo", false),
            [HrField.JobTitleCode] = new("50001234", false),
            [HrField.JobTitleName] = new("Operador", false),
            [HrField.DepartmentCode] = new("60001000", false),
            [HrField.DepartmentName] = new("Moagem", false),
            [HrField.ManagementUnitCode] = new("usga131", false),
        };
        foreach (var (field, text, number) in cells) defaults[field] = new HrCell(text, number);
        return new HrRawRow(7, defaults);
    }

    private static (HrRow? Row, List<HrImportIssue> Issues) Validate(params (HrField, string, bool)[] cells)
    {
        var issues = new List<HrImportIssue>();
        return (HrRowValidator.Validate(Raw(cells), issues), issues);
    }

    [Fact]
    public void ValidRow_IsConverted()
    {
        var (row, issues) = Validate();

        issues.Should().BeEmpty();
        row!.Status.Should().Be(EmployeeStatus.Active);
        row.AdmissionDate.Should().Be(new DateOnly(2020, 2, 1));
        row.ManagementUnitCode.Should().Be("USGA131");
    }

    [Fact]
    public void NumericCells_ArePaddedWithLeadingZeros()
    {
        var cpf = HrXlsxBuilder.Cpf("012345678");
        var (row, issues) = Validate((HrField.Cpf, long.Parse(cpf).ToString(), true), (HrField.EmployeeNumber, "5009001", true));

        issues.Should().BeEmpty();
        row!.Cpf.Should().Be(cpf).And.HaveLength(11).And.StartWith("0");
        row.EmployeeNumber.Should().Be("05009001");
    }

    [Fact]
    public void TextEmployeeNumberWithFewerDigits_IsNotPadded()
    {
        var (row, issues) = Validate((HrField.EmployeeNumber, "5009001", false));

        row.Should().BeNull();
        issues.Should().ContainSingle(i => i.Message.Contains("8 dígitos"));
    }

    [Fact]
    public void FormattedCpfText_IsAccepted()
    {
        var cpf = HrXlsxBuilder.Cpf(1);
        var formatted = $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";

        Validate((HrField.Cpf, formatted, false)).Row!.Cpf.Should().Be(cpf);
    }

    [Theory]
    [InlineData("12345678900")]
    [InlineData("11111111111")]
    [InlineData("1234")]
    public void InvalidCpf_IsRejected_WithoutEchoingIt(string cpf)
    {
        var (row, issues) = Validate((HrField.Cpf, cpf, false));

        row.Should().BeNull();
        issues.Should().ContainSingle().Which.Message.Should().Contain("CPF inválido").And.NotContain(cpf);
    }

    [Theory]
    [InlineData("2020-02-01")]
    [InlineData("01.02.2020")]
    [InlineData("31/02/2020")]
    [InlineData("1/2/2020")]
    public void DatesMustBeDdMmYyyy(string date)
    {
        Validate((HrField.AdmissionDate, date, false)).Issues.Should().ContainSingle(i => i.Message.Contains("dd/MM/yyyy"));
    }

    [Theory]
    [InlineData("Ativo", EmployeeStatus.Active)]
    [InlineData("ATIVA", EmployeeStatus.Active)]
    [InlineData("Afastado", EmployeeStatus.OnLeave)]
    [InlineData("Desligado", EmployeeStatus.Terminated)]
    [InlineData("demitida", EmployeeStatus.Terminated)]
    public void KnownStatuses_AreMapped(string status, EmployeeStatus expected)
    {
        var (row, _) = Validate((HrField.Status, status, false), (HrField.TerminationDate, "10/10/2026", false));

        row!.Status.Should().Be(expected);
    }

    [Theory]
    [InlineData("Aposentado")]
    [InlineData("Férias")]
    [InlineData("Inativo")]
    public void UnknownStatus_IsRejected_NeverGuessed(string status)
    {
        var (row, issues) = Validate((HrField.Status, status, false));

        row.Should().BeNull();
        issues.Should().ContainSingle().Which.Message.Should().Contain("desconhecida");
    }

    [Fact]
    public void ActiveWithTerminationDate_IgnoresTheDateWithAWarning()
    {
        var (row, issues) = Validate((HrField.TerminationDate, "10/10/2026", false));

        row!.TerminationDate.Should().BeNull();
        issues.Should().ContainSingle(i => i.Severity == HrIssueSeverity.Warning);
    }

    [Fact]
    public void UnknownGroup_IsRejected()
    {
        Validate((HrField.Group, "Escritório", false)).Row.Should().BeNull();
        Validate((HrField.Group, "Indústria", false)).Row!.Group.Should().Be(EmployeeGroup.Industry);
        Validate((HrField.Group, "CAMPO", false)).Row!.Group.Should().Be(EmployeeGroup.Field);
    }

    [Fact]
    public void Issues_IdentifyRowAndEmployeeNumber()
    {
        var (_, issues) = Validate((HrField.FullName, "", false));

        issues.Should().ContainSingle().Which.Should().Match<HrImportIssue>(i => i.RowNumber == 7 && i.EmployeeNumber == "50090001");
    }
}

public class ExcelHrSpreadsheetReaderTests
{
    private static HrSheet Read(byte[] xlsx) => new ExcelHrSpreadsheetReader().Read(new MemoryStream(xlsx));

    [Fact]
    public void ReadsOnlyTheClosedColumnList_EvenWithDuplicatedHeaders()
    {
        var sheet = Read(new HrXlsxBuilder().Add(new R("50090001", "Ana Sintética", HrXlsxBuilder.Cpf(1))).Build());

        var row = sheet.Rows.Should().ContainSingle().Subject;
        row.RowNumber.Should().Be(2);
        row.Cells.Keys.Should().BeSubsetOf(Enum.GetValues<HrField>());
        row.Cells.Values.Select(c => c.Text).Should().NotContain(t => HrXlsxBuilder.SensitiveMarkers.Any(t.Contains));
        row.Get(HrField.FullName).Text.Should().Be("Ana Sintética");
    }

    [Fact]
    public void NumericCells_AreFlaggedAsNumbers()
    {
        var sheet = Read(new HrXlsxBuilder().Add(new R(50090001L, "Ana", 1234567890L)).Build());

        sheet.Rows[0].Get(HrField.Cpf).Should().Be(new HrCell("1234567890", true));
        sheet.Rows[0].Get(HrField.EmployeeNumber).WasNumber.Should().BeTrue();
    }

    [Theory]
    [InlineData("Chave Organização")]
    [InlineData("Matrícula")]
    [InlineData("Descr.Cargo")]
    public void MissingRequiredColumn_IsReportedByName(string header)
    {
        var act = () => Read(new HrXlsxBuilder().Without(header).Add(new R("50090001", "Ana", HrXlsxBuilder.Cpf(1))).Build());

        act.Should().Throw<HrSpreadsheetException>().WithMessage($"*\"{header}\"*");
    }

    [Fact]
    public void OptionalColumnsMayBeMissing()
    {
        var sheet = Read(new HrXlsxBuilder().Without("Grupo").Without("Turma").Without("Data demissão")
            .Add(new R("50090001", "Ana", HrXlsxBuilder.Cpf(1))).Build());
        var issues = new List<HrImportIssue>();

        var row = HrRowValidator.Validate(sheet.Rows[0], issues);

        issues.Should().BeEmpty();
        row!.Group.Should().BeNull();
        row.Team.Should().BeNull();
        row.TerminationDate.Should().BeNull();
    }

    [Fact]
    public void NotAnXlsx_IsAClearError()
    {
        var act = () => Read("isto não é uma planilha FICTICIA"u8.ToArray());

        act.Should().Throw<HrSpreadsheetException>().Which.Message.Should().NotContain("FICTICIA");
    }
}

public class EmployeeImportPlannerTests
{
    private static readonly ReferenceSnapshot Refs = new(
        new Dictionary<string, string> { ["50001234"] = "Operador de Máquinas" },
        new Dictionary<string, string> { ["60001000"] = "Moagem" },
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "USGA131" });

    private static EmployeeSnapshot Existing(string number, string cpf, EmployeeStatus status = EmployeeStatus.Active) =>
        new(Guid.NewGuid(), number, "Pessoa " + number, cpf, new DateOnly(2020, 2, 1), status == EmployeeStatus.Terminated ? new DateOnly(2025, 1, 1) : null,
            status, "50001234", "60001000", "USGA131", EmployeeGroup.Industry, "A");

    private static IReadOnlyList<HrRawRow> Rows(params R[] rows) =>
        new ExcelHrSpreadsheetReader().Read(new MemoryStream(new HrXlsxBuilder().AddRange(rows).Build())).Rows;

    private static EmployeeImportPlan Plan(IReadOnlyList<HrRawRow> rows, params EmployeeSnapshot[] existing) =>
        EmployeeImportPlanner.Plan(rows, [], existing, Refs, new DateOnly(2026, 11, 13), 10);

    [Fact]
    public void AnExistingEmployeeWithIdenticalData_IsUnchanged()
    {
        var plan = Plan(Rows(new R("50090001", "Pessoa 50090001", HrXlsxBuilder.Cpf(1))), Existing("50090001", HrXlsxBuilder.Cpf(1)));

        plan.Changes.Should().BeEmpty();
        plan.UnchangedEmployeeNumbers.Should().Equal("50090001");
    }

    [Fact]
    public void Absent_IsTerminated_ButAbsentAlreadyTerminatedIsLeftAlone()
    {
        var plan = Plan(Rows(new R("50090001", "Pessoa 50090001", HrXlsxBuilder.Cpf(1))),
            Existing("50090001", HrXlsxBuilder.Cpf(1)), Existing("50090002", HrXlsxBuilder.Cpf(2)), Existing("50090003", HrXlsxBuilder.Cpf(3), EmployeeStatus.Terminated));

        plan.Of(EmployeeChangeKind.TerminatedByAbsence).Select(c => c.EmployeeNumber).Should().Equal("50090002");
    }

    [Fact]
    public void MassTermination_AboveTheLimit_RequiresConfirmation()
    {
        var existing = Enumerable.Range(1, 10).Select(i => Existing($"5009{i:0000}", HrXlsxBuilder.Cpf(i))).ToArray();
        var keepNine = Rows(Enumerable.Range(1, 9).Select(i => new R($"5009{i:0000}", $"Pessoa 5009{i:0000}", HrXlsxBuilder.Cpf(i))).ToArray());
        var keepEight = Rows(Enumerable.Range(1, 8).Select(i => new R($"5009{i:0000}", $"Pessoa 5009{i:0000}", HrXlsxBuilder.Cpf(i))).ToArray());

        Plan(keepNine, existing).RequiresMassTerminationConfirmation.Should().BeFalse("1 de 10 = 10%, não passa do limite");
        Plan(keepEight, existing).RequiresMassTerminationConfirmation.Should().BeTrue("2 de 10 = 20%");
    }

    [Fact]
    public void CpfOfAnotherActiveEmployee_IsAnError()
    {
        var plan = Plan(Rows(new R("50090009", "Outra Pessoa", HrXlsxBuilder.Cpf(1)), new R("50090001", "Pessoa 50090001", HrXlsxBuilder.Cpf(1))),
            Existing("50090001", HrXlsxBuilder.Cpf(1)));

        plan.Issues.Should().Contain(i => i.Severity == HrIssueSeverity.Error && i.Message.Contains("CPF repetido"));
    }

    [Fact]
    public void CpfOfATerminatedEmployeeInTheDatabase_IsAReadmissionWarning()
    {
        var plan = Plan(Rows(new R("50090009", "Pessoa Readmitida", HrXlsxBuilder.Cpf(1))),
            Existing("50090001", HrXlsxBuilder.Cpf(1), EmployeeStatus.Terminated));

        plan.Of(EmployeeChangeKind.Created).Should().ContainSingle();
        plan.Issues.Should().ContainSingle(i => i.Severity == HrIssueSeverity.Warning && i.Message.Contains("Possível readmissão") && i.Message.Contains("50090001"));
    }

    [Fact]
    public void CpfOfAnActiveEmployeeNotInTheFile_IsAReadmissionBecauseTheOldOneWillBeTerminated()
    {
        var plan = Plan(Rows(new R("50090009", "Pessoa Readmitida", HrXlsxBuilder.Cpf(1))), Existing("50090001", HrXlsxBuilder.Cpf(1)));

        plan.Of(EmployeeChangeKind.TerminatedByAbsence).Should().ContainSingle(c => c.EmployeeNumber == "50090001");
        plan.Of(EmployeeChangeKind.Created).Should().ContainSingle(c => c.EmployeeNumber == "50090009");
    }

    [Fact]
    public void ChangedCpf_IsShownMasked()
    {
        var plan = Plan(Rows(new R("50090001", "Pessoa 50090001", HrXlsxBuilder.Cpf(2))), Existing("50090001", HrXlsxBuilder.Cpf(1)));

        var change = plan.Of(EmployeeChangeKind.Updated).Single().Changes.Single(c => c.Field == "CPF");
        change.OldValue.Should().MatchRegex(@"^\*\*\*\.\d{3}\.\d{3}-\*\*$").And.NotBe(HrXlsxBuilder.Cpf(1));
        change.NewValue.Should().NotContain(HrXlsxBuilder.Cpf(2));
    }

    [Fact]
    public void TheFingerprint_IsStableForTheSameInput()
    {
        var rows = Rows(new R("50090001", "Pessoa 50090001", HrXlsxBuilder.Cpf(1)));
        var existing = Existing("50090001", HrXlsxBuilder.Cpf(2));

        Plan(rows, existing).Fingerprint.Should().Be(Plan(rows, existing).Fingerprint);
    }

    [Fact]
    public void MaskCpf_ShowsOnlyTheMiddleDigits()
    {
        EmployeeImportPlanner.MaskCpf("12398224700").Should().Be("***.982.247-**");
    }
}
