# Tasks — Cartão de Vacinação

| | |
|---|---|
| Versão | 1.0 |
| Data | 28/10/2026 |
| Status | Em revisão |
| Base | [PRD](PRD.md), [TechSpec](TECHSPEC.md) e [ADRs](adr/) |
| Prazo | fim do dia 03/11/2026 |

Este plano divide a construção do sistema em tasks pequenas e verificáveis, distribuídas até o prazo. O PRD define o que construir, e a TechSpec, como construir.

## 1. Como usar este plano

- Cada task é uma fatia vertical: uma funcionalidade inteira (domínio, caso de uso, persistência, rota e testes) ou uma peça de base que as fatias seguintes usam.
- A ordem numérica respeita as dependências. Uma task só usa o que as anteriores entregaram.
- A IA executa uma task por vez. No fim, para e entrega o resultado conforme o protocolo de `.agents/context.md` (T04). O autor revisa, faz o commit e libera a task seguinte.
- Uma lacuna ou contradição na especificação interrompe a task e vira pergunta ao autor. A resposta entra na TechSpec ou num ADR antes do código.
- Ao concluir uma task, a IA marca o item na seção 4, e o commit da task leva a marcação.

## 2. Definição de pronto

Uma task de código está pronta quando:

- `dotnet build` passa sem warnings, e `npm run build` também passa quando a task muda o frontend;
- `dotnet test` (unit, integração e arquitetura) e `npm test` passam;
- as regras de domínio nascem de testes escritos antes do código (TDD);
- cada caso de uso tem teste unitário e teste de integração;
- cada teste de regra leva `[Trait("RN", "RNxx")]` no backend, ou o código da regra no nome do teste no frontend;
- cada rota nova tem as anotações OpenAPI e um exemplo em `backend/VaccinationCard.http`;
- a TechSpec, os ADRs, o glossário e as skills refletem qualquer decisão, convenção ou termo novo da task.

## 3. Cronograma

| Dia | Foco | Tasks |
|---|---|---|
| 28/10 (qua) | definição | T01–T03 |
| 29/10 (qui) | agente, base do backend e vacinas | T04–T10 |
| 30/10 (sex) | pessoas, registro de vacinação e cartão | T11–T15 |
| 31/10 (sáb) | correção, exclusão, concorrência, autenticação e dados de demonstração | T16–T22 |
| 01/11 (dom) | frontend: base, sessão, vacinas e pessoas | T23–T26 |
| 02/11 (seg, Finados) | frontend: cartão e testes; Compose completo | T27–T31 |
| 03/11 (ter) | README, revisão final e entrega | T32–T33 |

As tasks marcadas como _cortável_ saem do plano se o prazo apertar, na ordem da seção 5.

## 4. Tasks

### 28/10 (qua) — definição

- [x] **T01 — PRD e glossário**
  - **Entrega:** sessão de perguntas e respostas para fechar as ambiguidades do enunciado; `docs/PRD.md` e `GLOSSARY.md`.
- [x] **T02 — TechSpec e ADRs**
  - **Entrega:** `docs/TECHSPEC.md` e `docs/adr/`.
- [x] **T03 — Plano de tasks**
  - **Entrega:** este arquivo.

### 29/10 (qui) — agente, base do backend e vacinas

- [x] **T04 — Contexto e regras do agente**
  - **Entrega:** `.agents/context.md`, com o projeto, a stack, os comandos de build, teste e execução, o mapa da documentação e o protocolo de entrega por task; `.agents/rules/architecture.md`, com os módulos, as camadas, as regras A1–A7 e as convenções de nomes; `CLAUDE.md` e `AGENTS.md` como symlinks para `.agents/context.md`.
  - **Verificação:** os symlinks resolvem; o contexto aponta para PRD, TechSpec, glossário, ADRs e este plano, sem copiar o conteúdo deles.
  - **Referência:** TechSpec §3 e §4.
- [ ] **T05 — Skills do agente**
  - **Entrega:** 11 skills em português em `.agents/skills/`, com `references/` para o detalhe: `architecture`, `domain-modeling`, `use-case`, `persistence`, `api-endpoint`, `error-handling`, `authentication`, `testing`, `angular-feature`, `new-feature` e `code-review`; `.claude/skills` como symlink para `.agents/skills`; ponteiro para as skills em `.agents/context.md`.
  - **Verificação:** cada skill tem `name` e uma `description` que diz quando usá-la; as 11 skills aparecem no Claude Code.
  - **Referência:** TechSpec §4 a §13.
