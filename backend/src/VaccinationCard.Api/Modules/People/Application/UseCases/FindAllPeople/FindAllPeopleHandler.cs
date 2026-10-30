using MediatR;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Shared.Pagination;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.FindAllPeople;

public sealed class FindAllPeopleHandler(IPersonDao dao) : IRequestHandler<FindAllPeopleQuery, Paginated<PersonSummaryDto>>
{
    // 20 people per page by default (RN19).
    private const int DefaultSize = 20;

    // The lists show the CPF masked (RN26).
    public async Task<Paginated<PersonSummaryDto>> Handle(FindAllPeopleQuery request, CancellationToken cancellationToken)
    {
        var page = await dao.FindAllAsync(request.Search?.Trim(), request.Page ?? 1, request.Size ?? DefaultSize, cancellationToken);
        return page with { Data = [.. page.Data.Select(person => person with { Cpf = Cpf.Create(person.Cpf).Masked })] };
    }
}
