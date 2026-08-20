using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes;
using SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using Xunit;

namespace SIGH.UnitTests.Disciplinary.UseCases;

public class InfractionTypeManagementUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IInfractionTypeRepository> _repositoryMock;

    public InfractionTypeManagementUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _repositoryMock = new Mock<IInfractionTypeRepository>();
    }

    [Fact]
    public async Task UpdateInfractionType_WithValidRequest_ShouldUpdateAndReturnSuccess()
    {
        // Arrange
        var entity = InfractionType.Create("INF-001", "Nome Antigo", InfractionSeverity.Low);
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var validator = new UpdateInfractionTypeValidator();
        var useCase = new UpdateInfractionTypeUseCase(_repositoryMock.Object, _contextMock.Object, validator);

        var request = new UpdateInfractionTypeRequest(
            entity.Id,
            "Nome Atualizado",
            InfractionSeverity.High,
            true,
            false,
            "Descrição Nova",
            "Art. 482 b");

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Nome Atualizado");
        result.Data.DefaultSeverity.Should().Be(InfractionSeverity.High);
        result.Data.RequiresFormalInvestigation.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateInfractionType_WhenInactive_ShouldActivate()
    {
        // Arrange
        var entity = InfractionType.Create("INF-002", "Infração Teste", InfractionSeverity.Moderate);
        entity.Deactivate();

        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var validator = new ActivateInfractionTypeValidator();
        var useCase = new ActivateInfractionTypeUseCase(_repositoryMock.Object, _contextMock.Object, validator);

        var request = new ActivateInfractionTypeRequest(entity.Id);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.IsActive.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateInfractionType_WhenActive_ShouldDeactivate()
    {
        // Arrange
        var entity = InfractionType.Create("INF-003", "Infração Teste 2", InfractionSeverity.High);

        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var validator = new DeactivateInfractionTypeValidator();
        var useCase = new DeactivateInfractionTypeUseCase(_repositoryMock.Object, _contextMock.Object, validator);

        var request = new DeactivateInfractionTypeRequest(entity.Id);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.IsActive.Should().BeFalse();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInfractionTypes_ShouldReturnPagedResult()
    {
        // Arrange
        var query = new GetInfractionTypesQuery(PageNumber: 1, PageSize: 10);
        var items = new List<InfractionTypeDto>
        {
            new InfractionTypeDto(Guid.NewGuid(), "INF-001", "Atraso", InfractionSeverity.Low, false, false, "Desc", "Art", true)
        };
        var paged = new PagedResult<InfractionTypeDto>(items, 1, 10, 1);

        _repositoryMock.Setup(r => r.GetPagedAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var validator = new GetInfractionTypesValidator();
        var useCase = new GetInfractionTypesUseCase(_repositoryMock.Object, validator);

        // Act
        var result = await useCase.ExecuteAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
    }
}
