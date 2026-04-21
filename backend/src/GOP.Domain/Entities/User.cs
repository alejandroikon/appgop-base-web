using GOP.Domain.Common;

namespace GOP.Domain.Entities;

public sealed class User : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Constructor privado para EF Core
    private User() { }

    public static User Create(string email, string passwordHash, string fullName)
        => new()
        {
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
