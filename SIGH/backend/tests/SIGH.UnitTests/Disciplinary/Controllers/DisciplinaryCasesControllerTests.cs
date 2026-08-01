using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGH.Api.Controllers;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence;
using SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;
using SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases;
using SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision;
using SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation;
using SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Infrastructure.Authorization;
using Xunit;

namespace SIGH.UnitTests.Disciplinary.Controllers;

public class DisciplinaryCasesControllerTests
{
    private readonly Mock<ICreateDisciplinaryCaseUseCase> _createUseCaseMock = new();
    private readonly Mock<IGetDisciplinaryCaseByIdUseCase> _getByIdUseCaseMock = new();
    private readonly Mock<IGetDisciplinaryCasesUseCase> _getCasesUseCaseMock = new();
    private readonly Mock<IOpenCaseUseCase> _openCaseUseCaseMock = new();
    private readonly Mock<IStartInvestigationUseCase> _startInvestigationUseCaseMock = new();
    private readonly Mock<ISubmitCaseForDecisionUseCase> _submitCaseForDecisionUseCaseMock = new();
    private readonly Mock<IApproveDecisionUseCase> _approveDecisionUseCaseMock = new();
    private readonly Mock<IRejectDecisionUseCase> _rejectDecisionUseCaseMock = new();
    private readonly Mock<IAddOccurrenceUseCase> _addOccurrenceUseCaseMock = new();
    private readonly Mock<IAddEmployeeToCaseUseCase> _addEmployeeToCaseUseCaseMock = new();
    private readonly Mock<IAddEvidenceUseCase> _addEvidenceUseCaseMock = new();
    private readonly Mock<IRecordDecisionUseCase> _recordDecisionUseCaseMock = new();
    private readonly Mock<IApplyMeasureUseCase> _applyMeasureUseCaseMock = new();
    private readonly Mock<ICancelDisciplinaryCaseUseCase> _cancelCaseUseCaseMock = new();
    private readonly Mock<IConcludeDisciplinaryCaseUseCase> _concludeCaseUseCaseMock = new();

    private readonly DisciplinaryCasesController _controller;

    public DisciplinaryCasesControllerTests()
    {
        _controller = new DisciplinaryCasesController(
            _createUseCaseMock.Object,
            _getByIdUseCaseMock.Object,
            _getCasesUseCaseMock.Object,
            _openCaseUseCaseMock.Object,
            _startInvestigationUseCaseMock.Object,
            _submitCaseForDecisionUseCaseMock.Object,
            _approveDecisionUseCaseMock.Object,
            _rejectDecisionUseCaseMock.Object,
            _addOccurrenceUseCaseMock.Object,
            _addEmployeeToCaseUseCaseMock.Object,
            _addEvidenceUseCaseMock.Object,
            _recordDecisionUseCaseMock.Object,
            _applyMeasureUseCaseMock.Object,
            _cancelCaseUseCaseMock.Object,
            _concludeCaseUseCaseMock.Object);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = new CreateDisciplinaryCaseRequest(
            CaseNumber: "PROC-2026-001",
            CompanyId: Guid.NewGuid(),
            Title: "Insubordinação grave",
            Description: "Descrição do processo disciplinar.",
            CreatedByUserId: Guid.NewGuid());

        var response = new CreateDisciplinaryCaseResponse(
            Id: Guid.NewGuid(),
            CaseNumber: request.CaseNumber,
            CompanyId: request.CompanyId,
            Title: request.Title,
            Status: DisciplinaryCaseStatus.Draft,
            Priority: CasePriority.Medium,
            OpenedAt: DateTimeOffset.UtcNow,
            OpenedByUserId: request.CreatedByUserId,
            ResponsibleEmployeeId: null,
            DueDate: null);

        _createUseCaseMock
            .Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateDisciplinaryCaseResponse>.Ok(response));

