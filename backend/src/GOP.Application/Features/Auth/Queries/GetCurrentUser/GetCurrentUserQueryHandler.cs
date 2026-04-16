using GOP.Domain.Common;
using GOP.Domain.Interfaces.Services;
using MediatR;

namespace GOP.Application.Features.Auth.Queries.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler(
    ICurrentUserService currentUserService
) : IRequestHandler<GetCurrentUserQuery, Result<UserProfileDto>>
{
    public Task<Result<UserProfileDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var dto = new UserProfileDto(
            currentUserService.UserId,
            currentUserService.Email,
            currentUserService.Name,
            currentUserService.Role,
            currentUserService.TenantId.ToString(),
            currentUserService.TenantName);

        return Task.FromResult(Result.Success(dto));
    }
}
