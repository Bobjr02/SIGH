using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases;
using SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation;
using SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using Xunit;

namespace SIGH.UnitTests.Disciplinary.UseCases;

public class DisciplinaryCaseLifecycleUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IDisciplinaryCaseRepository> _caseRepositoryMock;

    public DisciplinaryCaseLifecycleUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _caseRepositoryMock = new Mock<IDisciplinaryCaseRepository>();
    }

    [Fact]
    public async Task OpenCase_WhenDraft_ShouldTransitionToOpen()
    {
        // Arrange
        var caseObj = DisciplinaryCase.Create("PROC-100", Guid.NewGuid(), "Título", "Descrição", DateTimeOffset.UtcNow, Guid.NewGuid());
        _caseRepositoryMock.Setup(r => r.GetByIdAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        var validator = new OpenCaseValidator();
        var useCase = new OpenCaseUseCase(_caseRepositoryMock.Object, _contextMock.Object, validator);

        var request = new OpenCaseRequest(caseObj.Id, Guid.NewGuid());

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(DisciplinaryCaseStatus.Open);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartInvestigation_WhenOpenWithOccurrence_ShouldTransitionToUnderInvestigation()
    {
        // Arrange
        var caseObj = DisciplinaryCase.Create("PROC-101", Guid.NewGuid(), "Título", "Descrição", DateTimeOffset.UtcNow, Guid.NewGuid());
        caseObj.Open();
        var occurrence = DisciplinaryOccurrence.Create(caseObj.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "Falta injustificada", Guid.NewGuid(), Guid.NewGuid(), InfractionSeverity.Moderate);
        caseObj.AddOccurrence(occurrence);

        _caseRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        var validator = new StartInvestigationValidator();
        var useCase = new StartInvestigationUseCase(_caseRepositoryMock.Object, _contextMock.Object, validator);

        var request = new StartInvestigationRequest(caseObj.Id, Guid.NewGuid());

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(DisciplinaryCaseStatus.UnderInvestigation);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitCaseForDecision_WhenUnderInvestigationWithAccused_ShouldTransitionToAwaitingDecision()
    {
        // Arrange
        var caseObj = DisciplinaryCase.Create("PROC-102", Guid.NewGuid(), "Título", "Descrição", DateTimeOffset.UtcNow, Guid.NewGuid());
        caseObj.Open();
        var occurrence = DisciplinaryOccurrence.Create(caseObj.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "Descrição", Guid.NewGuid(), Guid.NewGuid(), InfractionSeverity.High);
        caseObj.AddOccurrence(occurrence);
        caseObj.StartInvestigation();

        var employeeLink = DisciplinaryCaseEmployee.Create(caseObj.Id, Guid.NewGuid(), CaseEmployeeRole.Accused, true);
        caseObj.AddEmployee(employeeLink);

        _caseRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        var validator = new SubmitCaseForDecisionValidator();
        var useCase = new SubmitCaseForDecisionUseCase(_caseRepositoryMock.Object, _contextMock.Object, validator);

        var request = new SubmitCaseForDecisionRequest(caseObj.Id, Guid.NewGuid());

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(DisciplinaryCaseStatus.AwaitingDecision);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveDecision_WhenAwaitingDecisionAndDecisionSubmitted_ShouldApprove()
    {
        // Arrange
        var caseObj = DisciplinaryCase.Create("PROC-103", Guid.NewGuid(), "Título", "Descrição", DateTimeOffset.UtcNow, Guid.NewGuid());
        caseObj.Open();
        caseObj.AddOccurrence(DisciplinaryOccurrence.Create(caseObj.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "Descrição", Guid.NewGuid(), Guid.NewGuid(), InfractionSeverity.Moderate));
        caseObj.StartInvestigation();
        caseObj.AddEmployee(DisciplinaryCaseEmployee.Create(caseObj.Id, Guid.NewGuid(), CaseEmployeeRole.Accused, true));
        caseObj.SubmitForDecision();

        var decision = DisciplinaryDecision.Create(caseObj.Id, DecisionType.FormalWarning, "Justificativa da advertência", Guid.NewGuid(), DateTimeOffset.UtcNow);
        caseObj.RegisterDecision(decision);
        caseObj.SubmitDecisionForApproval();

        _caseRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        var validator = new ApproveDecisionValidator();
        var useCase = new ApproveDecisionUseCase(_caseRepositoryMock.Object, _contextMock.Object, validator);

        var request = new ApproveDecisionRequest(caseObj.Id, Guid.NewGuid());

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(DisciplinaryCaseStatus.Decided);
        result.Data.DecisionStatus.Should().Be(DecisionStatus.Approved);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectDecision_WhenAwaitingDecisionAndDecisionPending_ShouldReject()
    {
        // Arrange
        var caseObj = DisciplinaryCase.Create("PROC-104", Guid.NewGuid(), "Título", "Descrição", DateTimeOffset.UtcNow, Guid.NewGuid());
        caseObj.Open();
        caseObj.AddOccurrence(DisciplinaryOccurrence.Create(caseObj.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "Descrição", Guid.NewGuid(), Guid.NewGuid(), InfractionSeverity.Moderate));
        caseObj.StartInvestigation();
        caseObj.AddEmployee(DisciplinaryCaseEmployee.Create(caseObj.Id, Guid.NewGuid(), CaseEmployeeRole.Accused, true));
        caseObj.SubmitForDecision();

        var decision = DisciplinaryDecision.Create(caseObj.Id, DecisionType.Suspension, "Suspender 3 dias", Guid.NewGuid(), DateTimeOffset.UtcNow);
        caseObj.RegisterDecision(decision);
        caseObj.SubmitDecisionForApproval();

        _caseRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        var validator = new RejectDecisionValidator();
        var useCase = new RejectDecisionUseCase(_caseRepositoryMock.Object, _contextMock.Object, validator);

        var request = new RejectDecisionRequest(caseObj.Id, Guid.NewGuid(), "Falta de fundamentação probatória.");

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.DecisionStatus.Should().Be(DecisionStatus.Rejected);
        result.Data.Reason.Should().Be("Falta de fundamentação probatória.");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDisciplinaryCases_ShouldReturnPagedResult()
    {
        // Arrange
        var query = new GetDisciplinaryCasesQuery(PageNumber: 1, PageSize: 10);
        var items = new List<DisciplinaryCaseSummaryDto>
        {
            new DisciplinaryCaseSummaryDto(
                Guid.NewGuid(), "PROC-001", Guid.NewGuid(), "Caso 1",
                DisciplinaryCaseStatus.Open, DisciplinaryCasePriority.Normal,
                DateTimeOffset.UtcNow, Guid.NewGuid(), null, null, null, 1, 1)
        };
        var paged = new PagedResult<DisciplinaryCaseSummaryDto>(items, 1, 10, 1);

        _caseRepositoryMock.Setup(r => r.GetPagedAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var validator = new GetDisciplinaryCasesValidator();
        var useCase = new GetDisciplinaryCasesUseCase(_caseRepositoryMock.Object, validator);

        // Act
        var result = await useCase.ExecuteAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
    }
}
