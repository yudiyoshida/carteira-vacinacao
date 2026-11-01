# Testes manuais da API

Roteiro para conferir, no Scalar, se a API atende ao [PRD](PRD.md) e à [TechSpec](TECHSPEC.md). O resultado esperado de cada passo vem desses documentos. Um resultado diferente é um achado: anote o passo, o status e o corpo da resposta.

Siga a ordem das seções. Elas compartilham os dados criados, e as listas e contagens partem de um banco de dados limpo.

## 1. Preparação

### 1.1 Subir o ambiente

```bash
docker compose up db
rm -f /tmp/api-manual.log
dotnet run --project backend/src/VaccinationCard.Api 2>&1 | tee -a /tmp/api-manual.log
```

- Scalar: `http://localhost:8080/scalar`.
- A carga de demonstração só preenche tabelas vazias (TechSpec §6.4), e o roteiro precisa de um banco de dados limpo. **`docker compose down -v` apaga todos os dados locais do volume `db-data`**; rode esse comando só se puder perder esses dados.
- Use o Chrome. O cookie do refresh token é `Secure`, e outro navegador pode recusá-lo em `http://localhost`. Nesse caso, suba a API com `RefreshCookie__Secure=false`.
- Deixe o DevTools aberto na aba **Network**. O navegador esconde o `Set-Cookie` do JavaScript, e a seção **Cookies** da resposta do Scalar mostra "No cookies" mesmo quando o cookie foi gravado.

### 1.2 Usar o Scalar

1. Na barra lateral, abra a rota e clique em **Test Request**.
2. Em **Request Body**, troque o exemplo pelo JSON do passo e clique em **Send**. Confira o corpo antes de cada envio.
3. Um `{id}` na rota aparece em **Variables**.
4. `page`, `size` e `search` aparecem em **Query Parameters**. Digitar um valor marca a linha, e o Scalar codifica o valor: digite `%`, e não `%25`. Os valores continuam lá ao reabrir a rota; desmarque o que não for mais usado.
5. Status, corpo e headers da resposta ficam no painel à direita.

O campo **Authentication > Bearer Token** continua preenchido ao abrir outra rota, mas **recarregar a página apaga o campo**. Com o campo vazio (botão **X**), o Scalar envia `Authorization: Bearer`, sem token.

O cliente do Scalar não faz o que segue; estes passos usam `curl`:

- trocar o método ou o caminho da rota;
- enviar um valor próprio de cookie: a tabela **Cookies** não altera o cookie enviado;
- `GET /health` não aparece na barra lateral: abra a URL numa aba.

### 1.3 Entrar

Em **Autenticação > Abre a sessão do operador**, envie:

```json
{ "email": "operador@demo.local", "password": "Demo@2026!" }
```

Cole o `accessToken` em **Bearer Token** e exporte o mesmo valor para os passos em `curl`:

```bash
export TOKEN='cole-o-access-token-aqui'
```

O access token vale 15 minutos. Um `401` inesperado pede um novo login.

### 1.4 Convenções

- Marque `[x]` quando o resultado bater com o esperado.
- `400 errors.campo` significa `400`, `code` `validation.failed` e a chave `campo` em `errors`.
- Os códigos e as mensagens vêm da TechSpec §8.3. Toda falha responde `application/problem+json`, com `title`, `status`, `detail`, `instance`, `code` e `traceId`. Confira esses campos em pelo menos uma resposta de cada status.
- `HOJE` é a data atual em Brasília, e `AMANHÃ` é o dia seguinte, ambas em `yyyy-MM-dd`.
- `AUSENTE` é um id que não existe: `00000000-0000-7000-8000-000000000000`.

Anote os valores que os passos reutilizam:

| Variável | Origem | Valor |
|---|---|---|
| `TOKEN` | 1.3, A1, A6, A11, A16 | |
| `BCG`, `FEBRE` | D1 | |
| `MARIA` | D2 | |
| `COOKIE_1`, `COOKIE_2`, `COOKIE_3` | A2, A9, A11 | |
| `VACINA` | V1 | |
| `PESSOA`, `JOAO` | P1, P5 | |
| `DOSE1`, `DOSE2`, `REFORCO1` | R10, R14, R15 | |

