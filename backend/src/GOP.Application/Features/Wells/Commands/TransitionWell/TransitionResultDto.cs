namespace GOP.Application.Features.Wells.Commands.TransitionWell;

public sealed record TransitionResultDto(
    Guid Id,
    string Estado,
    string EstadoAnterior,
    string? Uwi,
    string Action,
    string? Comment,
    DateTime TransitionedAt,
    string TransitionedBy
);
