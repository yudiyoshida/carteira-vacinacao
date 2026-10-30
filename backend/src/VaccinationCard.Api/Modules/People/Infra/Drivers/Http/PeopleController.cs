using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindPersonById;

namespace VaccinationCard.Api.Modules.People.Infra.Drivers.Http;

// The CPF travels only in the request body, never in the route or the query string (RN25).
[ApiController]
[Route("api/v1/people")]
[Tags("Pessoas")]
public sealed class PeopleController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Consulta uma pessoa")]
    [ProducesResponseType<PersonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PersonDto>> FindById(Guid id, CancellationToken cancellationToken)
    {
        var output = await sender.Send(new FindPersonByIdQuery(id), cancellationToken);
        return Ok(output);
    }

    [HttpPost]
    [EndpointSummary("Cadastra uma pessoa")]
    [ProducesResponseType<CreatePersonOutput>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CreatePersonOutput>> Create(CreatePersonCommand command, CancellationToken cancellationToken)
    {
        var output = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(FindById), new { id = output.Id }, output);
    }
}