Textos de tamanho exato:

| Apelido | Texto |
|---|---|
| `V100` | `VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV` |
| `V101` | `VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV` |
| `Z150` | `ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ` |
| `Z151` | `ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ` |
| `A101` | `aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa` |

## 2. Saúde e pipeline HTTP

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | S1 | abrir `http://localhost:8080/health` numa aba | `200` | RNF08 |
| [ ] | S2 | `docker compose stop db`; recarregar `/health`; `docker compose start db`; recarregar até responder | `503`, depois `200` | RNF08 |
| [ ] | S3 | Scalar: **Lista as pessoas**, com **Bearer Token** vazio | `401` `auth.unauthenticated`, com `WWW-Authenticate: Bearer` | RN21 |
| [ ] | S4 | Scalar: a mesma rota, com **Bearer Token** `abc`; depois, cole o token de volta | `401` `auth.unauthenticated` | TechSpec §9 |
| [ ] | S5 | `curl`: rota inexistente, sem token | `401` `auth.unauthenticated` | TechSpec §7.2 |
| [ ] | S6 | `curl`: rota inexistente, com token | `404` `route.not_found` | TechSpec §7.2 |
| [ ] | S7 | `curl`: `PUT`, `PATCH` e `DELETE` em `/api/v1/vaccines` e em `/api/v1/vaccines/{BCG}` | `405` `route.method_not_allowed` | RN07 |
| [ ] | S8 | Scalar: **Cadastra uma vacina**; em **Request Body**, trocar `JSON` por `XML`; depois, voltar para `JSON` | `415` `request.unsupported_media_type` | TechSpec §8.3 |
| [ ] | S9 | Scalar: a mesma rota, com o corpo `abc` e, depois, `{` | `400 errors.body`, "O corpo da requisição é inválido." | TechSpec §8.1 |
| [ ] | S10 | `curl`: a mesma rota, com `Content-Type: application/json` e sem corpo | `400 errors.body` | TechSpec §8.1 |
| [ ] | S11 | Scalar: a mesma rota, com o corpo `{ "name": ` | `400 errors.name`, "O valor informado é inválido.": o JSON acabou dentro do campo | TechSpec §8.1 |
| [ ] | S12 | Scalar: a mesma rota, com `{ "name": 123, "primaryDoses": 1, "boosterDoses": 0 }` | `400 errors.name`, "O valor informado é inválido." | TechSpec §8.1 |
| [ ] | S13 | conferir nas respostas de S1 a S12 e em uma resposta `200` | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` e `Referrer-Policy: no-referrer` | TechSpec §9 |

Comandos de S5, S6, S7 e S10 (o `BCG` vem do passo D1):

```bash
curl -i http://localhost:8080/api/v1/nao-existe
curl -i http://localhost:8080/api/v1/nao-existe -H "Authorization: Bearer $TOKEN"

curl -i -X PUT    http://localhost:8080/api/v1/vaccines -H "Authorization: Bearer $TOKEN"
curl -i -X PATCH  http://localhost:8080/api/v1/vaccines -H "Authorization: Bearer $TOKEN"
curl -i -X DELETE http://localhost:8080/api/v1/vaccines -H "Authorization: Bearer $TOKEN"

curl -i -X PUT    http://localhost:8080/api/v1/vaccines/$BCG -H "Authorization: Bearer $TOKEN"
curl -i -X PATCH  http://localhost:8080/api/v1/vaccines/$BCG -H "Authorization: Bearer $TOKEN"
curl -i -X DELETE http://localhost:8080/api/v1/vaccines/$BCG -H "Authorization: Bearer $TOKEN"

