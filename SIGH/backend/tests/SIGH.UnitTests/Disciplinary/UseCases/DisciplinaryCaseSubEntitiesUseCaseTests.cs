using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence;
using SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;
using SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Disciplinary.UseCases;

public class DisciplinaryCaseSubEntitiesUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IDisciplinaryCaseRepository> _caseRepositoryMock;
    private readonly Mock<IInfractionTypeRepository> _infractionTypeRepositoryMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    private readonly AddOccurrenceUseCase _addOccurrenceUseCase;
    private readonly AddEmployeeToCaseUseCase _addEmployeeUseCase;
    private readonly AddEvidenceUseCase _addEvidenceUseCase;
    private readonly RecordDecisionUseCase _recordDecisionUseCase;
    private readonly ApplyMeasureUseCase _applyMeasureUseCase;

    private readonly DateTimeOffset _now = new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    public DisciplinaryCaseSubEntitiesUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _caseRepositoryMock = new Mock<IDisciplinaryCaseRepository>();
        _infractionTypeRepositoryMock = new Mock<IInfractionTypeRepository>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(_now);

        _addOccurrenceUseCase = new AddOccurrenceUseCase(_contextMock.Object, _caseRepositoryMock.Object, _infractionTypeRepositoryMock.Object, _dateTimeProviderMock.Object);
        _addEmployeeUseCase = new AddEmployeeToCaseUseCase(_contextMock.Object, _caseRepositoryMock.Object, _employeeRepositoryMock.Object);
        _addEvidenceUseCase = new AddEvidenceUseCase(_contextMock.Object, _caseRepositoryMock.Object);
        _recordDecisionUseCase = new RecordDecisionUseCase(_contextMock.Object, _caseRepositoryMock.Object, _dateTimeProviderMock.Object);
        _applyMeasureUseCase = new ApplyMeasureUseCase(_contextMock.Object, _caseRepositoryMock.Object, _employeeRepositoryMock.Object, _dateTimeProviderMock.Object);
    }

    [Fact]
    public async Task AddOccurrence_WithValidInput_ShouldAddAndSave()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var caseObj = DisciplinaryCase.Create("PROC-OCC-01", companyId, "Título", "Descrição", userId, _now);
        var infraction = InfractionType.Create("INF-01", "Atraso", InfractionSeverity.Low);

        _caseRepositoryMock.Setup(r => r.GetByIdAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);
        _infractionTypeRepositoryMock.Setup(r => r.GetByIdAsync(infraction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(infraction);

        var request = new AddOccurrenceRequest(caseObj.Id, _now, "Descrição da ocorrência", userId, infraction.Id, InfractionSeverity.Medium);

        // Act
        var result = await _addOccurrenceUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        caseObj.Occurrences.Should().HaveCount(1);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddEmployeeToCase_WithValidInput_ShouldAddAndSave()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var caseObj = DisciplinaryCase.Create("PROC-EMP-01", companyId, "Título", "Descrição", userId, _now);

        var company = Company.Create("Empresa SIGH", "Empresa SIGH LTDA", "12345678000195");
        var unit = ManagementUnit.Create(company.Id, "Unidade 1", "UG-01");
        var jobTitle = JobTitle.Create(company.Id, "Desenvolvedor", "DEV");
        var employee = Employee.Create("João Silva", "12345678909", "joao@empresa.com", "EMP-001", company.Id, unit.Id, jobTitle.Id, new DateOnly(2025, 1, 1));

        _caseRepositoryMock.Setup(r => r.GetByIdAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);
        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new AddEmployeeToCaseRequest(caseObj.Id, employee.Id, CaseEmployeeRole.Accused, true);

        // Act
        var result = await _addEmployeeUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        caseObj.Employees.Should().HaveCount(1);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddEvidence_WithValidInput_ShouldAddAndSave()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var caseObj = DisciplinaryCase.Create("PROC-EVI-01", companyId, "Título", "Descrição", userId, _now);

        _caseRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        var request = new AddEvidenceRequest(
            caseObj.Id,
            EvidenceType.Document,
            "E-mail impresso",
            userId,
            _now,
            StorageReference: "evidences/email-impresso.pdf",
            OriginalFileName: "email-impresso.pdf",
            ContentType: "application/pdf",
            FileSize: 1024);

        // Act
        var result = await _addEvidenceUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        caseObj.Evidences.Should().HaveCount(1);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordDecision_WithValidInput_ShouldRecordAndSave()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var caseObj = DisciplinaryCase.Create("PROC-DEC-01", companyId, "Título", "Descrição", userId, _now);

        _caseRepositoryMock.Setup(r => r.GetByIdAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        var request = new RecordDecisionRequest(
            caseObj.Id,
            DecisionType.FormalWarning,
            "Advertência por escrito.",
            "A apuração confirmou a conduta e fundamentou a aplicação da advertência.",
            userId);

        // Act
        var result = await _recordDecisionUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        caseObj.Decisions.Should().HaveCount(1);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyMeasure_WithValidDecisionAndEmployee_ShouldApplyAndSave()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var caseObj = DisciplinaryCase.Create("PROC-MEA-01", companyId, "Título", "Descrição", userId, _now);

        var decision = DisciplinaryDecision.Create(caseObj.Id, DecisionType.WrittenWarning, "Justificativa", userId, _now);
        caseObj.RecordDecision(decision);

        var company = Company.Create("Empresa SIGH", "Empresa SIGH LTDA", "12345678000195");
        var unit = ManagementUnit.Create(company.Id, "Unidade 1", "UG-01");
        var jobTitle = JobTitle.Create(company.Id, "Desenvolvedor", "DEV");
        var employee = Employee.Create("Maria Souza", "98765432100", "maria@empresa.com", "EMP-002", company.Id, unit.Id, jobTitle.Id, new DateOnly(2025, 1, 1));

        _caseRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);
        _employeeRepositoryMock.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var request = new ApplyMeasureRequest(
            caseObj.Id,
            decision.Id,
            employee.Id,
            DisciplinaryMeasureType.WrittenWarning,
            "Advertência por escrito referente ao atraso de 25/07.",
            _now,
            userId);

        // Act
        var result = await _applyMeasureUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        caseObj.Measures.Should().HaveCount(1);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
