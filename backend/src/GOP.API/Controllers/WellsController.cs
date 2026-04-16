using GOP.API.Extensions;
using GOP.Application.Common;
using GOP.Application.Features.Wells.Commands.CreateWell;
using GOP.Application.Features.Wells.Commands.DeleteWell;
using GOP.Application.Features.Wells.Commands.UpdateWell;
using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Application.Features.Wells.Queries.GetWellsList;
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
    /// <summary>GET /api/v1/wells — HU-020: Listar pozos con paginación y filtros</summary>
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

    /// <summary>GET /api/v1/wells/{id} — HU-022: Obtener detalle de un pozo</summary>
    [HttpGet("{id:guid}", Name = "GetWellById")]
    [ProducesResponseType(typeof(WellDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWell(Guid id, CancellationToken cancellationToken = default)
        => (await sender.Send(new GetWellByIdQuery(id), cancellationToken)).ToActionResult();

    /// <summary>POST /api/v1/wells — HU-021: Crear un nuevo pozo en estado borrador</summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]
    [ProducesResponseType(typeof(WellDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateWell(
        [FromBody] CreateWellCommand command,
        CancellationToken cancellationToken = default)
        => (await sender.Send(command, cancellationToken))
            .ToCreatedResult("GetWellById", new { id = "placeholder" });

    /// <summary>PUT /api/v1/wells/{id} — HU-023: Actualizar un pozo en estado borrador</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]
    [ProducesResponseType(typeof(WellDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateWell(
        Guid id,
        [FromBody] UpdateWellRequest body,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateWellCommand(
            id, body.ContratoId, body.CampoId, body.TipoTrayectoria, body.Clasificacion,
            body.Denominacion, body.Consecutivo, body.TipoUbicacion, body.TipoAngulo,
            body.TipoObjetivo, body.TipoTerminacion, body.DepartamentoId, body.MunicipioId,
            body.ClusterId);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>DELETE /api/v1/wells/{id} — HU-024: Eliminar (soft delete) un pozo en borrador</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteWell(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteWellCommand(id), cancellationToken);
        if (result.IsSuccess) return NoContent();
        return result.ToActionResult();
    }
}

/// <summary>DTO de body para PUT /api/v1/wells/{id} — desacopla Id del path del body.</summary>
public sealed record UpdateWellRequest(
    int ContratoId,
    int CampoId,
    string TipoTrayectoria,
    string Clasificacion,
    string Denominacion,
    string Consecutivo,
    string TipoUbicacion,
    string TipoAngulo,
    string TipoObjetivo,
    string TipoTerminacion,
    int DepartamentoId,
    int MunicipioId,
    int? ClusterId
);
