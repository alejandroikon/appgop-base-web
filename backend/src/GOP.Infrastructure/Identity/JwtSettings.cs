namespace GOP.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; init; } = "GOP360";
    public string Audience { get; init; } = "GOP360-Client";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 30;
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
