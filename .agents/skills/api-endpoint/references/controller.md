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
- A exclusão declara `[HttpDelete("{id:guid}")]`, monta o command com o id e responde `Ok(output)`, com o `SuccessMessage` como tipo do `200`: `Delete`, em `backend/src/VaccinationCard.Api/Modules/People/Infra/Drivers/Http/PeopleController.cs`.
- O que o recurso não aceita fica sem action. A vacina não tem edição (RN07) nem remoção (PRD §4.2), então o roteamento responde `405` a `PUT`, `PATCH` e `DELETE` em `/api/v1/vaccines/{id}`, como registra o comentário da classe.

## Rota com parâmetro e corpo

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Infra/Drivers/Http/VaccinationsController.cs` e `Requests/RegisterVaccinationRequest.cs`, na mesma pasta.

- A rota base do controller é a raiz do agregado, `api/v1/people/{personId:guid}`, e cada action completa o caminho (`vaccinations`, `vaccinations/{vaccinationId:guid}`, `vaccination-card`). Uma rota nova da pessoa entra no mesmo controller, sem rota absoluta, como o cartão: `FindCard`, com `[HttpGet("vaccination-card")]`.
- O corpo vira um `<Nome>Request`, num arquivo próprio em `Infra/Drivers/Http/Requests`, e o action monta o command com o id da rota: `new RegisterVaccinationCommand(personId, body.VaccineId, body.Dose, body.AppliedOn)`.
- O `CreatedAtAction` leva os dois ids da rota da consulta: `CreatedAtAction(nameof(FindById), new { personId, vaccinationId = output.Id }, output)`.
- A edição segue o mesmo formato: `CorrectDate`, com `[HttpPatch("vaccinations/{vaccinationId:guid}")]`, recebe o `CorrectVaccinationDateRequest`, monta o command com os dois ids da rota e responde `Ok(output)`, com o `SuccessMessage` como tipo do `200`.
- A exclusão tem a mesma forma, sem corpo: `Delete`, com `[HttpDelete("vaccinations/{vaccinationId:guid}")]`.
- Os `[ProducesResponseType]` seguem a linha da rota na TechSpec §7.2, e não só os erros que o caso de uso lança hoje: o `409` do `PATCH` e do `DELETE` é o da concorrência (RN16).

## Rota anônima com cookie

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Authentication/Infra/Drivers/Http/AuthController.cs`.

- `[AllowAnonymous]` fica na action, porque o logout, no mesmo controller, exige o access token.
- O corpo é igual ao command, então a action recebe o `SignInCommand`. O caso de uso devolve o `SessionTokensDto`, e a action separa os dois tokens: o refresh token vai para o cookie, e só o `AccessTokenDto` vai para o corpo (TechSpec §5.4).
- Os atributos do cookie ficam num helper privado, com a validade do próprio token. O `Secure` vem de `RefreshCookie:Secure` e fica ligado quando a chave falta.
- O `429` já aparece nos `[ProducesResponseType]`, porque eles seguem a linha da rota na TechSpec §7.2.

## Enum no corpo

Código real: `backend/src/VaccinationCard.Api/Infra/Http/HttpPipelineExtensions.cs`.

- O enum entra e sai pelo nome, e um número no lugar do nome é recusado no model binding (TechSpec §7.1): `JsonStringEnumConverter(allowIntegerValues: false)`.
- O helper `UseEnumNames` aplica o conversor nas opções do MVC (`AddJsonOptions`), que leem e escrevem os corpos, e nas opções do HTTP (`ConfigureHttpJsonOptions`), que o documento OpenAPI usa para descrever os schemas. Sem as opções do HTTP, o OpenAPI descreveria o enum como número; o `OpenApiTests` confere os nomes.

## Exemplo no arquivo .http

Exemplo real: `backend/VaccinationCard.http`.

- A variável `@baseUrl` fica no topo do arquivo.
- Cada rota tem um bloco, com o título em português: `### Cadastrar vacina`.
- O bloco que cria um recurso leva `# @name`, e o bloco seguinte lê o id com `{{<nome>.response.body.$.id}}`.
- O bloco de lista mostra os parâmetros: `?search=hepatite&page=1&size=10`.
- A variável `@accessToken` lê o token do bloco de login (`# @name login`), e cada rota protegida leva `Authorization: Bearer {{accessToken}}` (skill `authentication`).
