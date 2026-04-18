using GOP.API.Contracts;
using GOP.API.Extensions;
using GOP.Application.Common;
using GOP.Application.Features.Wells.Commands.CreateWell;
using GOP.Application.Features.Wells.Commands.DeleteWell;
using GOP.Application.Features.Wells.Commands.TransitionWell;
using GOP.Application.Features.Wells.Commands.UpdateWell;
using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Application.Features.Wells.Queries.GetWellHistory;
using GOP.Application.Features.Wells.Queries.GetWellsList;
using GOP.Application.Features.Wells.Queries.PreviewUwi;
using GOP.Application.Features.Wells.Queries.PreviewWellName;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOP.API.Controllers;

[ApiController]
[Route("api/v1/wells")]
[Authorize]
[Produces("application/json")]
public sealed class WellsController(ISender sender) : ControllerBase
{
    /// <summary>GET /api/v1/wells — Listar pozos con paginación y filtros</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<WellListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListWells(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] int? contratoId = null,
        [FromQuery] string? estado = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWellsListQuery(page, pageSize, search, sortBy, sortDir, contratoId, estado);
        return (await sender.Send(query, cancellationToken)).ToActionResult();
    }

    /// <summary>GET /api/v1/wells/preview-uwi — Preview UWI PPDM</summary>
    [HttpGet("preview-uwi")]
    [Authorize(Roles = "ADMIN,OPERADOR")]
    [ProducesResponseType(typeof(UwiPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PreviewUwi(
        [FromQuery] string codigoDaneDpto,
        [FromQuery] string codigoDaneMpio,
        [FromQuery] string denominacion,
        [FromQuery] int consecutivo,
        [FromQuery] string? clusterNombre,
        [FromQuery] int clusterNumero = 0,
        [FromQuery] string tipoAngulo = "V",
        [FromQuery] string tipoTrayectoria = "O",
        [FromQuery] int trayectoriaConsecutivo = 1,
        [FromQuery] string tipoObjetivo = "PH",
        [FromQuery] string tipoTerminacion = "OH",
        [FromQuery] bool isAnh = false,
        [FromQuery] Guid? excludeWellId = null,
        CancellationToken cancellationToken = default)
        => (await sender.Send(new PreviewUwiQuery(
            codigoDaneDpto, codigoDaneMpio, denominacion, consecutivo,
            clusterNombre, clusterNumero, tipoAngulo, tipoTrayectoria,
            trayectoriaConsecutivo, tipoObjetivo, tipoTerminacion, isAnh, excludeWellId),
            cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/wells/preview-name — Preview nombre del pozo</summary>
    [HttpGet("preview-name")]
    [Authorize(Roles = "ADMIN,OPERADOR")]
    [ProducesResponseType(typeof(WellNamePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PreviewWellName(
        [FromQuery] int contratoId,
        [FromQuery] int? campoId,
        [FromQuery] string denominacion,
        [FromQuery] int consecutivo,
        [FromQuery] Guid? excludeWellId,
        CancellationToken cancellationToken = default)
        => (await sender.Send(
            new PreviewWellNameQuery(contratoId, campoId, denominacion, consecutivo, excludeWellId),
            cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/wells/{id} — Detalle de un pozo</summary>
    [HttpGet("{id:guid}", Name = "GetWellById")]
    [ProducesResponseType(typeof(WellDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWell(Guid id, CancellationToken cancellationToken = default)
        => (await sender.Send(new GetWellByIdQuery(id), cancellationToken)).ToActionResult();

    /// <summary>POST /api/v1/wells — Crear pozo (DRAFT o FINALIZE)</summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN,OPERADOR")]
    [ProducesResponseType(typeof(WellDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateWell(
        [FromBody] CreateWellCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult();
        return CreatedAtRoute("GetWellById", new { id = result.Value.Id }, result.Value);
    }

    /// <summary>PUT /api/v1/wells/{id} — Actualizar pozo (SAVE o FINALIZE)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]
    [ProducesResponseType(typeof(WellDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateWell(
        Guid id,
        [FromBody] UpdateWellRequest body,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateWellCommand(
            WellId: id,
            Action: body.Action,
            ContratoId: body.ContratoId,
            CampoId: body.CampoId,
            Denominacion: body.Denominacion,
            Consecutivo: body.Consecutivo,
            TipoTrayectoria: body.TipoTrayectoria,
            Clasificacion: body.Clasificacion,
            SubClasificacion: body.SubClasificacion,
            TipoUbicacion: body.TipoUbicacion,
            TipoAngulo: body.TipoAngulo,
            TipoObjetivo: body.TipoObjetivo,
            TipoTerminacion: body.TipoTerminacion,
            DepartamentoId: body.DepartamentoId,
            MunicipioId: body.MunicipioId,
            ClusterId: body.ClusterId,
            ClusterNumero: body.ClusterNumero,
            TrayectoriaConsecutivo: body.TrayectoriaConsecutivo);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>DELETE /api/v1/wells/{id} — Eliminar pozo (soft delete)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN,OPERADOR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteWell(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteWellCommand(id), cancellationToken);
        if (result.IsSuccess) return NoContent();
        return result.ToActionResult();
    }

    /// <summary>PATCH /api/v1/wells/{id}/transition — Legacy V1 (TODO-ITER-9.3: remover)</summary>
    [HttpPatch("{id:guid}/transition")]
    [Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]
    public async Task<IActionResult> TransitionWell(
        Guid id,
        [FromBody] TransitionWellRequest request,
        CancellationToken cancellationToken = default)
        => (await sender.Send(new TransitionWellCommand(id, request.Action, request.Comment), cancellationToken))
            .ToActionResult();

    /// <summary>GET /api/v1/wells/{id}/history — Historial de transiciones</summary>
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetWellHistory(Guid id, CancellationToken cancellationToken = default)
        => (await sender.Send(new GetWellHistoryQuery(id), cancellationToken)).ToActionResult();
}
