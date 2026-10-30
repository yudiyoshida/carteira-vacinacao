namespace VaccinationCard.Api.Shared.Pagination;

// The envelope of every list (TechSpec §7.1); the JSON keeps the order of the fields.
public sealed record Paginated<T>(int CurrentPage, int ItemsPerPage, int TotalItems, int TotalPages, IReadOnlyList<T> Data)
{
    // Without a size, an empty list has a page size of 0; an empty list has no pages either way (TechSpec §7.1).
    public static Paginated<T> Create(IReadOnlyList<T> data, int totalItems, int currentPage, int itemsPerPage)
    {
        var totalPages = itemsPerPage == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)itemsPerPage);
        return new Paginated<T>(currentPage, itemsPerPage, totalItems, totalPages, data);
    }
}
