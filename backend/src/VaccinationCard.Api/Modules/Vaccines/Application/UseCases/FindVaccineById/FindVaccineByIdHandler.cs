using MediatR;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Vaccines.Domain.Errors;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccineById;

public sealed class FindVaccineByIdHandler(IVaccineDao dao) : IRequestHandler<FindVaccineByIdQuery, VaccineDto>
{
    public async Task<VaccineDto> Handle(FindVaccineByIdQuery request, CancellationToken cancellationToken)
    {
        var output = await dao.FindByIdAsync(request.Id, cancellationToken);
        return output ?? throw VaccinesErrors.VaccineNotFound();
    }
}
