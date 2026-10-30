using MediatR;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindAllVaccines;

public sealed class FindAllVaccinesHandler(IVaccineDao dao) : IRequestHandler<FindAllVaccinesQuery, Paginated<VaccineDto>>
{
    // Without a size, one page holds every vaccine (RN20).
    public Task<Paginated<VaccineDto>> Handle(FindAllVaccinesQuery request, CancellationToken cancellationToken)
    {
        return dao.FindAllAsync(request.Search?.Trim(), request.Page ?? 1, request.Size, cancellationToken);
    }
}
