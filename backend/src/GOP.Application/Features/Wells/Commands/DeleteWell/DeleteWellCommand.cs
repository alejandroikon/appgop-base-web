using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Wells.Commands.DeleteWell;

public sealed record DeleteWellCommand(Guid WellId) : IRequest<Result>;
