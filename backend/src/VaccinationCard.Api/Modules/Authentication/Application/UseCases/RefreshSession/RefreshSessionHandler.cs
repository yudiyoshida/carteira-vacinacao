using MediatR;
using VaccinationCard.Api.Modules.Accounts.Application.UseCases.FindAccountById;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Services;
using VaccinationCard.Api.Modules.Authentication.Domain.Errors;

namespace VaccinationCard.Api.Modules.Authentication.Application.UseCases.RefreshSession;

public sealed class RefreshSessionHandler(ISender sender, RefreshTokenSession session) : IRequestHandler<RefreshSessionCommand, SessionTokensDto>
{
    public async Task<SessionTokensDto> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var accountId = await session.ValidateAsync(request.RefreshToken, cancellationToken);

        var account = await sender.Send(new FindAccountByIdQuery(accountId), cancellationToken)
            ?? throw AuthenticationErrors.InvalidRefreshToken();

        return await session.IssueAsync(account.Id, cancellationToken);
    }
}
