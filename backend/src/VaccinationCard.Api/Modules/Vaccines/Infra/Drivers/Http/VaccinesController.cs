using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccineById;

namespace VaccinationCard.Api.Modules.Vaccines.Infra.Drivers.Http;

// A vaccine is neither edited (RN07) nor removed (PRD §4.2): without PUT, PATCH or DELETE, routing answers 405.
[ApiController]
[Route("api/v1/vaccines")]
[Tags("Vacinas")]
public sealed class VaccinesController(ISender sender) : ControllerBase
{
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
