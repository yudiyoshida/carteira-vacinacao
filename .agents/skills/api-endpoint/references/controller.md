# Controller

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

```csharp
namespace VaccinationCard.Api.Modules.Vaccines.Infra.Drivers.Http;

[ApiController]
[Route("api/v1/vaccines")]
[Tags("Vacinas")]
public sealed class VaccinesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lista as vacinas")]
    [ProducesResponseType<Paginated<VaccineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<Paginated<VaccineDto>>> FindAll(
        [FromQuery] FindAllVaccinesQuery query, CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [EndpointSummary("Consulta uma vacina")]
    [ProducesResponseType<VaccineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<VaccineDto>> FindById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new FindVaccineByIdQuery(id), cancellationToken));

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
```

## Rota com parâmetro e corpo

O corpo vira um `<Nome>Request`, num arquivo próprio em `Infra/Drivers/Http/Requests`, e o action monta o command:

```csharp
public sealed record RegisterVaccinationRequest(Guid VaccineId, DoseDto Dose, DateOnly AppliedOn);
```

```csharp
[HttpPost]
public async Task<ActionResult<RegisterVaccinationOutput>> Register(
    Guid personId, RegisterVaccinationRequest body, CancellationToken cancellationToken)
{
    var command = new RegisterVaccinationCommand(personId, body.VaccineId, body.Dose, body.AppliedOn);
    var output = await sender.Send(command, cancellationToken);
    return CreatedAtAction(nameof(FindById), new { personId, vaccinationId = output.Id }, output);
}
```

## Exemplo no arquivo .http

Um bloco por rota em `backend/VaccinationCard.http`, com o título em português:

```http
### Cadastrar vacina
# @name createVaccine
POST {{baseUrl}}/api/v1/vaccines
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "name": "Hepatite A",
  "primaryDoses": 2,
  "boosterDoses": 0
}

### Consultar vacina
GET {{baseUrl}}/api/v1/vaccines/{{createVaccine.response.body.$.id}}
Authorization: Bearer {{accessToken}}
```

Uma rota com id usa a resposta de outra chamada: o bloco que cria o recurso leva `# @name`, e o bloco seguinte lê o id com `{{<nome>.response.body.$.id}}`.
