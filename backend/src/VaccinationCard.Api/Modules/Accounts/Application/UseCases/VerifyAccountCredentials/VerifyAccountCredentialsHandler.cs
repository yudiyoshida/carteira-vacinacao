using MediatR;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Accounts.Application.Security;

namespace VaccinationCard.Api.Modules.Accounts.Application.UseCases.VerifyAccountCredentials;

public sealed class VerifyAccountCredentialsHandler(IAccountDao dao, IPasswordHasher passwordHasher) : IRequestHandler<VerifyAccountCredentialsQuery, AccountDto?>
{
    public async Task<AccountDto?> Handle(VerifyAccountCredentialsQuery request, CancellationToken cancellationToken)
    {
        var credentials = await dao.FindCredentialsByEmailAsync(request.Email, cancellationToken);

        // The check runs even without an account, so the response time does not reveal the e-mail (RN22).
        var verified = passwordHasher.Verify(credentials?.PasswordHash, request.Password);

        return verified && credentials is not null ? new AccountDto(credentials.Id, credentials.Email) : null;
    }
}