curl -i -X POST http://localhost:8080/api/v1/vaccines -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json'
```

## 3. Dados de demonstração (PRD §9)

| | ID | Ação | Esperado |
|---|---|---|---|
| [ ] | D1 | **Lista as vacinas**, sem parâmetros; anote `BCG` e `FEBRE` | por nome: BCG (1 + 0), DTP (3 + 2), Febre amarela (1 + 0), Hepatite B (3 + 0), Meningocócica C (2 + 1), Pneumocócica 10-valente (2 + 1), Poliomielite inativada (VIP) (3 + 1) e Tríplice viral (2 + 0); envelope com `currentPage` 1, `itemsPerPage` 8, `totalItems` 8 e `totalPages` 1 |
| [ ] | D2 | **Lista as pessoas**; anote `MARIA` | só `Maria Demonstração`, com CPF `***.456.789-**` |
| [ ] | D3 | **Consulta uma pessoa**, com `MARIA` | campos `id`, `name` e `cpf`; CPF `123.456.789-09` |
| [ ] | D4 | **Consulta o cartão de vacinação**, com `MARIA` | por nome: BCG, DTP e Hepatite B. BCG: `nextExpectedDose` `null`. DTP: 3/3 primárias, série completa, 1/2 reforços e próxima `Booster 2`. Hepatite B: 2/3 primárias, série incompleta e próxima `Primary 3` |
| [ ] | D5 | parar a API (`Ctrl+C`), subir de novo com o comando de 1.1 e repetir D1 e D2 | os mesmos dados, sem duplicar |

O reinício de D5 apaga o token só se você recarregar a página. O token continua válido na API.

## 4. Autenticação (F01)

O limite é de 5 tentativas de login por minuto, contando as válidas e as inválidas (RN24). Se um passo responder `429` antes de A15, espere um minuto e repita o passo.

Rotas no Scalar: **Autenticação > Abre a sessão do operador**, **Renova a sessão do operador** e **Encerra a sessão do operador**. O reinício de D5 zerou o contador de tentativas.

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | A1 | login com o corpo de 1.3; cole o novo token | `200`, só com `accessToken` | RN22 |
| [ ] | A2 | no Network, abrir a resposta de A1, ler o `Set-Cookie` e guardar o valor de `refresh_token` como `COOKIE_1` | `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth` e validade de 7 dias | TechSpec §9 |
| [ ] | A3 | login com `{ "email": "operador@demo.local", "password": "Senha@Errada1" }` | `401` `auth.invalid_credentials`, "E-mail ou senha inválidos.", sem `Set-Cookie` | RN22 |
| [ ] | A4 | login com `{ "email": "ninguem@demo.local", "password": "Senha@Errada1" }` | a mesma resposta de A3, exceto `traceId` | RN22 |
| [ ] | A5 | login com `{ "email": "semarroba", "password": "" }` | `400 errors.email` e `errors.password` | TechSpec §7.2 |

Espere um minuto antes de continuar.

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | A6 | refresh no Scalar; cole o novo token | `200`, só com `accessToken`; novo `Set-Cookie` no Network | RN23 |
| [ ] | A7 | `curl` com `COOKIE_1` | `401` `auth.invalid_refresh_token`; `Set-Cookie: refresh_token=;`, com `expires` em 1970: a rotação invalidou o cookie anterior | RN23 |
| [ ] | A8 | `curl` sem cookie | `401` `auth.invalid_refresh_token` | RN23 |
| [ ] | A9 | login no Scalar; guardar o cookie como `COOKIE_2`; outro login no Scalar; `curl` com `COOKIE_2` | `401` `auth.invalid_refresh_token`: o novo login encerrou a sessão anterior | RN23 |
| [ ] | A10 | refresh no Scalar | `200`: a sessão do último login continua válida | RN23 |
| [ ] | A11 | login no Scalar; guardar o cookie como `COOKIE_3`; cole o novo token; logout no Scalar | `200`, "Sessão encerrada com sucesso."; `Set-Cookie` que apaga o cookie no Network | RN23 |
| [ ] | A12 | `curl` com `COOKIE_3` | `401` `auth.invalid_refresh_token`: sair encerrou a sessão | F01 |
| [ ] | A13 | logout no Scalar, com **Bearer Token** vazio | `401` `auth.unauthenticated` | TechSpec §7.2 |

Espere um minuto antes de continuar.

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | A14 | 5 logins seguidos com a senha errada de A3 | `401` nos cinco | RN24 |
| [ ] | A15 | logo em seguida, login com a senha certa | `429` `auth.too_many_attempts`, "Muitas tentativas de login. Aguarde um minuto e tente de novo." | RN24 |
| [ ] | A16 | esperar um minuto, fazer login, colar o token em **Bearer Token** e exportar `TOKEN` de novo | `200` | RN24 |

Comando de A7, A9 e A12, trocando `COOKIE_1` pelo cookie do passo; sem o header, é o comando de A8:

```bash
curl -i -X POST http://localhost:8080/api/v1/auth/refresh -H 'Cookie: refresh_token=COOKIE_1'
```

## 5. Vacinas (F02, F03)

Rotas no Scalar: **Vacinas > Cadastra uma vacina**, **Consulta uma vacina** e **Lista as vacinas**. Nos passos com vários valores, envie um por vez.

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | V1 | cadastrar `{ "name": "Vacina Teste", "primaryDoses": 2, "boosterDoses": 1 }`; anote `VACINA` | `201 { id }`, com `Location: .../api/v1/vaccines/{id}` | F02 |
| [ ] | V2 | consultar `VACINA` | `id`, `name`, `primaryDoses` e `boosterDoses`, com os valores de V1 | TechSpec §7.3 |
| [ ] | V3 | consultar `AUSENTE` | `404` `vaccine.not_found` | F03 |
| [ ] | V4 | cadastrar com o nome `  vacina TESTE  ` | `409` `vaccine.name_already_registered` | RN05 |
| [ ] | V5 | cadastrar com os nomes `A`, `  A  `, `   ` e `V101` e sem o campo `name` (1 + 0) | `400 errors.name` | RN05 |
| [ ] | V6 | cadastrar com os nomes `AB`, `V100` e `  Vacina Com Espacos  ` (1 + 0) | `201`; a última volta na consulta como `Vacina Com Espacos` | RN05 |
| [ ] | V7 | cadastrar com o nome `Fora Limite` e `primaryDoses` 0 e 11 | `400 errors.primaryDoses` | RN06 |
| [ ] | V8 | cadastrar com o nome `Fora Limite` e `boosterDoses` -1 e 6 | `400 errors.boosterDoses` | RN06 |
| [ ] | V9 | cadastrar `Limite Minimo` (1 + 0) e `Limite Maximo` (10 + 5) | `201` | RN06 |
| [ ] | V10 | listar, sem parâmetros | 14 vacinas numa página, por nome, começando em `AB`, `BCG` e `DTP` e terminando em `Vacina Com Espacos`, `Vacina Teste` e `V100`; `itemsPerPage` e `totalItems` 14; `totalPages` 1 | RN20 |
| [ ] | V11 | listar com `search` = `hepat` | só Hepatite B | RN20 |
| [ ] | V12 | listar com `search` = `TRIPLICE` | Tríplice viral: ignora maiúsculas e acentos | TechSpec §6.2 |
| [ ] | V13 | listar com `search` = `%`, `_` e `\` | texto comum: `data` vazio; `totalItems`, `totalPages` e `itemsPerPage` 0 | TechSpec §6.2, §7.1 |
| [ ] | V14 | listar com `page` 1 e `size` 2 | 2 itens; `itemsPerPage` 2, `totalItems` 14 e `totalPages` 7 | TechSpec §7.1 |
| [ ] | V15 | listar com `page` 999 e `size` 2 | `200`, com `data` vazio e `currentPage` 999 | TechSpec §7.1 |
| [ ] | V16 | listar com `size` 0 e 101 e com `page` 0 e `abc` | `400 errors.size` e `400 errors.page` | TechSpec §7.1 |
| [ ] | V17 | listar com `search` = `A101` | `400 errors.search` | TechSpec §7.1 |

## 6. Pessoas (F04, F05)

Rotas no Scalar: **Pessoas > Cadastra uma pessoa**, **Consulta uma pessoa** e **Lista as pessoas**.

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | P1 | cadastrar `{ "name": "Ana Teste", "cpf": "529.982.247-25" }`; anote `PESSOA` | `201 { id }`, com `Location: .../api/v1/people/{id}` | F04, RN02 |
| [ ] | P2 | consultar `PESSOA` | `id`, `name` e `cpf`; CPF `529.982.247-25` | RN26 |
| [ ] | P3 | cadastrar `{ "name": "Outra Pessoa", "cpf": "52998224725" }` | `409` `person.cpf_already_registered`, sem CPF nem nome no corpo | RN02, RN03, RN25 |
| [ ] | P4 | cadastrar `Outra Pessoa` com os CPFs `529.982.247-26`, `111.111.111-11` e `123` e sem o campo `cpf` | `400 errors.cpf` | RN02 |
| [ ] | P5 | cadastrar `{ "name": "João Teste", "cpf": "11144477735" }`; anote `JOAO`; consultar | `201`; a consulta mostra `111.444.777-35` | RN02, RN26 |
| [ ] | P6 | cadastrar com o CPF `987.654.321-00` e os nomes `Ab`, `  Ab  `, `   ` e `Z151` | `400 errors.name` | RN01 |
| [ ] | P7 | cadastrar `Bia` com o CPF `987.654.321-00` e `Z150` com o CPF `246.813.579-28` | `201` | RN01 |
| [ ] | P8 | consultar `AUSENTE` | `404` `person.not_found` | F05 |
| [ ] | P9 | listar, sem parâmetros | por nome: Ana Teste, Bia, João Teste, Maria Demonstração e `Z150`; `itemsPerPage` 20, `totalItems` 5 e `totalPages` 1; CPF no formato `***.ddd.ddd-**` em todos | RN19, RN26 |
| [ ] | P10 | listar com `search` = `joao` e `MARIA` | `João Teste` e `Maria Demonstração` | RN19 |
| [ ] | P11 | listar com `page` 2 e `size` 1 | só `Bia`; `currentPage` 2, `itemsPerPage` 1 e `totalPages` 5 | RN19 |
| [ ] | P12 | listar com `size` 100, `size` 101 e `page` 0 | `200`, `400 errors.size` e `400 errors.page` | RN19 |
| [ ] | P13 | listar com `search` = `52998224725` | `data` vazio: a busca não usa CPF | PRD §4.2 |

## 7. Registrar vacinação (F07)

Rota no Scalar: **Vacinações > Registra uma vacinação**, com `personId` = `PESSOA`. `VACINA` tem 2 doses primárias e 1 dose de reforço. Modelo do corpo:

```json
{ "vaccineId": "VACINA", "dose": { "type": "Primary", "number": 1 }, "appliedOn": "2024-01-10" }
```

Sem indicação, o passo usa `vaccineId` = `VACINA`. Siga a ordem: cada passo depende das doses já registradas.

| | ID | Dose | `appliedOn` | Esperado | Origem |
|---|---|---|---|---|---|
| [ ] | R1 | `Primary 3` | 2024-01-10 | `422` `vaccination.dose_outside_scheme` | RN09 |
| [ ] | R2 | `Booster 2`; e `Booster 1` com `vaccineId` = `BCG` | 2024-01-10 | `422` `vaccination.dose_outside_scheme` | RN09 |
| [ ] | R3 | `Primary 2` | 2024-01-10 | `422` `vaccination.dose_out_of_sequence` | RN10 |
| [ ] | R4 | `Booster 1` | 2024-01-10 | `422` `vaccination.dose_out_of_sequence`: reforço só depois da série primária | RN10 |
| [ ] | R5 | `Primary 1` | `AMANHÃ` | `422` `vaccination.applied_on_in_future` | RN12 |
| [ ] | R6 | `Primary 1` | 1899-12-31 | `422` `vaccination.applied_on_before_minimum` | RN12 |
| [ ] | R7 | `Primary 1` | sem o campo | `400 errors.appliedOn` | RN12 |
| [ ] | R8 | `"number": 0`; `"type": "Extra"`; `"type": 1`; `appliedOn` `10/01/2024`; sem `dose`; sem `vaccineId` | 2024-01-10, exceto no caso do formato | `400 errors.dose.number`, `errors.dose.type`, `errors.dose.type`, `errors.appliedOn`, `errors.dose` e `errors.vaccineId` | RN08, TechSpec §7.1 |
| [ ] | R9 | `Primary 1`, com `vaccineId` = `AUSENTE`; depois, com `personId` = `AUSENTE` | 2024-01-10 | `404` `vaccine.not_found` e `404` `person.not_found` | F07 |
| [ ] | R10 | `Primary 1`; anote `DOSE1` | 2024-01-10 | `201 { id }`, com `Location: .../vaccinations/{id}` | F07 |
| [ ] | R11 | `Primary 1` | 2024-01-20 | `409` `vaccination.dose_already_registered` | RN11 |
| [ ] | R12 | `Primary 2` | 2024-01-10 e 2024-01-09 | `422` `vaccination.applied_on_not_after_previous` | RN13 |
| [ ] | R13 | ordem das regras: `Primary 3`, `Primary 1` e `Booster 1`, todos em `AMANHÃ`; `Primary 2` em 1899-12-31 | ver a ação | `dose_outside_scheme`, `dose_already_registered`, `dose_out_of_sequence` e `applied_on_before_minimum`, nessa ordem | PRD §6 |
| [ ] | R14 | `Primary 2`; anote `DOSE2` | 2024-03-10 | `201` | RN10 |
| [ ] | R15 | `Booster 1`; anote `REFORCO1` | 2024-06-10 | `201` | RN10 |
| [ ] | R16 | `Primary 1`, com `vaccineId` = `BCG` | `HOJE` | `201`: hoje é aceito | RN12 |
| [ ] | R17 | `Primary 1`, com `vaccineId` = `FEBRE` | 1900-01-01 | `201`: o limite mínimo é aceito | RN12 |

Rota no Scalar: **Vacinações > Consulta uma vacinação**.

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | R18 | consultar `DOSE1` de `PESSOA` | `id`, `vaccineId`, `dose` e `appliedOn`, com os valores de R10 | TechSpec §7.3 |
| [ ] | R19 | consultar `DOSE1` de `MARIA` | `404` `vaccination.not_found` | TechSpec §5.2 |
| [ ] | R20 | consultar `AUSENTE` de `PESSOA` | `404` `vaccination.not_found` | TechSpec §7.2 |
| [ ] | R21 | consultar `DOSE1` de `AUSENTE` | `404` `person.not_found` | TechSpec §5.2 |

## 8. Cartão de vacinação (F08)

Rota no Scalar: **Vacinações > Consulta o cartão de vacinação**.

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | C1 | cartão de `PESSOA` | `person` com CPF `529.982.247-25`; vacinas por nome: BCG, Febre amarela e Vacina Teste | RN17, RN26 |
| [ ] | C2 | entrada de Vacina Teste | `scheme` 2 + 1; doses `Primary 1`, `Primary 2` e `Booster 1`, com as datas de R10, R14 e R15; `primaryApplied` 2, `primaryTotal` 2, `primarySeriesComplete` `true`, `boostersApplied` 1 e `boostersTotal` 1; `nextExpectedDose` `null` | RN17 |
| [ ] | C3 | cartão de `JOAO` | `vaccines` vazio, com CPF `111.444.777-35` | RN17, RN26 |
| [ ] | C4 | cartão de `AUSENTE` | `404` `person.not_found` | TechSpec §7.2 |
| [ ] | C5 | campos de cada nível de C1 | os da TechSpec §7.3, na mesma ordem | TechSpec §7.3 |

## 9. Correção da data de aplicação (F09)

Rota no Scalar: **Vacinações > Corrige a data de aplicação de uma vacinação**, com `personId` = `PESSOA` e o corpo `{ "appliedOn": "..." }`. As datas atuais são `DOSE1` 2024-01-10, `DOSE2` 2024-03-10 e `REFORCO1` 2024-06-10.

| | ID | Vacinação | `appliedOn` | Esperado | Origem |
|---|---|---|---|---|---|
| [ ] | K1 | `DOSE2` | 2024-02-01 | `200`, "Data de aplicação corrigida com sucesso."; a consulta mostra a nova data | RN14 |
| [ ] | K2 | `DOSE2` | 2024-01-10 | `422` `vaccination.applied_on_out_of_order`: igual à dose anterior | RN14 |
| [ ] | K3 | `DOSE2` | 2024-06-10 | `422` `vaccination.applied_on_out_of_order`: igual à dose seguinte | RN14 |
| [ ] | K4 | `DOSE1` | 2024-02-01 | `422` `vaccination.applied_on_out_of_order` | RN14 |
| [ ] | K5 | `DOSE1` | 2023-01-01 | `200`: a primeira dose só tem o limite de RN12 antes dela | RN14 |
| [ ] | K6 | `REFORCO1` | `HOJE` | `200`: a última dose só tem o limite de RN12 depois dela | RN14 |
| [ ] | K7 | `REFORCO1` | `AMANHÃ` | `422` `vaccination.applied_on_in_future` | RN12 |
| [ ] | K8 | `DOSE1` | 1899-12-31 | `422` `vaccination.applied_on_before_minimum` | RN12 |
| [ ] | K9 | `DOSE1` | corpo `{}` | `400 errors.appliedOn` | RN12 |
| [ ] | K10 | `DOSE1` | `{ "appliedOn": "2023-02-01", "dose": { "type": "Booster", "number": 1 } }` | `200`; a consulta mostra a nova data e a dose `Primary 1` | RN14 |
| [ ] | K11 | `AUSENTE` | 2024-01-01 | `404` `vaccination.not_found` | TechSpec §7.2 |
| [ ] | K12 | `DOSE1`, com `personId` = `AUSENTE` | 2024-01-01 | `404` `person.not_found` | TechSpec §5.2 |

## 10. Exclusão de vacinação (F10)

Rota no Scalar: **Vacinações > Exclui uma vacinação**, com `personId` = `PESSOA`.

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | E1 | excluir `DOSE1` e `DOSE2` | `422` `vaccination.not_last_dose` nas duas | RN15 |
| [ ] | E2 | excluir `REFORCO1` | `200`, "Vacinação excluída com sucesso." | RN15 |
| [ ] | E3 | consultar `REFORCO1` e o cartão | `404` `vaccination.not_found`; Vacina Teste com `nextExpectedDose` `Booster 1` | F10 |
| [ ] | E4 | excluir `DOSE2` e depois `DOSE1` | `200` nas duas | RN15 |
| [ ] | E5 | consultar o cartão | só BCG e Febre amarela: Vacina Teste saiu do cartão | F10 |
| [ ] | E6 | excluir `AUSENTE` | `404` `vaccination.not_found` | TechSpec §7.2 |
| [ ] | E7 | excluir `DOSE1`, com `personId` = `AUSENTE` | `404` `person.not_found` | TechSpec §5.2 |

## 11. Remoção de pessoa (F06)

Rota no Scalar: **Pessoas > Remove uma pessoa**.

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | M1 | remover `PESSOA`, que ainda tem as vacinações de R16 e R17 | `200`, "Pessoa removida com sucesso." | RN04 |
| [ ] | M2 | consultar a pessoa e o cartão | `404` `person.not_found` nos dois | RN04 |
| [ ] | M3 | remover `PESSOA` de novo | `404` `person.not_found` | TechSpec §5.2 |
| [ ] | M4 | cadastrar de novo `{ "name": "Ana Teste", "cpf": "529.982.247-25" }` e consultar o cartão | `201` e `vaccines` vazio: não há retenção | RN04 |

## 12. Privacidade e documentação

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | L1 | rodar o primeiro `grep` abaixo | `0` | RN25 |
| [ ] | L2 | rodar o segundo e o terceiro `grep` abaixo | `0`, e um número maior que zero: o log existe e não traz query string | RN25 |
| [ ] | L3 | conferir as rotas no Scalar | nenhuma recebe CPF no caminho ou na query | RN25 |
| [ ] | L4 | abrir `http://localhost:8080/openapi/v1.json` numa aba | `200`, sem token, com o esquema `Bearer` | RNF07 |
| [ ] | L5 | conferir no Scalar cada rota contra a TechSpec §7.2 | resumo e tag em português e os mesmos status, mais `401` nas rotas com access token; só login e refresh sem **Auth Required** | TechSpec §7.1 |