- [ ] **T06 — Base do backend e CI**
  - **Entrega:** `backend/` com `VaccinationCard.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `global.json` e `.editorconfig`; `VaccinationCard.Api` com logs em JSON, OpenAPI, Scalar, `AppDbContext` (Npgsql e snake_case), migrations na inicialização e `/health`; os 3 projetos de teste, com PostgreSQL via Testcontainers, Respawn e `WebApplicationFactory`; testes de arquitetura A1–A7; `docker-compose.yml` com o serviço `db`; `.env.example` e `.gitignore`; job `backend` em `.github/workflows/ci.yml`.
  - **Verificação:** `dotnet build` sem warnings; `dotnet test` passa, com o teste de integração de `/health`; `docker compose up db` sobe o banco de dados saudável.
  - **Referência:** TechSpec §2, §3, §4.1, §4.2, §7.1, §10, §12, §13 e §14.
- [ ] **T07 — Erros, validação e pipeline HTTP**
  - **Entrega:** as 4 exceções de `Shared/Errors`; `IExceptionHandler` com ProblemDetails, `code` e `traceId`, inclusive o `500` `internal.unexpected` sem detalhes; `ValidationBehavior`, com `400` e `errors` em camelCase; headers de segurança; log de requisição sem query string, com o log de hosting em `Warning`.
  - **Regras:** RN25.
  - **Verificação:** unit do mapeamento de cada exceção e do `ValidationBehavior`; integração: headers de segurança em toda resposta e log de requisição sem query string.
  - **Referência:** TechSpec §4.4, §8, §9 e §10.
- [ ] **T08 — Vacinas: cadastro e consulta**
  - **Entrega:** casos de uso `CreateVaccine` e `FindVaccineById`; `DoseScheme`, com as invariantes de RN06; `VaccinesErrors`; `IVaccineDao` e `VaccineDao`; `VaccineRecord`, configuração e a migration com a extensão `unaccent` e a tabela `vaccines`; violação de índice único (`23505`) convertida em `ConflictException`; `VaccinesController`, com `POST /api/v1/vaccines` e `GET /api/v1/vaccines/{id}`; `AddVaccinesModule`.
  - **Regras:** RN05, RN06 e RN07.
  - **Verificação:** unit do `DoseScheme`, do validator e dos handlers; integração dos handlers e do DAO, inclusive a conversão do `23505`; HTTP: `201` + `Location`, `400` com `errors`, `409` `vaccine.name_already_registered` para um nome que difere só nas maiúsculas, `404` `vaccine.not_found` e `405` em `PUT` e `PATCH` (RN07).
  - **Referência:** TechSpec §5.1, §6 e §7.
- [ ] **T09 — Vacinas: listagem**
  - **Entrega:** caso de uso `FindAllVaccines`; envelope `Paginated<T>`; validação comum de `page`, `size` e `search`; busca com `unaccent` e `ILIKE`, com `%`, `_` e `\` escapados; sem `page` e `size`, todas as vacinas numa página só; `GET /api/v1/vaccines`.
  - **Regras:** RN20.
  - **Verificação:** unit do validator; integração do DAO: ordem por nome, busca por parte do nome e sem acento, `%` e `_` tratados como texto; HTTP: envelope e `400` com `size` fora do limite.
  - **Referência:** TechSpec §6.2 e §7.1.
- [ ] **T10 — Ajuste das skills**
  - **Entrega:** skills e regras do agente revisadas com o que T06–T09 mostraram; exemplos em `references/` apontando para o código de vacinas.
  - **Verificação:** seguir as skills `use-case`, `persistence`, `api-endpoint`, `error-handling` e `testing` reproduz o código de vacinas sem correção manual.

### 30/10 (sex) — pessoas, registro de vacinação e cartão

- [ ] **T11 — Pessoas: cadastro e consulta**
  - **Entrega:** value object `Cpf`, com validação, formato e máscara; `Person` e `PersonFactory` (`Create` e `Load`); `PeopleErrors`; casos de uso `CreatePerson` e `FindPersonById`; `IPersonRepository`, `IPersonDao` e as implementações; `PersonRecord`, configuração e a migration da tabela `people`, ainda sem a coluna `version` (T18); `PeopleController`, com `POST /api/v1/people` e `GET /api/v1/people/{id}`; `AddPeopleModule`.
  - **Regras:** RN01, RN02, RN03, RN25 e RN26.
  - **Verificação:** unit do `Cpf` (dígitos verificadores, dígito repetido, com e sem máscara), da factory, do validator e dos handlers; integração do Repository, do DAO e dos handlers; HTTP: `201` + `Location`, `400`, `409` `person.cpf_already_registered` sem o CPF na mensagem, `404` `person.not_found` e CPF formatado na consulta.
  - **Referência:** TechSpec §5.2 e §6.
- [ ] **T12 — Pessoas: listagem e remoção**
  - **Entrega:** casos de uso `FindAllPeople` (busca, 20 pessoas por página por padrão e no máximo 100, CPF mascarado) e `DeletePerson`; `GET /api/v1/people` e `DELETE /api/v1/people/{id}`.
  - **Regras:** RN04, RN19 e RN26.
  - **Verificação:** integração do DAO: busca sem diferenciar maiúsculas e acentos, ordem por nome e paginação; HTTP: envelope com CPF mascarado, `200 { message }` na remoção e `404` na consulta seguinte.
  - **Referência:** TechSpec §5.2 e §7.
- [ ] **T13 — Vacinação: regras do agregado**
  - **Entrega:** só domínio, em TDD: `Dose`, `DoseType` e as operações de sequência do `DoseScheme`; `Vaccination`; `Person.RegisterVaccination` e `Person.NextExpectedDose`, com a ordem de checagem RN09, RN11, RN10, RN12, RN13; códigos e mensagens em `PeopleErrors`.
  - **Regras:** RN08, RN09, RN10, RN11, RN12 e RN13.
  - **Verificação:** unit de cada regra, da ordem de checagem quando mais de uma regra é violada, do reforço só depois da série primária completa e da próxima dose esperada no meio e no fim do esquema.
  - **Referência:** TechSpec §5.1 e §5.2; [ADR-0008](adr/0008-esquema-de-doses-e-sequencia-estrita.md).
- [ ] **T14 — Vacinação: registro ponta a ponta**
  - **Entrega:** `IClock`, com "hoje" no fuso de `App:TimeZone`; casos de uso `RegisterVaccination` e `FindVaccinationById`; vacinações no `PersonRepository.FindByIdAsync` e o `EditAsync`; `VaccinationRecord`, configuração e a migration da tabela `vaccinations`; `23505` da dose convertido em `vaccination.dose_already_registered`; `VaccinationsController`, com `POST /api/v1/people/{id}/vaccinations` e `GET /api/v1/people/{id}/vaccinations/{vaccinationId}`.
  - **Regras:** RN04, RN08, RN11 e RN12.
  - **Verificação:** unit do `IClock` com `FakeTimeProvider` (às 23h30 em Brasília, quando o UTC já está no dia seguinte, "hoje" continua sendo a data de Brasília), do validator e do handler; integração do Repository e do handler; HTTP: `201` + `Location`, `404` de pessoa e de vacina, `409` e `422` com o código de cada regra; remover a pessoa apaga as vacinações (RN04).
  - **Referência:** TechSpec §4.3, §5.2 e §6.
- [ ] **T15 — Cartão de vacinação**
  - **Entrega:** `FindVaccinesByIdsQuery` em Vaccines; `IPersonDao.FindCardDataAsync`; caso de uso `FindVaccinationCard`, com as vacinas ordenadas por nome, as doses na ordem da sequência, o progresso e a próxima dose esperada; `GET /api/v1/people/{id}/vaccination-card`.
  - **Regras:** RN17 e RN26.
  - **Verificação:** unit da montagem do cartão; integração do handler; HTTP: contrato da TechSpec §7.3, CPF formatado, cartão sem vacinas para uma pessoa sem vacinação e `404` de pessoa inexistente.
  - **Referência:** TechSpec §4.3, §5.2 e §7.3; [ADR-0011](adr/0011-comunicacao-entre-modulos-por-queries.md).

### 31/10 (sáb) — correção, exclusão, concorrência, autenticação e dados de demonstração

- [ ] **T16 — Correção da data de aplicação**
  - **Entrega:** `Person.CorrectVaccinationDate`, em TDD; caso de uso `CorrectVaccinationDate`; data atualizada pelo `EditAsync`; `PATCH /api/v1/people/{id}/vaccinations/{vaccinationId}`.
  - **Regras:** RN12 e RN14.
  - **Verificação:** unit da nova data entre as doses vizinhas, na primeira e na última dose e igual à data de uma vizinha; integração do handler; HTTP: `200 { message }`, `404` `vaccination.not_found` e `422` `vaccination.applied_on_out_of_order`.
  - **Referência:** TechSpec §5.2.
- [ ] **T17 — Exclusão da última dose**
  - **Entrega:** `Person.RemoveVaccination`, em TDD; caso de uso `DeleteVaccination`; vacinação removida pelo `EditAsync`; `DELETE /api/v1/people/{id}/vaccinations/{vaccinationId}`.
  - **Regras:** RN15.
  - **Verificação:** unit; integração do handler; HTTP: `200 { message }`, `404` e `422` `vaccination.not_last_dose`; a vacina sai do cartão quando a única vacinação dela é excluída.
  - **Referência:** TechSpec §5.2.
- [ ] **T18 — Concorrência otimista** · _cortável_
  - **Entrega:** migration da coluna `people.version`; checagem de versão no `EditAsync`; `ConflictException` `vaccination_card.concurrent_update`.
  - **Regras:** RN16.
  - **Verificação:** integração: duas alterações sobre a mesma versão do cartão, e só a primeira grava; HTTP: `409` com o código.
  - **Referência:** TechSpec §6.3.
- [ ] **T19 — Contas, login e rotas protegidas**
  - **Entrega:** módulo Accounts, com a tabela `accounts`, `IAccountDao`, `IPasswordHasher`, `VerifyAccountCredentialsQuery` e `FindAccountByIdQuery`; módulo Authentication, com o caso de uso `SignIn`, `ITokenService`, `ISessionDao`, a tabela `sessions` e `RefreshTokenSession`; `POST /api/v1/auth/login`, com o cookie do refresh token; JwtBearer, fallback policy, `OnChallenge` em ProblemDetails e esquema Bearer no documento OpenAPI. Os testes HTTP anteriores passam a usar um cliente autenticado, com o token emitido pelo `ITokenService`.
  - **Regras:** RN21, RN22 e RN23.
  - **Verificação:** unit dos handlers e do `RefreshTokenSession`; integração: o login grava o hash do refresh token e substitui a sessão anterior da conta; HTTP: `200 { accessToken }` + cookie com os atributos da TechSpec §9, `401` com a mesma mensagem para e-mail inexistente e senha errada, e `401` `auth.unauthenticated` sem token; um teste percorre as rotas e confere que só login, refresh, `/health`, OpenAPI e Scalar são anônimos.
  - **Referência:** TechSpec §5.3, §5.4, §7 e §9; [ADR-0007](adr/0007-refresh-token-em-cookie-httponly.md).
- [ ] **T20 — Renovação da sessão e logout**
  - **Entrega:** casos de uso `RefreshSession`, com rotação, e `SignOut`; `POST /api/v1/auth/refresh` e `POST /api/v1/auth/logout`; cookie reescrito na renovação e apagado no logout e na renovação recusada.
  - **Regras:** RN23.
  - **Verificação:** unit dos handlers; HTTP: a renovação devolve um par novo; depois da rotação, o refresh token anterior é recusado; um refresh token vencido é recusado; depois do logout, a renovação responde `401` `auth.invalid_refresh_token`.
  - **Referência:** TechSpec §5.4 e §9.
- [ ] **T21 — Limite de tentativas de login** · _cortável_
  - **Entrega:** política `login` do rate limiter, com janela fixa de 1 minuto e 5 requisições por IP; `429` em ProblemDetails, com `auth.too_many_attempts`; forwarded headers aceitos só de `ForwardedHeaders:KnownProxies`.
  - **Regras:** RN24.
  - **Verificação:** HTTP: a 6ª tentativa no mesmo minuto responde `429`; um `X-Forwarded-For` vindo de fora da lista de proxies é ignorado.
  - **Referência:** TechSpec §9.
- [ ] **T22 — Dados de demonstração**
  - **Entrega:** seeds em `Infra/Database/Seeds`, ligados por `Database:SeedOnStartup`: a conta de demonstração, as 8 vacinas do PRD §9 e a pessoa de demonstração com cartão parcial, registrado pelo agregado.
  - **Verificação:** integração: o seed roda duas vezes sem duplicar dados, e o cartão da pessoa de demonstração respeita as regras; login com a conta de demonstração.
  - **Referência:** PRD §9; TechSpec §6.4.

### 01/11 (dom) — frontend: base, sessão, vacinas e pessoas

- [ ] **T23 — Base do frontend**
  - **Entrega:** projeto Angular 21 em `frontend/` (standalone, zoneless e Vitest); Angular Material com tema M3; `LOCALE_ID` `pt-BR` e adaptador date-fns no locale `ptBR`; `proxy.conf.json` para `http://localhost:8080`; modelos `Paginated<T>` e `ProblemDetails`; conversão de ProblemDetails em erro de campo ou snackbar; shell com barra superior e navegação; job `frontend` no CI.
  - **Verificação:** `npm run build` e `npm test` passam; unit da conversão de ProblemDetails; `npm start` abre o shell.
  - **Referência:** TechSpec §11 e §14.
