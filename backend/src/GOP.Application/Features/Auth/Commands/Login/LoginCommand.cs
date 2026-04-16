using GOP.Application.Features.Auth.Dtos;
using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<Result<TokenResponseDto>>;
