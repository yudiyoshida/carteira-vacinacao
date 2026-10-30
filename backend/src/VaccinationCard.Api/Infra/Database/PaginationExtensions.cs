using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.Api.Infra.Database;

public static class PaginationExtensions
{
    // Without a size, one page holds every item (TechSpec §7.1). A page past the last one comes back empty without
    // reading the items, and the offset is a long, so a huge page number does not overflow.
    public static async Task<Paginated<T>> ToPaginatedAsync<T>(this IQueryable<T> query, int page, int? size, CancellationToken cancellationToken)
    {
        var totalItems = await query.CountAsync(cancellationToken);
        var itemsPerPage = size ?? totalItems;
        var offset = (long)(page - 1) * itemsPerPage;

        IReadOnlyList<T> data = offset < totalItems
            ? await query.Skip((int)offset).Take(itemsPerPage).ToListAsync(cancellationToken)
            : [];

        return Paginated<T>.Create(data, totalItems, page, itemsPerPage);
    }
}
