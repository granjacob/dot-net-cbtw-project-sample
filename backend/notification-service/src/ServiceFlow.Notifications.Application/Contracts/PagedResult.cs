namespace ServiceFlow.Notifications.Application.Contracts;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    long Total,
    int Page,
    int PageSize)
{
    public int TotalPages => Total == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
