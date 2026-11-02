using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.DeletePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindAllPeople;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindPersonById;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.Api.Modules.People.Infra.Drivers.Http;

// The CPF travels only in the request body, never in the route or the query string (RN25).
[ApiController]
[Route("api/v1/people")]
[Tags("Pessoas")]
public sealed class PeopleController(ISender sender) : ControllerBase
{
    // The parameters keep the names of the query string in the OpenAPI document and in the keys of "errors".
    [HttpGet]
    [EndpointSummary("Lista as pessoas")]
    [ProducesResponseType<Paginated<PersonSummaryDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<Paginated<PersonSummaryDto>>> FindAll(int? page, int? size, string? search, CancellationToken cancellationToken)
    {
        var output = await sender.Send(new FindAllPeopleQuery(page, size, search), cancellationToken);
        return Ok(output);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Consulta uma pessoa")]
    [ProducesResponseType<PersonDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PersonDto>> FindById(Guid id, CancellationToken cancellationToken)
    {
        var output = await sender.Send(new FindPersonByIdQuery(id), cancellationToken);
        return Ok(output);
    }

    [HttpPost]
    [EndpointSummary("Cadastra uma pessoa")]
    [ProducesResponseType<CreatePersonOutput>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CreatePersonOutput>> Create(CreatePersonCommand command, CancellationToken cancellationToken)
    {
        var output = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(FindById), new { id = output.Id }, output);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Remove uma pessoa")]
    [ProducesResponseType<SuccessMessage>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SuccessMessage>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var output = await sender.Send(new DeletePersonCommand(id), cancellationToken);
        return Ok(output);
    }
}
