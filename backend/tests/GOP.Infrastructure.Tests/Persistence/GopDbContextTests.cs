using FluentAssertions;
using GOP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Tests.Persistence;

public sealed class GopDbContextTests
{
    [Fact]
    public void CanInstantiateDbContext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<GopDbContext>()
            .UseInMemoryDatabase(databaseName: $"GopTest_{Guid.NewGuid()}")
            .Options;

        // Act
        var act = () =>
        {
            using var context = new GopDbContext(options);
            return context;
        };

        // Assert
        act.Should().NotThrow();
    }
}