- [ ] **T24 — Login e sessão**
  - **Entrega:** `AuthService`, com o access token num signal; tela `/login`; refresh na inicialização (`provideAppInitializer`); `authInterceptor`, com o header `Authorization`, um único refresh compartilhado e a repetição das requisições; `authGuard`; "Sair" na barra superior.
  - **Regras:** RN21 e RN23.
  - **Verificação:** unit do `authInterceptor`: várias requisições com `401` disparam um só refresh e são repetidas, uma falha no refresh leva ao `/login`, e um `401` do login ou do refresh não dispara outro refresh; teste manual: login com a conta de demonstração, recarregar a página mantém a sessão, e "Sair" volta ao login.
  - **Referência:** TechSpec §11.2.
- [ ] **T25 — Vacinas no frontend**
  - **Entrega:** `/vaccines`, com lista, busca e cadastro com o esquema de doses; `VaccinesService`; skill `angular-feature` revisada com o que a primeira feature mostrou.
  - **Verificação:** unit do `VaccinesService` com `HttpTestingController`; teste manual: cadastro, erro de campo e nome repetido.
  - **Referência:** TechSpec §11.
- [ ] **T26 — Pessoas no frontend**
  - **Entrega:** `/people`, com lista, busca, paginação e CPF mascarado; cadastro com máscara de CPF; remoção com `ConfirmDialog`; `PeopleService`.
  - **Verificação:** unit do `PeopleService`; teste manual: busca sem acento, paginação, cadastro e remoção.
  - **Referência:** TechSpec §11.

