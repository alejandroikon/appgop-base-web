// T-INFRA-19: DbSeeder — Seed idempotente (upsert) de datos DANE DIVIPOLA
// RN-INFRA-07: inserción vía EF Core con detección de existencia para ser idempotente
// Fuente de datos: proyecto26/colombia basada en DANE/MinTIC DIVIPOLA
//
// NOTA: Los datos base de Contratos, Campos, Clusters ya vienen en las migraciones
// EF Core vía HasData. Este seeder solo maneja los datos DANE (Departamentos, Municipios)
// que son demasiado voluminosos para HasData.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using GOP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GOP.Infrastructure.Persistence;

public sealed class DbSeeder(
    GopDbContext context,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedDepartamentosAsync(cancellationToken);
        await SeedMunicipiosAsync(cancellationToken);
    }

    private async Task SeedDepartamentosAsync(CancellationToken cancellationToken)
    {
        var jsonPath = GetDataFilePath("departamentos.json");
        if (!File.Exists(jsonPath))
        {
            logger.LogWarning("Archivo de departamentos no encontrado en: {Path}. Seed DANE omitido.", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath, cancellationToken);
        var departamentos = JsonSerializer.Deserialize<List<DepartamentoSeedDto>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var existingIds = await context.Departamentos
            .Select(d => d.Id)
            .ToHashSetAsync(cancellationToken);

        var toAdd = departamentos
            .Where(d => !existingIds.Contains(d.Id))
            .Select(d => new Departamento
            {
                Id = d.Id,
                Nombre = d.Nombre,
                CodigoDane = d.CodigoDane
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            context.Departamentos.AddRange(toAdd);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seed DANE: {Count} departamentos insertados (total en DB: {Total}).",
                toAdd.Count, existingIds.Count + toAdd.Count);
        }
        else
        {
            logger.LogInformation("Seed DANE: {Count} departamentos ya presentes, sin cambios.", existingIds.Count);
        }
    }

    private async Task SeedMunicipiosAsync(CancellationToken cancellationToken)
    {
        var jsonPath = GetDataFilePath("municipios.json");
        if (!File.Exists(jsonPath))
        {
            logger.LogWarning("Archivo de municipios no encontrado en: {Path}. Seed DANE omitido.", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath, cancellationToken);
        var municipios = JsonSerializer.Deserialize<List<MunicipioSeedDto>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var existingIds = await context.Municipios
            .Select(m => m.Id)
            .ToHashSetAsync(cancellationToken);

        // Only insert municipios whose departamentoId exists in DB
        var existingDptoIds = await context.Departamentos
            .Select(d => d.Id)
            .ToHashSetAsync(cancellationToken);

        var toAdd = municipios
            .Where(m => !existingIds.Contains(m.Id) && existingDptoIds.Contains(m.DepartamentoId))
            .Select(m => new Municipio
            {
                Id = m.Id,
                Nombre = m.Nombre,
                DepartamentoId = m.DepartamentoId,
                CodigoDane = m.CodigoDane
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            context.Municipios.AddRange(toAdd);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seed DANE: {Count} municipios insertados (total en DB: {Total}).",
                toAdd.Count, existingIds.Count + toAdd.Count);
        }
        else
        {
            logger.LogInformation("Seed DANE: {Count} municipios ya presentes, sin cambios.", existingIds.Count);
        }
    }

    private static string GetDataFilePath(string fileName)
    {
        // Priority 1: alongside the assembly (works in Docker/App Service after publish)
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        var candidate1 = Path.Combine(assemblyDir, "DaneData", fileName);
        if (File.Exists(candidate1)) return candidate1;

        // Priority 2: current working directory (works in dev with dotnet run)
        var candidate2 = Path.Combine(AppContext.BaseDirectory, "DaneData", fileName);
        if (File.Exists(candidate2)) return candidate2;

        // Priority 3: relative path from working directory
        return Path.Combine("DaneData", fileName);
    }

    // ── DTO records for JSON deserialization ──────────────────────────────────
    private sealed record DepartamentoSeedDto(
        [property: JsonPropertyName("id")]         int    Id,
        [property: JsonPropertyName("nombre")]     string Nombre,
        [property: JsonPropertyName("codigoDane")] string CodigoDane);

    private sealed record MunicipioSeedDto(
        [property: JsonPropertyName("id")]             int    Id,
        [property: JsonPropertyName("nombre")]         string Nombre,
        [property: JsonPropertyName("departamentoId")] int    DepartamentoId,
        [property: JsonPropertyName("codigoDane")]     string CodigoDane);
}