```bash
grep -c -E '52998224725|529\.982\.247|11144477735|111\.444\.777|12345678909|123\.456\.789|Ana Teste|João Teste|Maria Demonstra|Demo@2026|Senha@Errada1|eyJhbGci' /tmp/api-manual.log
grep -c -E 'search=|size=|page=' /tmp/api-manual.log
grep -c '/api/v1/people' /tmp/api-manual.log
```

## 13. Validade do access token (opcional)

Suba a API com um access token de 1 minuto:

```bash
Jwt__AccessTokenMinutes=1 dotnet run --project backend/src/VaccinationCard.Api
```

| | ID | Ação | Esperado | Origem |
|---|---|---|---|---|
| [ ] | O1 | login no Scalar e **Lista as vacinas** com o token | `200` | RN23 |
| [ ] | O2 | esperar 2 minutos (1 minuto de validade + 30 s de `ClockSkew`) e repetir a lista | `401` `auth.unauthenticated` | TechSpec §9 |
| [ ] | O3 | refresh no Scalar, colar o novo token e repetir a lista | `200` nos dois: a sessão se renova sem novo login | RN23 |

## 14. Fora deste roteiro

- **RN16 (concorrência):** depende de duas gravações entre a leitura e a escrita de uma requisição, o que não se reproduz à mão. Rode `dotnet test backend/VaccinationCard.slnx --filter "RN=RN16"`.
- **RN18 (rótulos de dose):** existe só na interface (T28).
- **Refresh token vencido, depois de 7 dias (RN23):** o teste automatizado avança o relógio. Rode `dotnet test backend/VaccinationCard.slnx --filter "RN=RN23"`.
- **`500` `internal.unexpected`:** nenhuma rota provoca esse erro à mão; os testes do pipeline cobrem o caso.
- **RNF05 (`docker compose up` completo):** este roteiro roda a API fora do Compose; a verificação da T30 ([TASKS](TASKS.md)) cobre a subida completa.

