using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Queries.PreviewWellName;

internal sealed class PreviewWellNameQueryHandler(
    IApplicationDbContext dbContext,
    IWellRepository wellRepository
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

        // Si hay campo, el nombre incluye el campo; si no, usa la cuenca del contrato
        string prefix = contrato.Cuenca;
        if (request.CampoId.HasValue)
        {
            var campo = await dbContext.Campos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CampoId.Value, cancellationToken);
            if (campo is not null)
                prefix = campo.Nombre;
        }

        var denominacionUpper = request.Denominacion.Trim().ToUpperInvariant();
        var nombrePozo = $"{prefix.ToUpperInvariant()}-{denominacionUpper}-{request.Consecutivo}";

        // Necesitamos TenantId para filtrar correctamente — obtenemos del primer pozo existente con este contrato
        // ADR: La unicidad del nombre se verifica globalmente en la tabla (el índice único ya filtra por TenantId implícitamente
        // a través del ContratoId que pertenece a un tenant). Usamos ContratoId como proxy de tenant aquí.
        var exists = await dbContext.Wells
            .AsNoTracking()
            .Where(w => w.NombrePozo == nombrePozo && w.ContratoId == request.ContratoId)
            .Where(w => !request.ExcludeWellId.HasValue || w.Id != request.ExcludeWellId.Value)
            .AnyAsync(cancellationToken);

        return Result.Success(new WellNamePreviewDto(nombrePozo, !exists));
    }
}
