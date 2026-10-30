using MediatR;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;

namespace VaccinationCard.Api.Modules.Authentication.Application.UseCases.RefreshSession;

public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<SessionTokensDto>;
