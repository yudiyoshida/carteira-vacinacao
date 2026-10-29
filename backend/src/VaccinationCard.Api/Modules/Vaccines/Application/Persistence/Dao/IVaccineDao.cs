using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

namespace VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;

// Vaccines has no aggregate, so its DAO also writes (ADR-0003).
public interface IVaccineDao
{
    Task<VaccineDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    // Compares the names without case and without the spaces at the ends (RN05).
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task SaveAsync(VaccineDto vaccine, CancellationToken cancellationToken);
}
