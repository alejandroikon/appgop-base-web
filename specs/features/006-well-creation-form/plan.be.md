# Plan Backend: Formulario de Creación de Pozo — Preview de Nombre

**Feature ID:** 006-well-creation-form
**Contrato:** `specs/features/006-well-creation-form/contract.yml`
**Dependencias backend:** 005-wells-catalog-crud (entidades Well, Contrato, Campo ya existen)

---

## 1. Alcance Backend

Esta feature agrega **un solo endpoint nuevo** al backend: `GET /api/v1/wells/preview-name`. El resto de los endpoints consumidos por el wizard frontend (CRUD de pozos + catálogos) ya están implementados en la iteración 005.

---

## 2. Proyectos Afectados

| Proyecto | Cambios |
|---|---|
| `GOP.Domain` | Ninguno — no hay entidades ni Value Objects nuevos |
| `GOP.Application` | 1 Query nueva: `PreviewWellName/` (query + handler + DTO + validator) |
| `GOP.Infrastructure` | Ninguno — la query lee directo del DbContext (ver CONSTITUTION.backend.md §4.4) |
| `GOP.API` | 1 action nuevo en `WellsController` |
| `GOP.Application.Tests` | 1 clase de tests para el handler nuevo |

---

## 3. Query: PreviewWellName

### 3.1. Ubicación

```
GOP.Application/Features/Wells/Queries/PreviewWellName/
├── PreviewWellNameQuery.cs
├── PreviewWellNameQueryHandler.cs
├── PreviewWellNameQueryValidator.cs
└── WellNamePreviewDto.cs
```

### 3.2. Query Record

```csharp
public sealed record PreviewWellNameQuery(
    int ContratoId,
    string Denominacion,
    string Consecutivo,
    Guid? ExcludeWellId    // Para modo edición: excluir el pozo actual de la verificación
) : IRequest<Result<WellNamePreviewDto>>;
```

### 3.3. DTO de Respuesta

```csharp
public sealed record WellNamePreviewDto(
    string NombrePozo,
    bool Available
);
```

### 3.4. Handler — Lógica

1. Buscar el contrato por `ContratoId` en la DB → obtener `Cuenca`
2. Si el contrato no existe → `Result.Failure(DomainErrors.Contrato.NotFound)`
3. Calcular `nombrePozo = "{cuenca}-{denominacion}-{consecutivo}"`
4. Verificar si existe algún Well con ese `NombrePozo` (excluyendo `ExcludeWellId` si se provee)
5. Retornar `Result.Success(new WellNamePreviewDto(nombrePozo, !exists))`

```csharp
internal sealed class PreviewWellNameQueryHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<PreviewWellNameQuery, Result<WellNamePreviewDto>>
{
    public async Task<Result<WellNamePreviewDto>> Handle(
        PreviewWellNameQuery request, CancellationToken cancellationToken)
    {
        var contrato = await dbContext.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ContratoId, cancellationToken);

        if (contrato is null)
            return Result.Failure<WellNamePreviewDto>(DomainErrors.Contrato.NotFound);

        var nombrePozo = $"{contrato.Cuenca}-{request.Denominacion.Trim()}-{request.Consecutivo}";

        var query = dbContext.Wells.AsNoTracking()
            .Where(w => w.NombrePozo == nombrePozo);

        if (request.ExcludeWellId.HasValue)
            query = query.Where(w => w.Id != request.ExcludeWellId.Value);

        var exists = await query.AnyAsync(cancellationToken);

        return Result.Success(new WellNamePreviewDto(nombrePozo, !exists));
    }
}
```

### 3.5. Validator

```csharp
internal sealed class PreviewWellNameQueryValidator : AbstractValidator<PreviewWellNameQuery>
{
    public PreviewWellNameQueryValidator()
    {
        RuleFor(x => x.ContratoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un contrato válido.");

        RuleFor(x => x.Denominacion)
            .NotEmpty().WithMessage("La denominación es requerida.")
            .MaximumLength(50).WithMessage("La denominación no puede exceder 50 caracteres.")
            .Matches(@"^[A-Za-záéíóúÁÉÍÓÚñÑ ]+$").WithMessage("La denominación solo acepta letras y espacios.");

        RuleFor(x => x.Consecutivo)
            .NotEmpty().WithMessage("El consecutivo es requerido.")
            .Matches(@"^\d{2}$").WithMessage("El consecutivo debe ser numérico de 2 dígitos.");
    }
}
```

---

## 4. Controller Action

Nuevo action en `WellsController.cs` (ya existente de la iteración 005):

```csharp
[HttpGet("preview-name")]
[Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]
[ProducesResponseType(typeof(WellNamePreviewDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
public async Task<IActionResult> PreviewWellName(
    [FromQuery] int contratoId,
    [FromQuery] string denominacion,
    [FromQuery] string consecutivo,
    [FromQuery] Guid? excludeWellId)
    => (await sender.Send(new PreviewWellNameQuery(
        contratoId, denominacion, consecutivo, excludeWellId))).ToActionResult();
```

**Referencia:** El controller sigue el patrón de máximo 3 líneas por action (CONSTITUTION.backend.md §13.3).

---

## 5. Errores de Dominio

Agregar al catálogo `DomainErrors` (si no existe ya):

```csharp
// Domain/Errors/DomainErrors.Contrato.cs (parcial)
public static class Contrato
{
    public static readonly Error NotFound = new(
        "Contrato.NotFound", "El contrato no fue encontrado.");
}
```

**Nota:** Si `DomainErrors.Contrato` ya fue creado en la iteración 005, solo se verifica que el error `NotFound` exista. No se duplica.

---

## 6. Tests Requeridos

### PreviewWellNameQueryHandlerTests

| Test | Escenario | Resultado Esperado |
|---|---|---|
| `Handle_ValidQuery_ReturnsAvailableName` | Contrato existe, nombre no en uso | `IsSuccess=true`, `Available=true`, `NombrePozo="{cuenca}-{denom}-{consec}"` |
| `Handle_DuplicateName_ReturnsUnavailable` | Nombre ya existe en DB | `IsSuccess=true`, `Available=false` |
| `Handle_ContratoNotFound_ReturnsFailure` | ContratoId no existe | `IsFailure=true`, Error = `Contrato.NotFound` |
| `Handle_ExcludeWellId_ExcludesFromCheck` | Nombre existe pero es el pozo excluido | `IsSuccess=true`, `Available=true` |

---

## 7. Mapping

No se requiere AutoMapper para esta query — el DTO se construye directamente en el handler (ver CONSTITUTION.backend.md §12.3: "Lógica compleja va en el handler").

---

## 8. Migraciones

**No se requieren migraciones.** No hay cambios en el modelo de datos. Las tablas Wells y Contratos ya existen de la iteración 005.
