using MediatR;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;

namespace VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;

public sealed record SignInCommand(string Email, string Password) : IRequest<SessionTokensDto>;