### 02/11 (seg, Finados) — frontend: cartão e testes; Compose completo

- [ ] **T27 — Protótipo do cartão** · _cortável_
  - **Entrega:** protótipo descartável da grade do cartão, para validar o layout antes da T28. Fica fora do repositório.
  - **Verificação:** o autor aprova o layout.
- [ ] **T28 — Cartão no frontend**
  - **Entrega:** `/people/:id/card`, com os dados da pessoa e a grade; `DoseLabelPipe`; diálogo de registro com a próxima dose esperada já selecionada; correção de data em qualquer dose; exclusão da última dose, com confirmação; `VaccinationCardService`.
  - **Regras:** RN17 e RN18.
  - **Verificação:** unit do `DoseLabelPipe` ("Dose única", "1ª dose", "1º reforço") e do service; teste manual: registrar, corrigir e excluir doses, com as mensagens da API no snackbar.
  - **Referência:** TechSpec §7.3 e §11.3.
- [ ] **T29 — Teste de componente do cartão** · _cortável_
  - **Entrega:** teste do componente do cartão: uma linha por vacina, as colunas na ordem da sequência, as células vazias e as ações da última dose.
  - **Verificação:** `npm test` passa.
- [ ] **T30 — Compose completo**
  - **Entrega:** `backend/Dockerfile` (SDK 10 no build, `aspnet:10.0` no runtime, usuário sem root e `tzdata`); `frontend/Dockerfile` e `nginx.conf` (fallback para `index.html`, proxy de `/api/`, compressão dos estáticos e headers de segurança); serviços `api` e `web` no `docker-compose.yml`, com o IP fixo do `web` em `ForwardedHeaders__KnownProxies`; `.env.example` completo.
  - **Verificação:** depois de `docker compose down -v`, `docker compose up --build` sobe tudo; `/health` responde `200`; em `http://localhost:4200`, login com a conta de demonstração e cartão da pessoa de demonstração.
  - **Referência:** TechSpec §12.
