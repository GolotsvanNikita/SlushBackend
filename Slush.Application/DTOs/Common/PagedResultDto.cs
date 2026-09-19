namespace Slush.Application.DTOs.Common
{
    public record PagedResultDto<T>(
        IEnumerable<T> Items,
        int TotalCount,
        int Page,
        int PageSize
    );
}
