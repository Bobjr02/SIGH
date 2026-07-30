using FluentAssertions;
using SIGH.Domain.Common;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests;

public class TestEntity : Entity
{
    public TestEntity() : base() { }
    public TestEntity(Guid id) : base(id) { }
}

public class TestAuditableEntity : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
}

public class FoundationArchitectureTests
{
    [Fact]
    public void Entity_Equality_ShouldWorkByGuid()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        entity1.Should().Be(entity2);
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void AuditableEntity_ShouldHaveAuditFieldsUsingDateTimeOffsetAndGuid()
    {
        var entity = new TestAuditableEntity
        {
            Name = "Aviso Disciplinar",
            CreatedBy = Guid.NewGuid(),
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.NewGuid(),
            IsDeleted = false
        };

        entity.CreatedAt.Should().NotBe(default);
        entity.CreatedBy.Should().NotBeNull();
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void BusinessRuleValidationException_ShouldSetMessageAndDetails()
    {
        var ex = new BusinessRuleValidationException("Regra violada", "Detalhes adicionais");

        ex.Message.Should().Be("Regra violada");
        ex.Details.Should().Be("Detalhes adicionais");
    }
}
