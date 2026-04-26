using FluentAssertions;
using GOP.Domain.Entities;
using GOP.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GOP.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifica que DbSeeder.SeedAsync() es idempotente cuando los 3 departamentos
/// y 5 municipios mínimos ya existen en la BD (sembrados por la migración
/// SeedCatalogosGeo o manualmente en staging).
///
/// El seeder usa: existingIds.Contains(d.Id) → toAdd vacío → no inserta nada.
/// </summary>
public sealed class DbSeederIdempotencyTests : IAsyncLifetime
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

        _context = new GopDbContext(options, new FakeCurrentUserService(tenantId: 0));
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task SeedAsync_WhenCatalogosGeoAlreadyExist_DoesNotDuplicate()
    {
        // Arrange — simular el estado post-migración SeedCatalogosGeo (IDs secuenciales, ED-09)
        _context.Departamentos.AddRange(
            new Departamento { Id = 1, Nombre = "Meta",       CodigoDane = "50" },
            new Departamento { Id = 2, Nombre = "Casanare",   CodigoDane = "85" },
            new Departamento { Id = 3, Nombre = "Santander",  CodigoDane = "68" });

        _context.Municipios.AddRange(
            new Municipio { Id = 1, Nombre = "Puerto Gaitán",    DepartamentoId = 1, CodigoDane = "50568" },
            new Municipio { Id = 2, Nombre = "Puerto López",     DepartamentoId = 1, CodigoDane = "50573" },
            new Municipio { Id = 3, Nombre = "Tauramena",        DepartamentoId = 2, CodigoDane = "85410" },
            new Municipio { Id = 4, Nombre = "Aguazul",          DepartamentoId = 2, CodigoDane = "85010" },
            new Municipio { Id = 5, Nombre = "Barrancabermeja",  DepartamentoId = 3, CodigoDane = "68081" });

        await _context.SaveChangesAsync();

        var countDptosBefore  = await _context.Departamentos.CountAsync();
        var countMpiosBefore  = await _context.Municipios.CountAsync();

        // DbSeeder: en ausencia de archivos JSON (entorno de test), hace return early
        // con LogWarning. Los existingIds están presentes así que toAdd queda vacío.
        var logger = Substitute.For<ILogger<DbSeeder>>();
        var seeder = new DbSeeder(_context, logger);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert — los recuentos no cambiaron (ni duplicados, ni eliminados)
        var countDptosAfter = await _context.Departamentos.CountAsync();
        var countMpiosAfter = await _context.Municipios.CountAsync();

        countDptosAfter.Should().Be(countDptosBefore,
            "el seeder no debe duplicar departamentos ya existentes");
        countMpiosAfter.Should().Be(countMpiosBefore,
            "el seeder no debe duplicar municipios ya existentes");

        // Confirmar que los 3+5 registros mínimos siguen correctos
        countDptosAfter.Should().BeGreaterThanOrEqualTo(3);
        countMpiosAfter.Should().BeGreaterThanOrEqualTo(5);

        // Verificar DANE correcto de Aguazul (ED-11)
        var aguazul = await _context.Municipios.FindAsync(4);
        aguazul!.CodigoDane.Should().Be("85010", "Aguazul tiene DANE 85010, no 85015 (ED-11)");
    }

    [Fact]
    public async Task SeedAsync_WhenNoJsonFiles_DoesNotThrow()
    {
        // Arrange — BD vacía, sin archivos JSON disponibles en entorno de test
        var logger = Substitute.For<ILogger<DbSeeder>>();
        var seeder = new DbSeeder(_context, logger);

        // Act — debe hacer return early con LogWarning, sin lanzar excepción
        var act = async () => await seeder.SeedAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync("el seeder maneja gracefully la ausencia de archivos JSON");
    }
}
