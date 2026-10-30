using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;

// Vaccines has no aggregate, so its DAO also writes (ADR-0003).
public interface IVaccineDao
{
    Task<Paginated<VaccineDto>> FindAllAsync(string? search, int page, int? size, CancellationToken cancellationToken);

    Task<VaccineDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task SaveAsync(VaccineDto vaccine, CancellationToken cancellationToken);
}