- [ ] **T31 — E2E smoke** · _cortável_
  - **Entrega:** um teste Playwright contra o Compose: login, lista de pessoas, cartão da pessoa de demonstração, registro da próxima dose e a dose na grade.
  - **Verificação:** o teste passa localmente com o Compose no ar. Fica fora do CI.

### 03/11 (ter) — README, revisão final e entrega

- [ ] **T32 — README**
  - **Entrega:** `README.md` em português: visão geral, como rodar (Compose e local), conta de demonstração, rotas e exemplos com `curl`, testes (inclusive o filtro por regra), decisões com links para os ADRs, limitações conhecidas, o cookie `Secure` em `http://localhost` e a seção "Uso de IA" (ferramenta, fluxo guiado por especificação, decisões do autor e revisão de cada task), com links para PRD, TechSpec, ADRs e este plano.
  - **Verificação:** seguindo só o README, uma máquina limpa com Docker chega ao cartão da pessoa de demonstração.
  - **Referência:** PRD RNF07; TechSpec §12 e §16.
- [ ] **T33 — Revisão final e entrega**
  - **Entrega:** revisão do repositório com a skill `code-review` e correção dos achados; cobertura de cada regra conferida (`dotnet test --filter "RN=RNxx"` e os testes do frontend); status final de PRD, TechSpec e deste plano.
  - **Verificação:** os critérios de entrega do PRD §11, com o CI verde no GitHub.

