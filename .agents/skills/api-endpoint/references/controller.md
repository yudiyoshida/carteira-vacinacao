# Controller

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

## Controller de um recurso

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Infra/Drivers/Http/VaccinesController.cs`.

- A classe é `sealed`, com `[ApiController]`, `[Route("api/v1/vaccines")]` e `[Tags("Vacinas")]`, e recebe o `ISender` pelo construtor primário.
- Cada action declara o verbo, como `[HttpGet("{id:guid}")]`, o `[EndpointSummary]` em português e um `[ProducesResponseType]` por status:
  - o sucesso leva o tipo da saída: `[ProducesResponseType<VaccineDto>(StatusCodes.Status200OK)]`;
  - o `400` leva `ValidationProblemDetails`, e os outros erros, `ProblemDetails`, todos com `"application/problem+json"`.
- O action tem corpo em bloco: guarda o resultado do `sender.Send` em `output` e responde `Ok(output)`.
- A lista recebe `int? page, int? size, string? search` e monta a `FindAllVaccinesQuery`.
- A criação recebe o command como corpo e responde `CreatedAtAction(nameof(FindById), new { id = output.Id }, output)`.
- O que o recurso não aceita fica sem action. A vacina não tem edição (RN07) nem remoção (PRD §4.2), então o roteamento responde `405` a `PUT`, `PATCH` e `DELETE` em `/api/v1/vaccines/{id}`, como registra o comentário da classe.

## Rota com parâmetro e corpo

Modelo para a T14. O corpo vira um `<Nome>Request`, num arquivo próprio em `Infra/Drivers/Http/Requests`, e o action monta o command:

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

Exemplo real: `backend/VaccinationCard.http`.

- A variável `@baseUrl` fica no topo do arquivo.
- Cada rota tem um bloco, com o título em português: `### Cadastrar vacina`.
- O bloco que cria um recurso leva `# @name`, e o bloco seguinte lê o id com `{{<nome>.response.body.$.id}}`.
- O bloco de lista mostra os parâmetros: `?search=hepatite&page=1&size=10`.
- A partir da T19, o arquivo ganha o bloco de login, e cada rota protegida leva `Authorization: Bearer {{accessToken}}` (skill `authentication`).
