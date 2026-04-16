using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<UserProfileDto>>;
