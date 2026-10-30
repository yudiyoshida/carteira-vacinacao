using MediatR;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.Authentication.Application.Services;

namespace VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignOut;

public sealed class SignOutHandler(RefreshTokenSession session) : IRequestHandler<SignOutCommand, SuccessMessage>
{
    public async Task<SuccessMessage> Handle(SignOutCommand request, CancellationToken cancellationToken)
    {
        await session.RevokeAsync(request.AccountId, cancellationToken);

        return new SuccessMessage("Sessão encerrada com sucesso.");
    }
}
