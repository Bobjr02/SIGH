using FluentAssertions;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

public class DisciplinaryCaseEmployeeTests
{
    private readonly Guid _caseId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidData_ShouldCreateCaseEmployee()
    {
        // Act
        var caseEmployee = DisciplinaryCaseEmployee.Create(
            disciplinaryCaseId: _caseId,
            employeeId: _employeeId,
            role: CaseEmployeeRole.Accused,
            isPrimarySubject: true
        );

        // Assert
        caseEmployee.Should().NotBeNull();
        caseEmployee.DisciplinaryCaseId.Should().Be(_caseId);
        caseEmployee.EmployeeId.Should().Be(_employeeId);
        caseEmployee.Role.Should().Be(CaseEmployeeRole.Accused);
        caseEmployee.IsPrimarySubject.Should().BeTrue();
        caseEmployee.Statement.Should().BeNull();
        caseEmployee.StatementRecordedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(CaseEmployeeRole.Undefined)]
    [InlineData((CaseEmployeeRole)999)]
    public void Create_WithInvalidRole_ShouldThrowBusinessRuleValidationException(CaseEmployeeRole invalidRole)
    {
        // Act
        Action act = () => DisciplinaryCaseEmployee.Create(
            disciplinaryCaseId: _caseId,
            employeeId: _employeeId,
            role: invalidRole
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O papel do funcionário no processo é inválido.");
    }

    [Fact]
    public void Create_WithEmptyEmployeeId_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryCaseEmployee.Create(
            disciplinaryCaseId: _caseId,
            employeeId: Guid.Empty,
            role: CaseEmployeeRole.Accused
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O ID do funcionário é obrigatório.");
    }

    [Fact]
    public void Create_WithEmptyCaseId_ShouldThrowBusinessRuleValidationException()
    {
        // Act
        Action act = () => DisciplinaryCaseEmployee.Create(
            disciplinaryCaseId: Guid.Empty,
            employeeId: _employeeId,
            role: CaseEmployeeRole.Accused
        );

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O ID do processo disciplinar é obrigatório.");
    }

    [Fact]
    public void RegisterStatement_WithValidText_ShouldRecordStatement()
    {
        // Arrange
        var caseEmployee = DisciplinaryCaseEmployee.Create(_caseId, _employeeId, CaseEmployeeRole.Witness);

        // Act
        caseEmployee.RegisterStatement("Declaro ter presenciado o fato no dia citado.", _now);

        // Assert
        caseEmployee.Statement.Should().Be("Declaro ter presenciado o fato no dia citado.");
        caseEmployee.StatementRecordedAt.Should().Be(_now);
    }

    [Fact]
    public void RegisterStatement_WithDefaultRecordedAt_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var caseEmployee = DisciplinaryCaseEmployee.Create(_caseId, _employeeId, CaseEmployeeRole.Witness);

        // Act
        Action act = () => caseEmployee.RegisterStatement("Depoimento válido", default);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A data do depoimento é inválida.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RegisterStatement_WithEmptyText_ShouldThrowBusinessRuleValidationException(string? invalidStatement)
    {
        // Arrange
        var caseEmployee = DisciplinaryCaseEmployee.Create(_caseId, _employeeId, CaseEmployeeRole.Witness);

        // Act
        Action act = () => caseEmployee.RegisterStatement(invalidStatement!, _now);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O depoimento não pode ser vazio.");
    }

    [Fact]
    public void UpdateRole_ShouldChangeRole()
    {
        // Arrange
        var caseEmployee = DisciplinaryCaseEmployee.Create(_caseId, _employeeId, CaseEmployeeRole.Witness);

        // Act
        caseEmployee.UpdateRole(CaseEmployeeRole.Accused);

        // Assert
        caseEmployee.Role.Should().Be(CaseEmployeeRole.Accused);
    }

    [Theory]
    [InlineData(CaseEmployeeRole.Undefined)]
    [InlineData((CaseEmployeeRole)999)]
    public void UpdateRole_WithInvalidRole_ShouldThrowBusinessRuleValidationException(CaseEmployeeRole invalidRole)
    {
        // Arrange
        var caseEmployee = DisciplinaryCaseEmployee.Create(_caseId, _employeeId, CaseEmployeeRole.Witness);

        // Act
        Action act = () => caseEmployee.UpdateRole(invalidRole);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("O papel do funcionário no processo é inválido.");
    }
}
