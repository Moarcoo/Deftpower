namespace Deftpower.Onb.Assessment.Pagination;

public class PaginationParams(int page = 1, int pageSize = PaginationParams.DefaultPageSize)
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public int Page { get; init; } = page;

    public int PageSize { get; init; } = pageSize;

    public int Skip => (Page - 1) * PageSize;

    public bool IsValid()
    {
        if (Page < 1) return false;
        if (PageSize is < 1 or > MaxPageSize) return false;
        return true;
    }
}
