using FluentAssertions;
using GOP.Domain.Common;

namespace GOP.Domain.Tests.Common;

// Clase concreta de test que hereda Entity (necesaria porque Entity es abstracta)
file sealed class TestEntity : Entity { }

public sealed class EntityTests
{
    [Fact]
    public void NewEntity_GeneratesNonEmptyGuid()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBeEmpty();
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void TwoEntities_HaveDifferentIds()
    {
        // Arrange & Act
        var entityA = new TestEntity();
        var entityB = new TestEntity();

        // Assert
        entityA.Id.Should().NotBe(entityB.Id);
    }
}
