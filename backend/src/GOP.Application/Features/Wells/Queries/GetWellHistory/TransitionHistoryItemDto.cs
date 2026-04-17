namespace GOP.Application.Features.Wells.Queries.GetWellHistory;

public sealed record TransitionHistoryItemDto(
    Guid Id,
    string FromState,
    string ToState,
    string Action,
    string? Comment,
    Guid PerformedBy,
    string PerformedByName,
    string PerformedByRole,
    DateTime CreatedAt
);
