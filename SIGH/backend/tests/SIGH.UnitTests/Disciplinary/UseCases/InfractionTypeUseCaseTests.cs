using FluentAssertions;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;
using SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypeById;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using Xunit;

namespace SIGH.UnitTests.Disciplinary.UseCases;

public class InfractionTypeUseCaseTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IInfractionTypeRepository> _repositoryMock;
    private readonly CreateInfractionTypeUseCase _createUseCase;
    private readonly GetInfractionTypeByIdUseCase _getByIdUseCase;

    public InfractionTypeUseCaseTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _repositoryMock = new Mock<IInfractionTypeRepository>();

        _createUseCase = new CreateInfractionTypeUseCase(_contextMock.Object, _repositoryMock.Object);
        _getByIdUseCase = new GetInfractionTypeByIdUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateInfractionType_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new CreateInfractionTypeRequest("INF-001", "Atraso Injustificado", InfractionSeverity.Low, "Descrição", "Art. 482 CLT");
        _repositoryMock.Setup(r => r.ExistsByCodeAsync(request.Code, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _createUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Code.Should().Be("INF-001");
        result.Data.Name.Should().Be("Atraso Injustificado");
        result.Data.DefaultSeverity.Should().Be(InfractionSeverity.Low);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<InfractionType>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInfractionType_WithDuplicateCode_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateInfractionTypeRequest("INF-001", "Atraso Injustificado", InfractionSeverity.Low);
        _repositoryMock.Setup(r => r.ExistsByCodeAsync(request.Code, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _createUseCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(DisciplinaryErrors.DuplicateInfractionCode);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<InfractionType>(), It.IsAny<CancellationToken>()), Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetInfractionTypeById_WhenExists_ShouldReturnItem()
    {
        // Arrange
        var entity = InfractionType.Create("INF-100", "Insubordinação", InfractionSeverity.High, "Descrição", "Artigo 482");
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _getByIdUseCase.ExecuteAsync(entity.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(entity.Id);
        result.Data.Code.Should().Be("INF-100");
    }

    [Fact]
    public async Task GetInfractionTypeById_WhenNotFound_ShouldReturnFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((InfractionType?)null);

        // Act
        var result = await _getByIdUseCase.ExecuteAsync(id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(DisciplinaryErrors.InfractionTypeNotFound);
    }

    [Fact]
    public void CreateInfractionTypeValidator_ShouldValidateRequiredFields()
    {
        // Arrange
        var validator = new CreateInfractionTypeValidator();
        var invalidRequest = new CreateInfractionTypeRequest("", "", (InfractionSeverity)99);

        // Act
        var validationResult = validator.Validate(invalidRequest);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Code");
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name");
        validationResult.Errors.Should().Contain(e => e.PropertyName == "DefaultSeverity");
    }
}
