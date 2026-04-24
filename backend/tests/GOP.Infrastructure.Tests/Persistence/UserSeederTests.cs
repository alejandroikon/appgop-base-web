using FluentAssertions;
using GOP.Infrastructure.Persistence;
using GOP.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GOP.Infrastructure.Tests.Persistence;

public sealed class UserSeederTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private GopDbContext _context = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<GopDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new GopDbContext(options, new FakeCurrentUserService());
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private UserSeeder CreateSeeder(IConfiguration configuration, IHostEnvironment environment)
    {
        var userRepo = new UserRepository(_context);
        var logger = Substitute.For<ILogger<UserSeeder>>();
        // GopDbContext implementa IUnitOfWork directamente
        return new UserSeeder(userRepo, _context, configuration, environment, logger);
    }

    private static IHostEnvironment DevEnvironment()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);
        return env;
    }

    private static IHostEnvironment ProdEnvironment()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Production);
        return env;
    }

    [Fact]
    public async Task SeedAsync_EmptyDb_InsertsDevUsers_WhenNoConfig()
    {
        // Arrange — sin sección SeedUsers, ambiente Development
        var config = new ConfigurationBuilder().Build();
        var seeder = CreateSeeder(config, DevEnvironment());

        // Act
        await seeder.SeedAsync();

        // Assert
        var count = await _context.Users.CountAsync();
        count.Should().Be(4,
            "deben insertarse los 4 dev users estáticos cuando no hay config y el ambiente es Development");
    }

    [Fact]
    public async Task SeedAsync_UsersAlreadyExist_NoChanges()
    {
        // Arrange — sembrar los 4 users primero
        var config = new ConfigurationBuilder().Build();
        var env = DevEnvironment();
        var seeder = CreateSeeder(config, env);
        await seeder.SeedAsync(); // primera ejecución → inserta 4

        // Act — segunda ejecución (idempotencia)
        await seeder.SeedAsync();

        // Assert — el count sigue siendo 4, sin duplicados
        var count = await _context.Users.CountAsync();
        count.Should().Be(4, "el seeder debe ser idempotente y no duplicar usuarios existentes");
    }

    [Fact]
    public async Task SeedAsync_WithConfig_InsertsConfiguredAdmins()
    {
        // Arrange — sección SeedUsers configurada con ExecAdmin y OpAdmin
        // Nota: se usa ConfigurationBuilder real en lugar de Substitute<IConfiguration>
        //       porque GetSection con claves anidadas no es trivial de mockear.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedUsers:ExecAdmin:Email"]    = "exec@test.co",
                ["SeedUsers:ExecAdmin:Password"] = "ExecPass123!",
                ["SeedUsers:ExecAdmin:FullName"] = "Exec Admin",
                ["SeedUsers:OpAdmin:Email"]      = "op@test.co",
                ["SeedUsers:OpAdmin:Password"]   = "OpPass123!",
                ["SeedUsers:OpAdmin:FullName"]   = "Op Admin",
            })
            .Build();

        var seeder = CreateSeeder(config, ProdEnvironment());

        // Act
        await seeder.SeedAsync();

        // Assert
        var count = await _context.Users.CountAsync();
        count.Should().Be(2, "deben insertarse exactamente los 2 admins configurados");

        (await _context.Users.AnyAsync(u => u.Email == "exec@test.co"))
            .Should().BeTrue("el ExecAdmin configurado debe existir");
        (await _context.Users.AnyAsync(u => u.Email == "op@test.co"))
            .Should().BeTrue("el OpAdmin configurado debe existir");
    }

    [Fact]
    public async Task SeedAsync_MissingPassword_ThrowsException()
    {
        // Arrange — sección SeedUsers existe pero ExecAdmin no tiene Password → seeder debe abortar
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedUsers:ExecAdmin:Email"] = "exec@test.co",
                // Password intencionalmente omitido
            })
            .Build();

        var seeder = CreateSeeder(config, ProdEnvironment());

        // Act
        Func<Task> act = () => seeder.SeedAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>(
            because: "el seeder debe lanzar InvalidOperationException cuando falta la contraseña configurada");
    }
}
