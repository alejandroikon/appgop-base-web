using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Commands.TransitionWell;

public sealed record TransitionWellCommand(
    Guid WellId,
    string Action,
    string? Comment
) : IRequest<Result<TransitionResultDto>>;
