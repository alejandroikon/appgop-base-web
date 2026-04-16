namespace GOP.Application.Common;

public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize
)
{
    public bool HasNextPage => Page * PageSize < Total;
    public bool HasPreviousPage => Page > 1;
}
