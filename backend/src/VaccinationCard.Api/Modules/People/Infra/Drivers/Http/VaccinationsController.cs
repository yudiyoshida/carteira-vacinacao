using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.UseCases.CorrectVaccinationDate;
using VaccinationCard.Api.Modules.People.Application.UseCases.DeleteVaccination;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationById;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationCard;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.People.Infra.Drivers.Http.Requests;

namespace VaccinationCard.Api.Modules.People.Infra.Drivers.Http;

// The vaccinations belong to the aggregate of the person, so the routes start at the person.
[ApiController]
[Route("api/v1/people/{personId:guid}")]
[Tags("Vacinações")]
public sealed class VaccinationsController(ISender sender) : ControllerBase
{
    [HttpGet("vaccinations/{vaccinationId:guid}")]
    [EndpointSummary("Consulta uma vacinação")]
    [ProducesResponseType<VaccinationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<VaccinationDto>> FindById(Guid personId, Guid vaccinationId, CancellationToken cancellationToken)
    {
        var output = await sender.Send(new FindVaccinationByIdQuery(personId, vaccinationId), cancellationToken);
        return Ok(output);
    }

    [HttpPost("vaccinations")]
    [EndpointSummary("Registra uma vacinação")]
    [ProducesResponseType<RegisterVaccinationOutput>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<RegisterVaccinationOutput>> Register(Guid personId, RegisterVaccinationRequest body, CancellationToken cancellationToken)
    {
        var command = new RegisterVaccinationCommand(personId, body.VaccineId, body.Dose, body.AppliedOn);
        var output = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(FindById), new { personId, vaccinationId = output.Id }, output);
    }

    [HttpPatch("vaccinations/{vaccinationId:guid}")]
    [EndpointSummary("Corrige a data de aplicação de uma vacinação")]
    [ProducesResponseType<SuccessMessage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<SuccessMessage>> CorrectDate(Guid personId, Guid vaccinationId, CorrectVaccinationDateRequest body, CancellationToken cancellationToken)
    {
        var command = new CorrectVaccinationDateCommand(personId, vaccinationId, body.AppliedOn);
        var output = await sender.Send(command, cancellationToken);
        return Ok(output);
    }

    [HttpDelete("vaccinations/{vaccinationId:guid}")]
    [EndpointSummary("Exclui uma vacinação")]
    [ProducesResponseType<SuccessMessage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<SuccessMessage>> Delete(Guid personId, Guid vaccinationId, CancellationToken cancellationToken)
    {
        var output = await sender.Send(new DeleteVaccinationCommand(personId, vaccinationId), cancellationToken);
        return Ok(output);
    }

    [HttpGet("vaccination-card")]
    [EndpointSummary("Consulta o cartão de vacinação")]
    [ProducesResponseType<VaccinationCardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<VaccinationCardDto>> FindCard(Guid personId, CancellationToken cancellationToken)
    {
        var output = await sender.Send(new FindVaccinationCardQuery(personId), cancellationToken);
        return Ok(output);
    }
}
