using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGH.Api.Controllers;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.ChangeOrganizationalAssignment;
using SIGH.Application.Employees.ChangeStatus;
using SIGH.Application.Employees.ChangeSupervisor;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.CreateEmployee;
using SIGH.Application.Employees.GetEmployeeById;
using SIGH.Application.Employees.GetEmployees;
using SIGH.Application.Employees.LinkUser;
using SIGH.Application.Employees.ReactivateEmployee;
using SIGH.Application.Employees.TerminateEmployee;
using SIGH.Application.Employees.UnlinkUser;
using SIGH.Application.Employees.UpdateEmployee;
using SIGH.Domain.Employees.Enums;
using SIGH.Infrastructure.Authorization;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class EmployeesControllerTests
{
    private readonly Mock<ICreateEmployeeUseCase> _createEmployeeUseCaseMock = new();
    private readonly Mock<IGetEmployeeByIdUseCase> _getEmployeeByIdUseCaseMock = new();
    private readonly Mock<IGetEmployeesUseCase> _getEmployeesUseCaseMock = new();
    private readonly Mock<IUpdateEmployeeUseCase> _updateEmployeeUseCaseMock = new();
    private readonly Mock<IChangeEmployeeOrganizationalAssignmentUseCase> _changeOrganizationalAssignmentUseCaseMock = new();
    private readonly Mock<IChangeEmployeeSupervisorUseCase> _changeSupervisorUseCaseMock = new();
    private readonly Mock<IChangeEmployeeStatusUseCase> _changeStatusUseCaseMock = new();
    private readonly Mock<ITerminateEmployeeUseCase> _terminateEmployeeUseCaseMock = new();
    private readonly Mock<IReactivateEmployeeUseCase> _reactivateEmployeeUseCaseMock = new();
    private readonly Mock<ILinkEmployeeUserUseCase> _linkEmployeeUserUseCaseMock = new();
    private readonly Mock<IUnlinkEmployeeUserUseCase> _unlinkEmployeeUserUseCaseMock = new();

    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        _controller = new EmployeesController(
            _createEmployeeUseCaseMock.Object,
            _getEmployeeByIdUseCaseMock.Object,
            _getEmployeesUseCaseMock.Object,
            _updateEmployeeUseCaseMock.Object,
            _changeOrganizationalAssignmentUseCaseMock.Object,
            _changeSupervisorUseCaseMock.Object,
            _changeStatusUseCaseMock.Object,
            _terminateEmployeeUseCaseMock.Object,
            _reactivateEmployeeUseCaseMock.Object,
            _linkEmployeeUserUseCaseMock.Object,
            _unlinkEmployeeUserUseCaseMock.Object
        );
    }

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = new CreateEmployeeRequest(
            CompanyId: Guid.NewGuid(),
            EmployeeNumber: "EMP001",
            FullName: "João Silva",
            Cpf: "12345678901",
            AdmissionDate: new DateOnly(2023, 1, 1),
            JobTitleId: Guid.NewGuid(),
            ManagementUnitId: Guid.NewGuid()
        );

        var response = new CreateEmployeeResponse(
            Id: Guid.NewGuid(),
            CompanyId: request.CompanyId,
            EmployeeNumber: request.EmployeeNumber,
            FullName: request.FullName,
            SocialName: null,
            CpfMasked: "***.456.789-**",
            AdmissionDate: request.AdmissionDate,
            Status: EmployeeStatus.PendingAdmission,
            JobTitleId: request.JobTitleId,
            ManagementUnitId: request.ManagementUnitId,
            DepartmentId: null,
            SupervisorId: null,
            UserId: null,
            HasSystemAccess: false
        );

        _createEmployeeUseCaseMock
            .Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateEmployeeResponse>.Ok(response, "Criado com sucesso."));

        // Act
        var actionResult = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = actionResult.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(EmployeesController.GetById));

        var resultValue = createdResult.Value.Should().BeOfType<Result<CreateEmployeeResponse>>().Subject;
        resultValue.Success.Should().BeTrue();
        resultValue.Data!.Id.Should().Be(response.Id);
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateEmployeeRequest(
            CompanyId: Guid.NewGuid(),
            EmployeeNumber: "EMP001",
            FullName: "João Silva",
            Cpf: "12345678901",
            AdmissionDate: new DateOnly(2023, 1, 1),
            JobTitleId: Guid.NewGuid(),
            ManagementUnitId: Guid.NewGuid()
        );

        _createEmployeeUseCaseMock
            .Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateEmployeeResponse>.Failure("CPF já cadastrado.", EmployeeErrors.CpfAlreadyExists));

        // Act
        var actionResult = await _controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var resultValue = badRequestResult.Value.Should().BeOfType<Result<CreateEmployeeResponse>>().Subject;
        resultValue.Success.Should().BeFalse();
        resultValue.ErrorCode.Should().Be(EmployeeErrors.CpfAlreadyExists);
    }

    [Fact]
    public async Task Create_WhenCompanyNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var request = new CreateEmployeeRequest(
            CompanyId: Guid.NewGuid(),
            EmployeeNumber: "EMP001",
            FullName: "João Silva",
            Cpf: "12345678901",
            AdmissionDate: new DateOnly(2023, 1, 1),
            JobTitleId: Guid.NewGuid(),
            ManagementUnitId: Guid.NewGuid()
        );

        _createEmployeeUseCaseMock
            .Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateEmployeeResponse>.Failure("Empresa não encontrada.", EmployeeErrors.CompanyNotFound));

        // Act
        var actionResult = await _controller.Create(request, CancellationToken.None);

        // Assert
        var notFoundResult = actionResult.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetById_WhenEmployeeExists_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var response = new GetEmployeeByIdResponse(
            Id: id,
            CompanyId: Guid.NewGuid(),
            CompanyName: "Empresa Teste",
            EmployeeNumber: "EMP001",
            FullName: "João Silva",
            SocialName: null,
            CpfMasked: "***.456.789-**",
            AdmissionDate: new DateOnly(2023, 1, 1),
            BirthDate: null,
            Status: EmployeeStatus.Active,
            JobTitleId: Guid.NewGuid(),
            JobTitleName: "Analista",
            ManagementUnitId: Guid.NewGuid(),
            ManagementUnitName: "Unidade SP",
            DepartmentId: null,
            DepartmentName: null,
            SupervisorId: null,
            SupervisorName: null,
            UserId: null,
            HasSystemAccess: false,
            CorporateEmail: "joao.silva@empresa.com",
            PersonalEmail: null,
            MobileNumber: null,
            TerminationDate: null,
            TerminationReason: null,
            Notes: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: null
        );

        _getEmployeeByIdUseCaseMock
            .Setup(x => x.ExecuteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GetEmployeeByIdResponse>.Ok(response));

        // Act
        var actionResult = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var resultValue = okResult.Value.Should().BeOfType<Result<GetEmployeeByIdResponse>>().Subject;
        resultValue.Success.Should().BeTrue();
        resultValue.Data!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_WhenEmployeeNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _getEmployeeByIdUseCaseMock
            .Setup(x => x.ExecuteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GetEmployeeByIdResponse>.Failure("Funcionário não encontrado.", EmployeeErrors.NotFound));

        // Act
        var actionResult = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var notFoundResult = actionResult.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetList_ShouldReturnOkWithPagedList()
    {
        // Arrange
        var query = new GetEmployeesQuery(PageNumber: 1, PageSize: 10);
        var pagedResult = new PagedResult<EmployeeListItemResponse>(
            Items: new List<EmployeeListItemResponse>(),
            TotalCount: 0,
            PageNumber: 1,
            PageSize: 10
        );

        _getEmployeesUseCaseMock
            .Setup(x => x.ExecuteAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<EmployeeListItemResponse>>.Ok(pagedResult));

        // Act
        var actionResult = await _controller.GetList(query, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateEmployeeRequest(
            EmployeeId: id,
            EmployeeNumber: "EMP001",
            FullName: "João Silva Alterado",
            Cpf: "12345678901",
            JobTitleId: Guid.NewGuid(),
            ManagementUnitId: Guid.NewGuid()
        );

        var response = new UpdateEmployeeResponse(
            Id: id,
            CompanyId: Guid.NewGuid(),
            EmployeeNumber: request.EmployeeNumber,
            FullName: request.FullName,
            SocialName: null,
            CpfMasked: "***.456.789-**",
            AdmissionDate: new DateOnly(2023, 1, 1),
            Status: EmployeeStatus.Active,
            JobTitleId: request.JobTitleId,
            ManagementUnitId: request.ManagementUnitId,
            DepartmentId: null,
            CorporateEmail: null,
            PersonalEmail: null,
            MobileNumber: null,
            Notes: null
        );

        _updateEmployeeUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<UpdateEmployeeRequest>(r => r.EmployeeId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UpdateEmployeeResponse>.Ok(response));

        // Act
        var actionResult = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ChangeOrganizationalAssignment_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ChangeEmployeeOrganizationalAssignmentRequest(id, Guid.NewGuid(), Guid.NewGuid());

        _changeOrganizationalAssignmentUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<ChangeEmployeeOrganizationalAssignmentRequest>(r => r.EmployeeId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.SuccessResult("Alocação alterada com sucesso."));

        // Act
        var actionResult = await _controller.ChangeOrganizationalAssignment(id, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ChangeSupervisor_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ChangeEmployeeSupervisorRequest(id, Guid.NewGuid());

        _changeSupervisorUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<ChangeEmployeeSupervisorRequest>(r => r.EmployeeId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.SuccessResult("Supervisor alterado com sucesso."));

        // Act
        var actionResult = await _controller.ChangeSupervisor(id, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ChangeStatus_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ChangeEmployeeStatusRequest(id, EmployeeStatus.Active);

        _changeStatusUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<ChangeEmployeeStatusRequest>(r => r.EmployeeId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.SuccessResult("Status alterado com sucesso."));

        // Act
        var actionResult = await _controller.ChangeStatus(id, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Terminate_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new TerminateEmployeeRequest(id, new DateOnly(2023, 12, 31), "Pedido de demissão");
        var response = new TerminateEmployeeResponse(id, EmployeeStatus.Terminated, request.TerminationDate, request.TerminationReason);

        _terminateEmployeeUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<TerminateEmployeeRequest>(r => r.EmployeeId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TerminateEmployeeResponse>.Ok(response));

        // Act
        var actionResult = await _controller.Terminate(id, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Reactivate_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ReactivateEmployeeRequest(id, new DateOnly(2024, 1, 15));

        _reactivateEmployeeUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<ReactivateEmployeeRequest>(r => r.EmployeeId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.SuccessResult("Funcionário readmitido."));

        // Act
        var actionResult = await _controller.Reactivate(id, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task LinkUser_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new LinkEmployeeUserRequest(id, Guid.NewGuid());

        _linkEmployeeUserUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<LinkEmployeeUserRequest>(r => r.EmployeeId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.SuccessResult("Usuário vinculado com sucesso."));

        // Act
        var actionResult = await _controller.LinkUser(id, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task UnlinkUser_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UnlinkEmployeeUserRequest(id);

        _unlinkEmployeeUserUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<UnlinkEmployeeUserRequest>(r => r.EmployeeId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.SuccessResult("Usuário desvinculado com sucesso."));

        // Act
        var actionResult = await _controller.UnlinkUser(id, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData(nameof(EmployeesController.Create), "Employees.Create")]
    [InlineData(nameof(EmployeesController.GetById), "Employees.View")]
    [InlineData(nameof(EmployeesController.GetList), "Employees.View")]
    [InlineData(nameof(EmployeesController.Update), "Employees.Update")]
    [InlineData(nameof(EmployeesController.ChangeOrganizationalAssignment), "Employees.ChangeOrganizationalAssignment")]
    [InlineData(nameof(EmployeesController.ChangeSupervisor), "Employees.ChangeSupervisor")]
    [InlineData(nameof(EmployeesController.ChangeStatus), "Employees.ChangeStatus")]
    [InlineData(nameof(EmployeesController.Terminate), "Employees.Terminate")]
    [InlineData(nameof(EmployeesController.Reactivate), "Employees.Reactivate")]
    [InlineData(nameof(EmployeesController.LinkUser), "Employees.LinkUser")]
    [InlineData(nameof(EmployeesController.UnlinkUser), "Employees.UnlinkUser")]
    public void ControllerMethods_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        // Arrange
        var method = typeof(EmployeesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == methodName);

        method.Should().NotBeNull($"Método {methodName} deve existir em EmployeesController");

        // Act
        var permissionAttr = method!.GetCustomAttribute<PermissionAttribute>();

        // Assert
        permissionAttr.Should().NotBeNull($"Método {methodName} deve ter PermissionAttribute aplicado");
        permissionAttr!.Permission.Should().Be(expectedPermission);
    }
}
