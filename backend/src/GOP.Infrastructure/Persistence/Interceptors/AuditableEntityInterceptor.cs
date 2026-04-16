using GOP.Domain.Common;
using GOP.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GOP.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(
    ICurrentUserService currentUser
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            UpdateAuditFields(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            UpdateAuditFields(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void UpdateAuditFields(DbContext context)
    {
        var now = DateTime.UtcNow;
        var userEmail = currentUser.IsAuthenticated ? currentUser.Email : "system";

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userEmail;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAt = now;
                    entry.Entity.LastModifiedBy = userEmail;

                    // Si IsDeleted cambió a true, registrar DeletedAt
                    if (entry.Property(e => e.IsDeleted).IsModified && entry.Entity.IsDeleted)
                        entry.Entity.DeletedAt = now;
                    break;
            }
        }
    }
}
