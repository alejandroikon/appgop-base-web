using FluentAssertions;
using GOP.Domain.Entities;
using GOP.Infrastructure.Persistence;
using GOP.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Tests.Persistence;

public sealed class UserRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<GopDbContext> _options = null!;
    private GopDbContext _context = null!;
    private UserRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        _options = new DbContextOptionsBuilder<GopDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new GopDbContext(_options);
        await _context.Database.EnsureCreatedAsync();
        _repo = new UserRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = User.Create("test@gop.co", "hash", "Test User");
        await _repo.AddAsync(user, CancellationToken.None);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repo.GetByEmailAsync("test@gop.co", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@gop.co");
        result.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _repo.GetByEmailAsync("noexiste@gop.co", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_DuplicateEmail_ThrowsException()
    {
        // Arrange
        var user1 = User.Create("dup@gop.co", "hash1", "User One");
        var user2 = User.Create("dup@gop.co", "hash2", "User Two");

        await _repo.AddAsync(user1, CancellationToken.None);
        await _context.SaveChangesAsync();

        await _repo.AddAsync(user2, CancellationToken.None);

        // Act
        Func<Task> act = () => _context.SaveChangesAsync();

        // Assert — SQLite debe rechazar el índice único IX_Users_Email
        await act.Should().ThrowAsync<Exception>(
            because: "el índice único en Email debe rechazar duplicados");
    }

    [Fact]
    public async Task ExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var user = User.Create("exists@gop.co", "hash", "Existing User");
        await _repo.AddAsync(user, CancellationToken.None);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repo.ExistsAsync("exists@gop.co", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistent_ReturnsFalse()
    {
        // Act
        var result = await _repo.ExistsAsync("nope@gop.co", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_RecordLogin_PersistsLastLoginAt()
    {
        // Arrange
        var user = User.Create("login@gop.co", "hash", "Login User");
        await _repo.AddAsync(user, CancellationToken.None);
        await _context.SaveChangesAsync();

        // Act
        user.RecordLogin();
        _repo.Update(user);
        await _context.SaveChangesAsync();

        // Verificar en contexto nuevo para evitar cache del change tracker
        await using var verifyContext = new GopDbContext(_options);
        var persisted = await verifyContext.Users.FindAsync(user.Id);

        // Assert
        persisted.Should().NotBeNull();
        persisted!.LastLoginAt.Should().NotBeNull(
            because: "RecordLogin debe persistir la fecha de último acceso");
    }
}
