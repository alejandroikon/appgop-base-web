namespace GOP.API.Contracts;

/// <summary>
/// DTO de body para PATCH /api/v1/wells/{id}/transition.
/// Desacopla el contrato HTTP del Command de la capa Application.
/// </summary>
public sealed record TransitionWellRequest(string Action, string? Comment);
