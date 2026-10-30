using MediatR;
using VaccinationCard.Api.Core.Messages;

namespace VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignOut;

public sealed record SignOutCommand(Guid AccountId) : IRequest<SuccessMessage>;
