using MediatR;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccinesByIds;

public sealed class FindVaccinesByIdsHandler(IVaccineDao dao) : IRequestHandler<FindVaccinesByIdsQuery, IReadOnlyList<VaccineDto>>
{
    // An unknown id is left out, without an error: the ids come from vaccinations, whose foreign key keeps their
    // vaccines in place (TechSpec §6.1).
    public Task<IReadOnlyList<VaccineDto>> Handle(FindVaccinesByIdsQuery request, CancellationToken cancellationToken)
    {
        return dao.FindByIdsAsync(request.Ids, cancellationToken);
    }
}
