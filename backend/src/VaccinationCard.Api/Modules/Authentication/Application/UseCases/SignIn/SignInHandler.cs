using MediatR;
using VaccinationCard.Api.Modules.Accounts.Application.UseCases.VerifyAccountCredentials;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Services;
using VaccinationCard.Api.Modules.Authentication.Domain.Errors;

namespace VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;

public sealed class SignInHandler(ISender sender, RefreshTokenSession session) : IRequestHandler<SignInCommand, SessionTokensDto>
{
    public async Task<SessionTokensDto> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var account = await sender.Send(new VerifyAccountCredentialsQuery(request.Email, request.Password), cancellationToken)
            ?? throw AuthenticationErrors.InvalidCredentials();

        return await session.IssueAsync(account.Id, cancellationToken);
    }
}
