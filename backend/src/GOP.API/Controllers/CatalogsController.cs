using GOP.API.Extensions;
using GOP.Application.Features.Catalogs.Queries.GetCampos;
using GOP.Application.Features.Catalogs.Queries.GetClusters;
using GOP.Application.Features.Catalogs.Queries.GetContratos;
using GOP.Application.Features.Catalogs.Queries.GetDepartamentos;
using GOP.Application.Features.Catalogs.Queries.GetMunicipios;
using GOP.Application.Features.Wells.Commands.CreateCluster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOP.API.Controllers;

[ApiController]
[Route("api/v1/catalogs")]
[Authorize]
[Produces("application/json")]
public sealed class CatalogsController(ISender sender) : ControllerBase
{
    /// <summary>GET /api/v1/catalogs/contratos — Listar contratos</summary>
    [HttpGet("contratos")]
    [ProducesResponseType(typeof(List<ContratoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListContratos(CancellationToken cancellationToken = default)
        => (await sender.Send(new GetContratosQuery(), cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/catalogs/campos?contratoId=N — Campos por contrato</summary>
    [HttpGet("campos")]
    [ProducesResponseType(typeof(List<CampoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListCampos(
        [FromQuery] int contratoId,
        CancellationToken cancellationToken = default)
        => (await sender.Send(new GetCamposQuery(contratoId), cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/catalogs/departamentos — Listar departamentos</summary>
    [HttpGet("departamentos")]
    [ProducesResponseType(typeof(List<DepartamentoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListDepartamentos(CancellationToken cancellationToken = default)
        => (await sender.Send(new GetDepartamentosQuery(), cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/catalogs/municipios?departamentoId=N — Municipios por departamento</summary>
    [HttpGet("municipios")]
    [ProducesResponseType(typeof(List<MunicipioItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListMunicipios(
        [FromQuery] int departamentoId,
        CancellationToken cancellationToken = default)
        => (await sender.Send(new GetMunicipiosQuery(departamentoId), cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/catalogs/clusters?campoId=N — Clusters por campo</summary>
    [HttpGet("clusters")]
    [ProducesResponseType(typeof(List<ClusterItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListClusters(
        [FromQuery] int campoId,
        CancellationToken cancellationToken = default)
        => (await sender.Send(new GetClustersQuery(campoId), cancellationToken)).ToActionResult();

    /// <summary>POST /api/v1/catalogs/clusters — Crear cluster nuevo</summary>
    [HttpPost("clusters")]
    [Authorize(Roles = "ADMIN,OPERADOR")]
    [ProducesResponseType(typeof(ClusterItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCluster(
        [FromBody] CreateClusterCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult();
        return Created($"/api/v1/catalogs/clusters/{result.Value.Id}", result.Value);
    }
}