## 15. Cobertura

| Item | Passos |
|---|---|
| F01 | A1–A16, O1–O3 |
| F02 | V1, V4–V9 |
| F03 | V2, V3, V10–V17 |
| F04 | P1, P3–P7 |
| F05 | P2, P8–P13 |
| F06 | M1–M4 |
| F07 | R1–R21 |
| F08 | D4, C1–C5 |
| F09 | K1–K12 |
| F10 | E1–E7 |
| RN01 | P6, P7 |
| RN02 | P1, P3–P5 |
| RN03 | P3 |
| RN04 | M1–M4 |
| RN05 | V4–V6 |
| RN06 | V7–V9 |
| RN07 | S7 |
| RN08 | R8 |
| RN09 | R1, R2, R13 |
| RN10 | R3, R4, R13–R15 |
| RN11 | R11, R13 |
| RN12 | R5–R7, R13, R16, R17, K6–K9 |
| RN13 | R12 |
| RN14 | K1–K6, K10 |
| RN15 | E1–E4 |
| RN16 | seção 14 |
| RN17 | D4, C1–C3 |
| RN18 | seção 14 |
| RN19 | P9–P13 |
| RN20 | V10–V13 |
| RN21 | S3–S5, A13 |
| RN22 | A1, A3–A5 |
| RN23 | A6–A12, O1–O3, seção 14 |
| RN24 | A14–A16 |
| RN25 | P3, L1–L3 |
| RN26 | D2, D3, P2, P5, P9, C1, C3 |
| RNF01 | S3–S12 |
| RNF02 | A2, A15, S3, S13 |
| RNF03 | M1–M4, L1–L3 |
| RNF05 | seção 14 |
| RNF07 | L4, L5 |
| RNF08 | S1, S2 |
| PRD §9 | D1–D5 |
