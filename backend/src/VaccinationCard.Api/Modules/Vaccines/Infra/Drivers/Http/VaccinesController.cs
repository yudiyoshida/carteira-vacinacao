using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindAllVaccines;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccineById;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.Api.Modules.Vaccines.Infra.Drivers.Http;

// A vaccine is neither edited (RN07) nor removed (PRD §4.2): without PUT, PATCH or DELETE, routing answers 405.
[ApiController]
[Route("api/v1/vaccines")]
[Tags("Vacinas")]
public sealed class VaccinesController(ISender sender) : ControllerBase
{
    // The parameters keep the names of the query string in the OpenAPI document and in the keys of "errors".
    [HttpGet]
    [EndpointSummary("Lista as vacinas")]
    [ProducesResponseType<Paginated<VaccineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<Paginated<VaccineDto>>> FindAll(int? page, int? size, string? search, CancellationToken cancellationToken)
    {
        var output = await sender.Send(new FindAllVaccinesQuery(page, size, search), cancellationToken);
        return Ok(output);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Consulta uma vacina")]
    [ProducesResponseType<VaccineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<VaccineDto>> FindById(Guid id, CancellationToken cancellationToken)
    {
        var output = await sender.Send(new FindVaccineByIdQuery(id), cancellationToken);
        return Ok(output);
    }

    [HttpPost]
    [EndpointSummary("Cadastra uma vacina")]
    [ProducesResponseType<CreateVaccineOutput>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CreateVaccineOutput>> Create(CreateVaccineCommand command, CancellationToken cancellationToken)
    {
        var output = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(FindById), new { id = output.Id }, output);
    }
}