## 5. Linha de corte

Se o prazo apertar, as tasks cortáveis saem nesta ordem:

1. T31 — E2E smoke.
2. T27 — protótipo do cartão.
3. T29 — teste de componente do cartão.
4. T18 — concorrência otimista.
5. T21 — limite de tentativas de login.

Não se cortam as regras de dose e os testes delas (T13, T14, T16 e T17), a autenticação com refresh token (T19, T20 e T24), o Compose (T30) e o README (T32).

Uma task cortada fica riscada na seção 4. Cortar T18 ou T21 deixa uma regra sem implementação (RN16 ou RN24). Nesse caso, a regra entra nas limitações conhecidas do README, e a TechSpec registra o corte.

## 6. Rastreabilidade

### 6.1 Funcionalidades

| Funcionalidade | API | Interface |
|---|---|---|
| F01 — Autenticação | T19, T20, T21 | T24 |
| F02 — Cadastrar vacina | T08 | T25 |
| F03 — Listar e consultar vacinas | T08, T09 | T25 |
| F04 — Cadastrar pessoa | T11 | T26 |
| F05 — Listar e consultar pessoas | T11, T12 | T26 |
| F06 — Remover pessoa | T12 | T26 |
| F07 — Registrar vacinação | T13, T14 | T28 |
| F08 — Consultar cartão de vacinação | T15 | T28 |
| F09 — Corrigir data de aplicação | T16 | T28 |
| F10 — Excluir vacinação | T17 | T28 |

### 6.2 Regras de negócio

Tasks que implementam cada regra. Cada regra tem pelo menos um teste automatizado nessas tasks.

| Regra | Tasks |
|---|---|
| RN01 | T11 |
| RN02 | T11 |
| RN03 | T11 |
| RN04 | T12, T14 |
| RN05 | T08 |
| RN06 | T08 |
| RN07 | T08 |
| RN08 | T13, T14 |
| RN09 | T13 |
| RN10 | T13 |
| RN11 | T13, T14 |
| RN12 | T13, T14, T16 |
| RN13 | T13 |
| RN14 | T16 |
| RN15 | T17 |
| RN16 | T18 |
| RN17 | T15, T28 |
| RN18 | T28 |
| RN19 | T12 |
| RN20 | T09 |
| RN21 | T19, T24 |
| RN22 | T19 |
| RN23 | T19, T20, T24 |
| RN24 | T21 |
| RN25 | T07, T11 |
| RN26 | T11, T12, T15 |
