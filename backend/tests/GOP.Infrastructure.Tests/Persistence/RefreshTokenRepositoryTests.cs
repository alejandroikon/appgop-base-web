using FluentAssertions;
using GOP.Domain.Entities;
using GOP.Infrastructure.Persistence;
using GOP.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Tests.Persistence;

public sealed class RefreshTokenRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<GopDbContext> _options = null!;
    private GopDbContext _context = null!;
    private RefreshTokenRepository _repo = null!;
    private User _testUser = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        _options = new DbContextOptionsBuilder<GopDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new GopDbContext(_options, new FakeCurrentUserService());
        await _context.Database.EnsureCreatedAsync();
        _repo = new RefreshTokenRepository(_context);

        // Pre-insertar usuario base (FK requerida por RefreshTokens)
        _testUser = User.Create("user@gop.co", "hash", "Test User");
        await _context.Users.AddAsync(_testUser);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task AddAndGetByToken_ReturnsToken()
    {
        // Arrange
        var rt = RefreshToken.Create(_testUser.Id, "my-token", DateTime.UtcNow.AddDays(7));
        await _repo.AddAsync(rt, CancellationToken.None);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repo.GetByTokenAsync("my-token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("my-token");
        result.UserId.Should().Be(_testUser.Id);
        result.RevokedAt.Should().BeNull("el token recién creado no debe estar revocado");
    }

    [Fact]
    public async Task GetByTokenAsync_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _repo.GetByTokenAsync("does-not-exist", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_RevokesAllActive()
    {
        // Arrange — 2 tokens activos + 1 pre-revocado
        var rt1 = RefreshToken.Create(_testUser.Id, "token-active-1", DateTime.UtcNow.AddDays(7));
        var rt2 = RefreshToken.Create(_testUser.Id, "token-active-2", DateTime.UtcNow.AddDays(7));
        var rt3 = RefreshToken.Create(_testUser.Id, "token-pre-revoked", DateTime.UtcNow.AddDays(7));
        rt3.Revoke();

        await _context.RefreshTokens.AddRangeAsync(rt1, rt2, rt3);
        await _context.SaveChangesAsync();

        var rt3OriginalRevokedAt = rt3.RevokedAt;

        // Act
        await _repo.RevokeAllForUserAsync(_testUser.Id, CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        rt1.RevokedAt.Should().NotBeNull("el token activo 1 debe haber sido revocado");
        rt2.RevokedAt.Should().NotBeNull("el token activo 2 debe haber sido revocado");
        rt3.RevokedAt.Should().Be(rt3OriginalRevokedAt,
            "el token ya revocado no debe ser modificado por RevokeAllForUserAsync");
    }

    [Fact]
    public async Task CascadeDelete_DeletingUser_DeletesTokens()
    {
        // Arrange
        var rt1 = RefreshToken.Create(_testUser.Id, "cascade-token-1", DateTime.UtcNow.AddDays(7));
        var rt2 = RefreshToken.Create(_testUser.Id, "cascade-token-2", DateTime.UtcNow.AddDays(7));
        await _context.RefreshTokens.AddRangeAsync(rt1, rt2);
        await _context.SaveChangesAsync();

        // Act — eliminar el usuario debe eliminar sus tokens en cascada (OnDelete.Cascade)
        _context.Users.Remove(_testUser);
        await _context.SaveChangesAsync();

        // Assert
        var tokenCount = await _context.RefreshTokens.CountAsync();
        tokenCount.Should().Be(0,
            "cascade delete debe eliminar todos los RefreshTokens del usuario eliminado");
    }
}
