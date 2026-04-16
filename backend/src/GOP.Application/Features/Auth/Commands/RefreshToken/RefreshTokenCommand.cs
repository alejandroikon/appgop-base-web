using GOP.Application.Features.Auth.Dtos;
using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken) : IRequest<Result<TokenResponseDto>>;
