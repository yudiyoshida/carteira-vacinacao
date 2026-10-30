using MediatR;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.Persistence.Dao;

namespace VaccinationCard.Api.Modules.Accounts.Application.UseCases.FindAccountById;

public sealed class FindAccountByIdHandler(IAccountDao dao) : IRequestHandler<FindAccountByIdQuery, AccountDto?>
{
    public Task<AccountDto?> Handle(FindAccountByIdQuery request, CancellationToken cancellationToken)
    {
        return dao.FindByIdAsync(request.Id, cancellationToken);
    }
}
