using GOP.API.Extensions;
using GOP.Application.Features.Catalogs.Queries.GetCampos;
using GOP.Application.Features.Catalogs.Queries.GetClusters;
using GOP.Application.Features.Catalogs.Queries.GetContratos;
using GOP.Application.Features.Catalogs.Queries.GetDepartamentos;
using GOP.Application.Features.Catalogs.Queries.GetMunicipios;
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
    /// <summary>GET /api/v1/catalogs/contratos — HU-025: Listar contratos</summary>
    [HttpGet("contratos")]
    [ProducesResponseType(typeof(List<ContratoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListContratos(CancellationToken cancellationToken = default)
        => (await sender.Send(new GetContratosQuery(), cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/catalogs/campos?contratoId=N — HU-025: Campos por contrato</summary>
    [HttpGet("campos")]
    [ProducesResponseType(typeof(List<CampoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListCampos(
        [FromQuery] int contratoId,
        CancellationToken cancellationToken = default)
        => (await sender.Send(new GetCamposQuery(contratoId), cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/catalogs/departamentos — HU-025: Listar departamentos</summary>
    [HttpGet("departamentos")]
    [ProducesResponseType(typeof(List<DepartamentoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListDepartamentos(CancellationToken cancellationToken = default)
        => (await sender.Send(new GetDepartamentosQuery(), cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/catalogs/municipios?departamentoId=N — HU-025: Municipios por departamento</summary>
    [HttpGet("municipios")]
    [ProducesResponseType(typeof(List<MunicipioItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListMunicipios(
        [FromQuery] int departamentoId,
        CancellationToken cancellationToken = default)
        => (await sender.Send(new GetMunicipiosQuery(departamentoId), cancellationToken)).ToActionResult();

    /// <summary>GET /api/v1/catalogs/clusters?campoId=N — HU-025: Clusters por campo</summary>
    [HttpGet("clusters")]
    [ProducesResponseType(typeof(List<ClusterItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListClusters(
        [FromQuery] int campoId,
        CancellationToken cancellationToken = default)
        => (await sender.Send(new GetClustersQuery(campoId), cancellationToken)).ToActionResult();
}
