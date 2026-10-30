using MediatR;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Domain.Errors;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationById;

public sealed class FindVaccinationByIdHandler(IPersonDao dao) : IRequestHandler<FindVaccinationByIdQuery, VaccinationDto>
{
    public async Task<VaccinationDto> Handle(FindVaccinationByIdQuery request, CancellationToken cancellationToken)
    {
        var vaccination = await dao.FindVaccinationAsync(request.PersonId, request.VaccinationId, cancellationToken);
        if (vaccination is not null)
        {
            return vaccination;
        }

        // An unknown person comes first, as in the routes that load the aggregate (TechSpec §5.2).
        var person = await dao.FindByIdAsync(request.PersonId, cancellationToken);
        throw person is null ? PeopleErrors.PersonNotFound() : PeopleErrors.VaccinationNotFound();
    }
}
