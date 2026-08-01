using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using Xunit;

namespace SIGH.UnitTests.Disciplinary.UseCases;

public class DisciplinaryCaseUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IDisciplinaryCaseRepository> _caseRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    private readonly CreateDisciplinaryCaseUseCase _createUseCase;
    private readonly GetDisciplinaryCaseByIdUseCase _getByIdUseCase;
    private readonly CancelDisciplinaryCaseUseCase _cancelUseCase;
    private readonly ConcludeDisciplinaryCaseUseCase _concludeUseCase;

    private readonly DateTimeOffset _now = new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    public DisciplinaryCaseUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _caseRepositoryMock = new Mock<IDisciplinaryCaseRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(_now);

        _createUseCase = new CreateDisciplinaryCaseUseCase(_contextMock.Object, _caseRepositoryMock.Object, _dateTimeProviderMock.Object);
        _getByIdUseCase = new GetDisciplinaryCaseByIdUseCase(_caseRepositoryMock.Object);
        _cancelUseCase = new CancelDisciplinaryCaseUseCase(_contextMock.Object, _caseRepositoryMock.Object, _dateTimeProviderMock.Object);
        _concludeUseCase = new ConcludeDisciplinaryCaseUseCase(_contextMock.Object, _caseRepositoryMock.Object, _dateTimeProviderMock.Object);
    }

    [Fact]
    public async Task CreateDisciplinaryCase_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var responsibleEmployeeId = Guid.NewGuid();
        var dueDate = _now.AddDays(10);
        var request = new CreateDisciplinaryCaseRequest(
            "PROC-2026-001",
            companyId,
            "Processo Disciplinar Atraso",
            "Descrição do caso",
            userId,
            DisciplinaryCasePriority.High,
            responsibleEmployeeId,
            dueDate);

        _caseRepositoryMock.Setup(r => r.ExistsByCaseNumberAsync(companyId, request.CaseNumber, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _createUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CaseNumber.Should().Be("PROC-2026-001");
        result.Data.Status.Should().Be(DisciplinaryCaseStatus.Draft);
        result.Data.Priority.Should().Be(DisciplinaryCasePriority.High);
        result.Data.OpenedAt.Should().Be(_now);
        result.Data.OpenedByUserId.Should().Be(userId);
        result.Data.ResponsibleEmployeeId.Should().Be(responsibleEmployeeId);
        result.Data.DueDate.Should().Be(dueDate);

        _caseRepositoryMock.Verify(r => r.AddAsync(
            It.Is<DisciplinaryCase>(c =>
                c.OpenedAt == _now &&
                c.OpenedByUserId == userId &&
                c.Status == DisciplinaryCaseStatus.Draft &&
                c.Priority == DisciplinaryCasePriority.High &&
                c.ResponsibleEmployeeId == responsibleEmployeeId &&
                c.DueDate == dueDate),
            It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDisciplinaryCase_WithDuplicateNumberInCompany_ShouldReturnFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateDisciplinaryCaseRequest("PROC-2026-001", companyId, "Processo Duplicado", "Descrição", userId);

        _caseRepositoryMock.Setup(r => r.ExistsByCaseNumberAsync(companyId, request.CaseNumber, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _createUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(DisciplinaryErrors.DuplicateCaseNumber);

        _caseRepositoryMock.Verify(r => r.AddAsync(It.IsAny<DisciplinaryCase>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDisciplinaryCaseById_WhenExists_ShouldReturnCaseWithDetails()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var caseObj = DisciplinaryCase.Create("PROC-001", companyId, "Título", "Descrição", userId, _now);

        _caseRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        // Act
        var result = await _getByIdUseCase.ExecuteAsync(caseObj.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CaseNumber.Should().Be("PROC-001");
    }

    [Fact]
    public async Task CancelDisciplinaryCase_WhenInDraft_ShouldCancelSuccessfully()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var caseObj = DisciplinaryCase.Create("PROC-CANCEL", companyId, "Título", "Descrição", userId, _now);

        _caseRepositoryMock.Setup(r => r.GetByIdAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        var request = new CancelDisciplinaryCaseRequest(caseObj.Id, userId, "Abertura por engano");

        // Act
        var result = await _cancelUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(DisciplinaryCaseStatus.Cancelled);
        result.Data.Reason.Should().Be("Abertura por engano");

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConcludeDisciplinaryCase_WhenInAnalysis_ShouldConcludeSuccessfully()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var caseObj = DisciplinaryCase.Create("PROC-CONCLUDE", companyId, "Título", "Descrição", userId, _now);
        caseObj.TransitionStatus(DisciplinaryCaseStatus.UnderAnalysis, userId, _now);

        _caseRepositoryMock.Setup(r => r.GetByIdAsync(caseObj.Id, It.IsAny<CancellationToken>())).ReturnsAsync(caseObj);

        var request = new ConcludeDisciplinaryCaseRequest(caseObj.Id, userId, "Resumo da conclusão do processo.");

        // Act
        var result = await _concludeUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(DisciplinaryCaseStatus.Concluded);
        result.Data.FinalSummary.Should().Be("Resumo da conclusão do processo.");

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateDisciplinaryCaseValidator_ShouldValidateFields()
    {
        // Arrange
        var validator = new CreateDisciplinaryCaseValidator();
        var invalid = new CreateDisciplinaryCaseRequest("", Guid.Empty, "", "", Guid.Empty);

        // Act
        var validation = validator.Validate(invalid);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.PropertyName == "CaseNumber");
        validation.Errors.Should().Contain(e => e.PropertyName == "CompanyId");
        validation.Errors.Should().Contain(e => e.PropertyName == "Title");
        validation.Errors.Should().Contain(e => e.PropertyName == "Description");
        validation.Errors.Should().Contain(e => e.PropertyName == "CreatedByUserId");
    }
}
