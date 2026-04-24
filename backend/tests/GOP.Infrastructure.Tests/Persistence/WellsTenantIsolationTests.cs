using FluentAssertions;
using GOP.Domain.Entities;
using GOP.Domain.Enums;
using GOP.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GOP.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifica RN-23 y RN-26: aislamiento de datos por TenantId.
/// El HasQueryFilter de GopDbContext debe filtrar automáticamente sin queries manuales.
/// </summary>
public sealed class WellsTenantIsolationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<GopDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        // Conexión compartida para que los dos contextos operen sobre la misma BD en memoria
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        _options = new DbContextOptionsBuilder<GopDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Crear el esquema con TenantId=0 (bypassa el filtro de tenant)
        await using var setupContext = new GopDbContext(_options, new FakeCurrentUserService(tenantId: 0));
        await setupContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private GopDbContext ContextForTenant(int tenantId)
        => new(_options, new FakeCurrentUserService(tenantId));

    /// <summary>
    /// Crea un Well con datos mínimos para tenantId dado.
    /// El nombre se diferencia para distinguirlos en los asserts.
    /// </summary>
    private static Well BuildWell(int tenantId, string discriminator) =>
        Well.CreateDraft(
            operadora: $"Operadora-{discriminator}",
            tenantId: tenantId,
            contratoId: tenantId * 100,
            contrato: $"CT-{discriminator}",
            tipoContrato: "E&P",
            cuenca: "LLA",
            campoId: null,
            campo: null,
            denominacion: discriminator,
            consecutivo: 1,
            tipoTrayectoria: TipoTrayectoria.ST,
            clasificacion: Clasificacion.Exploratorio,
            subClasificacion: null,
            tipoUbicacion: TipoUbicacion.Continental,
            tipoAngulo: TipoAngulo.H,
            tipoObjetivo: TipoObjetivo.PH,
            tipoTerminacion: TipoTerminacion.CD,
            departamentoId: null,
            departamento: null,
            codigoDaneDpto: null,
            municipioId: null,
            municipio: null,
            codigoDaneMpio: null,
            clusterId: null,
            cluster: null);

    // ─── Tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task TenantA_CannotSeeWellsOfTenantB()
    {
        // Arrange — insertar un pozo por cada tenant
        await using var ctxA = ContextForTenant(1);
        var wellA = BuildWell(tenantId: 1, discriminator: "POZO-A");
        ctxA.Wells.Add(wellA);
        await ctxA.SaveChangesAsync();

        await using var ctxB = ContextForTenant(2);
        var wellB = BuildWell(tenantId: 2, discriminator: "POZO-B");
        ctxB.Wells.Add(wellB);
        await ctxB.SaveChangesAsync();

        // Act — consultar SIN filtro manual (el HasQueryFilter lo hace automáticamente)
        await using var queryCtxA = ContextForTenant(1);
        var wellsVisibleByA = await queryCtxA.Wells.ToListAsync();

        await using var queryCtxB = ContextForTenant(2);
        var wellsVisibleByB = await queryCtxB.Wells.ToListAsync();

        // Assert — RN-23: cada tenant solo ve sus propios pozos
        wellsVisibleByA.Should().HaveCount(1, "tenantA solo debe ver el pozo de tenantA");
        wellsVisibleByA.Single().NombrePozo.Should().Contain("POZO-A");
        wellsVisibleByA.Should().NotContain(w => w.TenantId == 2,
            "tenantA no debe ver pozos de tenantB (RN-23)");

        wellsVisibleByB.Should().HaveCount(1, "tenantB solo debe ver el pozo de tenantB");
        wellsVisibleByB.Single().NombrePozo.Should().Contain("POZO-B");
        wellsVisibleByB.Should().NotContain(w => w.TenantId == 1,
            "tenantB no debe ver pozos de tenantA (RN-23)");
    }

    [Fact]
    public async Task TenantZero_Bypass_SeesAllWells()
    {
        // Arrange — insertar pozos de dos tenants distintos
        await using var ctxA = ContextForTenant(1);
        ctxA.Wells.Add(BuildWell(tenantId: 1, discriminator: "BYPASS-A"));
        await ctxA.SaveChangesAsync();

        await using var ctxB = ContextForTenant(2);
        ctxB.Wells.Add(BuildWell(tenantId: 2, discriminator: "BYPASS-B"));
        await ctxB.SaveChangesAsync();

        // Act — TenantId=0 bypassa el filtro (usado por migraciones/seed)
        await using var bypassCtx = ContextForTenant(0);
        var allWells = await bypassCtx.Wells.ToListAsync();

        // Assert — RN-26: bypass para operaciones administrativas/seed
        allWells.Should().HaveCount(2,
            "TenantId=0 debe bypassar el filtro y ver todos los pozos (RN-26)");
    }

    [Fact]
    public async Task SoftDeleted_WellsAreExcluded_EvenWithinSameTenant()
    {
        // Arrange
        await using var ctxInsert = ContextForTenant(1);
        var well = BuildWell(tenantId: 1, discriminator: "SOFT-DEL");
        ctxInsert.Wells.Add(well);
        await ctxInsert.SaveChangesAsync();

        // Soft delete
        well.SoftDelete();
        await ctxInsert.SaveChangesAsync();

        // Act
        await using var queryCtx = ContextForTenant(1);
        var wells = await queryCtx.Wells.ToListAsync();

        // Assert — el global query filter también excluye soft-deleted
        wells.Should().BeEmpty(
            "el HasQueryFilter excluye pozos con IsDeleted=true del mismo tenant");
    }
}
