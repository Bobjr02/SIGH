using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGH.Api.Controllers;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypeById;
using SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes;
using SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Infrastructure.Authorization;
using Xunit;

namespace SIGH.UnitTests.Disciplinary.Controllers;

public class InfractionTypesControllerTests
{
    private readonly Mock<ICreateInfractionTypeUseCase> _createUseCaseMock = new();
    private readonly Mock<IGetInfractionTypeByIdUseCase> _getByIdUseCaseMock = new();
    private readonly Mock<IGetInfractionTypesUseCase> _getTypesUseCaseMock = new();
    private readonly Mock<IUpdateInfractionTypeUseCase> _updateUseCaseMock = new();
    private readonly Mock<IActivateInfractionTypeUseCase> _activateUseCaseMock = new();
    private readonly Mock<IDeactivateInfractionTypeUseCase> _deactivateUseCaseMock = new();

    private readonly InfractionTypesController _controller;

    public InfractionTypesControllerTests()
    {
        _controller = new InfractionTypesController(
            _createUseCaseMock.Object,
            _getByIdUseCaseMock.Object,
            _getTypesUseCaseMock.Object,
            _updateUseCaseMock.Object,
            _activateUseCaseMock.Object,
            _deactivateUseCaseMock.Object);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = new CreateInfractionTypeRequest(
            Code: "INF-010",
            Name: "Atraso Reincidente",
            DefaultSeverity: InfractionSeverity.Medium);

        var response = new CreateInfractionTypeResponse(
            Id: Guid.NewGuid(),
            Code: request.Code,
            Name: request.Name,
            DefaultSeverity: request.DefaultSeverity,
            RequiresFormalInvestigation: false,
            RequiresSuspension: false,
            Description: null,
            LegalBasis: null,
            IsActive: true);

        _createUseCaseMock
            .Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateInfractionTypeResponse>.Ok(response, "Criado com sucesso."));

        // Act
        var actionResult = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = actionResult.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(InfractionTypesController.GetById));

        var resultValue = createdResult.Value.Should().BeOfType<Result<CreateInfractionTypeResponse>>().Subject;
        resultValue.Success.Should().BeTrue();
        resultValue.Data!.Id.Should().Be(response.Id);
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateInfractionTypeRequest("INF-010", "", InfractionSeverity.Low);

        _createUseCaseMock
            .Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateInfractionTypeResponse>.Failure("O nome é obrigatório.", "ValidationError"));

        // Act
        var actionResult = await _controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GetById_WhenExists_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new InfractionTypeDto(id, "INF-001", "Insubordinação", InfractionSeverity.High, true, true, "Descrição", "Art 482", true);

        _getByIdUseCaseMock
            .Setup(x => x.ExecuteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InfractionTypeDto>.Ok(dto));

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
            .ReturnsAsync(Result<InfractionTypeDto>.Failure("Tipo de infração não encontrado.", "NotFound"));

        // Act
        var actionResult = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var notFoundResult = actionResult.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetPaged_ShouldReturnOkWithPagedList()
    {
        // Arrange
        var query = new GetInfractionTypesQuery(PageNumber: 1, PageSize: 10);
        var pagedResult = new PagedResult<InfractionTypeDto>(new List<InfractionTypeDto>(), 1, 10, 0);

        _getTypesUseCaseMock
            .Setup(x => x.ExecuteAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<InfractionTypeDto>>.Ok(pagedResult));

        // Act
        var actionResult = await _controller.GetPaged(query, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateInfractionTypeRequest(id, "Nome Novo", InfractionSeverity.High, true, false, "Desc", "Art");
        var response = new UpdateInfractionTypeResponse(id, "INF-001", "Nome Novo", InfractionSeverity.High, true, false, "Desc", "Art", true);

        _updateUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<UpdateInfractionTypeRequest>(r => r.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UpdateInfractionTypeResponse>.Ok(response));

        // Act
        var actionResult = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Activate_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var response = new ActivateInfractionTypeResponse(id, true);

        _activateUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<ActivateInfractionTypeRequest>(r => r.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivateInfractionTypeResponse>.Ok(response));

        // Act
        var actionResult = await _controller.Activate(id, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Deactivate_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var response = new DeactivateInfractionTypeResponse(id, false);

        _deactivateUseCaseMock
            .Setup(x => x.ExecuteAsync(It.Is<DeactivateInfractionTypeRequest>(r => r.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DeactivateInfractionTypeResponse>.Ok(response));

        // Act
        var actionResult = await _controller.Deactivate(id, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData(nameof(InfractionTypesController.Create), "Disciplinary.InfractionTypes.Create")]
    [InlineData(nameof(InfractionTypesController.GetById), "Disciplinary.InfractionTypes.View")]
    [InlineData(nameof(InfractionTypesController.GetPaged), "Disciplinary.InfractionTypes.View")]
    [InlineData(nameof(InfractionTypesController.Update), "Disciplinary.InfractionTypes.Update")]
    [InlineData(nameof(InfractionTypesController.Activate), "Disciplinary.InfractionTypes.Activate")]
    [InlineData(nameof(InfractionTypesController.Deactivate), "Disciplinary.InfractionTypes.Deactivate")]
    public void ControllerMethods_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        var method = typeof(InfractionTypesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == methodName);

        method.Should().NotBeNull($"Método {methodName} deve existir no controller");
        var permissionAttr = method!.GetCustomAttribute<PermissionAttribute>();
        permissionAttr.Should().NotBeNull($"Método {methodName} deve possuir PermissionAttribute");
        permissionAttr!.Permission.Should().Be(expectedPermission);
    }
}
