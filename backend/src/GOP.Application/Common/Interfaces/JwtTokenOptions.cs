namespace GOP.Application.Common.Interfaces;

public sealed class JwtTokenOptions
{
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
