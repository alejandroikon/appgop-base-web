using GOP.Domain.Common;
using GOP.Domain.Enums;

namespace GOP.Domain.Entities;

public sealed class WellTransitionHistory : Entity
{
    public Guid WellId { get; private init; }
    public WellStatus FromState { get; private init; }
    public WellStatus ToState { get; private init; }
    public TransitionAction Action { get; private init; }
    public string? Comment { get; private init; }
    public Guid PerformedByUserId { get; private init; }
    public string PerformedByName { get; private init; } = string.Empty;
    public string PerformedByRole { get; private init; } = string.Empty;
    public DateTime CreatedAt { get; private init; }

    // Constructor privado para EF Core
    private WellTransitionHistory() { }

    public static WellTransitionHistory Create(
        Guid wellId,
        WellStatus from,
        WellStatus to,
        TransitionAction action,
        string? comment,
        Guid userId,
        string userName,
        string userRole)
    {
        return new WellTransitionHistory
        {
            WellId = wellId,
            FromState = from,
            ToState = to,
            Action = action,
            Comment = comment,
            PerformedByUserId = userId,
            PerformedByName = userName,
            PerformedByRole = userRole,
            CreatedAt = DateTime.UtcNow
        };
    }
}
