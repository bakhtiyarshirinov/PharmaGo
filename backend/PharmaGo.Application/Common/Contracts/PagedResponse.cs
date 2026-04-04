namespace PharmaGo.Application.Common.Contracts;

public class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}