        // Act
        var actionResult = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = actionResult.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(DisciplinaryCasesController.GetById));

        var resultValue = createdResult.Value.Should().BeOfType<Result<CreateDisciplinaryCaseResponse>>().Subject;
        resultValue.Success.Should().BeTrue();
        resultValue.Data!.Id.Should().Be(response.Id);
    }

    [Fact]
    public async Task GetById_WhenExists_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var response = new GetDisciplinaryCaseByIdResponse(
            Id: id,
            CaseNumber: "PROC-001",
            CompanyId: Guid.NewGuid(),
            Title: "Título",
            Description: null,
            Status: DisciplinaryCaseStatus.Open,
            Priority: CasePriority.Medium,
            OpenedAt: DateTimeOffset.UtcNow,
            OpenedByUserId: null,
            ResponsibleEmployeeId: null,
            DueDate: null,
            ClosedAt: null,
            ConcludedByUserId: null,
            FinalSummary: null,
            Occurrences: new List<DisciplinaryOccurrenceDto>(),
            Employees: new List<CaseEmployeeDto>(),
            Evidences: new List<EvidenceDto>(),
            Decisions: new List<DisciplinaryDecisionDto>(),
            Measures: new List<DisciplinaryMeasureDto>());

        _getByIdUseCaseMock
            .Setup(x => x.ExecuteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GetDisciplinaryCaseByIdResponse>.Ok(response));

        // Act
        var actionResult = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _getByIdUseCaseMock
            .Setup(x => x.ExecuteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GetDisciplinaryCaseByIdResponse>.Failure("Processo disciplinar não encontrado.", "NotFound"));

        // Act
        var actionResult = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var notFoundResult = actionResult.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task OpenCase_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new OpenCaseResponse(caseId, DisciplinaryCaseStatus.Open, DateTimeOffset.UtcNow);

        _openCaseUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<OpenCaseRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpenCaseResponse>.Ok(response));

        // Act
        var actionResult = await _controller.Open(caseId, new OpenCaseRequest(caseId, userId), CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task StartInvestigation_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new StartInvestigationResponse(caseId, DisciplinaryCaseStatus.UnderInvestigation);

        _startInvestigationUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<StartInvestigationRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StartInvestigationResponse>.Ok(response));

        // Act
        var actionResult = await _controller.StartInvestigation(caseId, new StartInvestigationRequest(caseId, userId), CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task SubmitForDecision_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new SubmitCaseForDecisionResponse(caseId, DisciplinaryCaseStatus.AwaitingDecision);

        _submitCaseForDecisionUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<SubmitCaseForDecisionRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SubmitCaseForDecisionResponse>.Ok(response));

        // Act
        var actionResult = await _controller.SubmitForDecision(caseId, new SubmitCaseForDecisionRequest(caseId, userId), CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ApproveDecision_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new ApproveDecisionResponse(caseId, Guid.NewGuid(), DisciplinaryCaseStatus.Decided, DecisionStatus.Approved, DateTimeOffset.UtcNow);

        _approveDecisionUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<ApproveDecisionRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApproveDecisionResponse>.Ok(response));

        // Act
        var actionResult = await _controller.ApproveDecision(caseId, new ApproveDecisionRequest(caseId, userId), CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task RejectDecision_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new RejectDecisionRequest(caseId, userId, "Falta fundamentação legal.");
        var response = new RejectDecisionResponse(caseId, Guid.NewGuid(), DisciplinaryCaseStatus.UnderInvestigation, DecisionStatus.Rejected, request.Reason);

        _rejectDecisionUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<RejectDecisionRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RejectDecisionResponse>.Ok(response));

        // Act
        var actionResult = await _controller.RejectDecision(caseId, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task AddOccurrence_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var request = new AddOccurrenceRequest(caseId, DateTimeOffset.UtcNow, "Falta não justificada", Guid.NewGuid(), Guid.NewGuid(), InfractionSeverity.Medium);
        var response = new AddOccurrenceResponse(Guid.NewGuid(), caseId, request.InfractionTypeId, request.Severity, OccurrenceStatus.Reported);

        _addOccurrenceUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<AddOccurrenceRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AddOccurrenceResponse>.Ok(response));

        // Act
        var actionResult = await _controller.AddOccurrence(caseId, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task AddEmployee_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var request = new AddEmployeeToCaseRequest(caseId, Guid.NewGuid(), CaseEmployeeRole.Accused, true);
        var response = new AddEmployeeToCaseResponse(Guid.NewGuid(), caseId, request.EmployeeId, request.Role, request.IsPrimarySubject);

        _addEmployeeToCaseUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<AddEmployeeToCaseRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AddEmployeeToCaseResponse>.Ok(response));

        // Act
        var actionResult = await _controller.AddEmployee(caseId, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task AddEvidence_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var request = new AddEvidenceRequest(
            caseId,
            EvidenceType.Document,
            "Relatório de catraca",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            StorageReference: "evidences/relatorio-catraca.pdf",
            OriginalFileName: "relatorio-catraca.pdf",
            ContentType: "application/pdf",
            FileSize: 1024);
        var response = new AddEvidenceResponse(Guid.NewGuid(), caseId, request.EvidenceType, EvidenceStatus.PendingVerification, request.CollectedAt);

        _addEvidenceUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<AddEvidenceRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AddEvidenceResponse>.Ok(response));

        // Act
        var actionResult = await _controller.AddEvidence(caseId, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task RecordDecision_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var request = new RecordDecisionRequest(
            caseId,
            DecisionType.Suspension,
            "Suspensão por três dias.",
            "A apuração confirmou os fatos que fundamentam a suspensão.",
            Guid.NewGuid());
        var response = new RecordDecisionResponse(
            Guid.NewGuid(),
            caseId,
            request.DecisionType,
            request.Summary,
            request.Reasoning,
            DecisionStatus.Draft,
            DateTimeOffset.UtcNow,
            request.DecidedByUserId,
            null,
            null);

        _recordDecisionUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<RecordDecisionRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RecordDecisionResponse>.Ok(response));

        // Act
        var actionResult = await _controller.RecordDecision(caseId, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ApplyMeasure_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var request = new ApplyMeasureRequest(caseId, Guid.NewGuid(), Guid.NewGuid(), DisciplinaryMeasureType.Suspension, "Suspenso por 3 dias", DateTimeOffset.UtcNow, Guid.NewGuid());
        var response = new ApplyMeasureResponse(
            Guid.NewGuid(),
            caseId,
            request.DisciplinaryDecisionId,
            request.EmployeeId,
            request.MeasureType,
            request.Reason,
            request.EffectiveFrom,
            request.EffectiveUntil,
            request.AppliedByUserId,
            DateTimeOffset.UtcNow,
            DisciplinaryMeasureStatus.Applied,
            request.Notes);

        _applyMeasureUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<ApplyMeasureRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApplyMeasureResponse>.Ok(response));

        // Act
        var actionResult = await _controller.ApplyMeasure(caseId, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Cancel_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var request = new CancelDisciplinaryCaseRequest(caseId, Guid.NewGuid(), "Aviso prévio já cumprido.");
        var response = new CancelDisciplinaryCaseResponse(caseId, DisciplinaryCaseStatus.Cancelled, DateTimeOffset.UtcNow, request.Reason);

        _cancelCaseUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<CancelDisciplinaryCaseRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CancelDisciplinaryCaseResponse>.Ok(response));

        // Act
        var actionResult = await _controller.Cancel(caseId, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Conclude_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var request = new ConcludeDisciplinaryCaseRequest(caseId, Guid.NewGuid(), "Processo concluído com cumprimento da penalidade.");
        var response = new ConcludeDisciplinaryCaseResponse(caseId, DisciplinaryCaseStatus.Concluded, DateTimeOffset.UtcNow, request.FinalSummary);

        _concludeCaseUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<ConcludeDisciplinaryCaseRequest>(r => r.DisciplinaryCaseId == caseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConcludeDisciplinaryCaseResponse>.Ok(response));

        // Act
        var actionResult = await _controller.Conclude(caseId, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData(nameof(DisciplinaryCasesController.Create), "Disciplinary.Cases.Create")]
    [InlineData(nameof(DisciplinaryCasesController.GetById), "Disciplinary.Cases.View")]
    [InlineData(nameof(DisciplinaryCasesController.GetPaged), "Disciplinary.Cases.View")]
    [InlineData(nameof(DisciplinaryCasesController.Open), "Disciplinary.Cases.Open")]
    [InlineData(nameof(DisciplinaryCasesController.StartInvestigation), "Disciplinary.Cases.StartInvestigation")]
    [InlineData(nameof(DisciplinaryCasesController.SubmitForDecision), "Disciplinary.Cases.SubmitForDecision")]
    [InlineData(nameof(DisciplinaryCasesController.ApproveDecision), "Disciplinary.Cases.ApproveDecision")]
    [InlineData(nameof(DisciplinaryCasesController.RejectDecision), "Disciplinary.Cases.RejectDecision")]
    [InlineData(nameof(DisciplinaryCasesController.AddOccurrence), "Disciplinary.Cases.AddOccurrence")]
    [InlineData(nameof(DisciplinaryCasesController.AddEmployee), "Disciplinary.Cases.AddEmployee")]
    [InlineData(nameof(DisciplinaryCasesController.AddEvidence), "Disciplinary.Cases.AddEvidence")]
    [InlineData(nameof(DisciplinaryCasesController.RecordDecision), "Disciplinary.Cases.RecordDecision")]
    [InlineData(nameof(DisciplinaryCasesController.ApplyMeasure), "Disciplinary.Cases.ApplyMeasure")]
    [InlineData(nameof(DisciplinaryCasesController.Cancel), "Disciplinary.Cases.Cancel")]
    [InlineData(nameof(DisciplinaryCasesController.Conclude), "Disciplinary.Cases.Conclude")]
    public void ControllerMethods_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        var method = typeof(DisciplinaryCasesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == methodName);

        method.Should().NotBeNull($"Método {methodName} deve existir no controller");
        var permissionAttr = method!.GetCustomAttribute<PermissionAttribute>();
        permissionAttr.Should().NotBeNull($"Método {methodName} deve possuir PermissionAttribute");
        permissionAttr!.Permission.Should().Be(expectedPermission);
    }
}
