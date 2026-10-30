# Regras de arquitetura

Resumo operacional da TechSpec §4 (backend) e §11 (frontend), para consultar ao criar, mover ou importar código. Os caminhos partem da raiz do repositório. Os testes de `VaccinationCard.ArchitectureTests` verificam as regras A1–A7; uma mudança de regra atualiza este arquivo, a TechSpec e os testes na mesma task.

## Backend

O backend é um único projeto, `backend/src/VaccinationCard.Api`. As camadas são separadas por namespace e garantidas pelos testes de arquitetura (`docs/adr/0001-projeto-unico-com-modulos.md`).

### Módulos

| Módulo | Estilo | Responsabilidade |
|---|---|---|
| `Vaccines` | CRUD simples | vacinas e os conceitos de dose (`Dose`, `DoseType`, `DoseScheme`) |
| `People` | DDD-light | agregado `Person` + `Vaccination`, registro de vacinação e cartão |
| `Accounts` | CRUD simples, sem rota HTTP | contas e conferência de senha |
| `Authentication` | casos de uso sem entidade | login, renovação da sessão e logout |

Todo módulo tem `Domain/`, `Application/` e `Infra/`, e registra as próprias dependências em `<Módulo>Module.cs` (`Add<Módulo>Module`), chamado pelo `Program.cs`. Entidade só existe onde há regra de negócio; um módulo CRUD lê e grava pelo DAO (`docs/adr/0002-ddd-light-so-onde-ha-regra.md`). As vacinações são filhas do agregado `Person`, e não um módulo próprio (`docs/adr/0012-vacinacoes-no-agregado-da-pessoa.md`).

### Onde cada coisa fica

Pastas relativas a `backend/src/VaccinationCard.Api/`; `<M>` é `Modules/<Módulo>`. O namespace segue a pasta.

| Artefato | Pasta |
|---|---|
| entidade e factory | `<M>/Domain/Entities`, `<M>/Domain/Factories` |
| value object e enum do módulo | `<M>/Domain/ValueObjects`, `<M>/Domain/Enums` |
| códigos e mensagens de erro | `<M>/Domain/Errors/<Módulo>Errors.cs` |
| caso de uso: command ou query, handler, validator e output | `<M>/Application/UseCases/<Verbo><Entidade>/` |
| DTO | `<M>/Application/Dtos` |
| port de leitura e port de escrita do agregado | `<M>/Application/Persistence/Dao`, `<M>/Application/Persistence/Repository` |
| port de segurança (hash de senha, tokens) | `<M>/Application/Security` |
| serviço usado por mais de um caso de uso | `<M>/Application/Services` |
| controller | `<M>/Infra/Drivers/Http` |
| corpo de rota com parâmetro (`*Request`) | `<M>/Infra/Drivers/Http/Requests` |
| registro de persistência, configuração do EF Core, DAO e Repository | `<M>/Infra/Driven/Persistence/EfCore` |
| implementação de port de segurança e as opções dela | `<M>/Infra/Driven/Security` |
| value object usado por mais de um módulo | `Shared/ValueObjects` |
| exceções de erro e envelope de paginação | `Shared/Errors`, `Shared/Pagination` |
| behavior do MediatR, relógio, validação comum e `SuccessMessage` | `Core/Behaviors`, `Core/Time`, `Core/Validation`, `Core/Messages` |
| registro do MediatR, do `ValidationBehavior`, dos validators e do relógio (`TimeProvider` e `IClock`) | `Core/CoreExtensions.cs` (`AddCore`) |
| `AppDbContext`, migrations, seeds e extensões de consulta (`ToPaginatedAsync`, `LikePattern`, `IsUniqueViolation`) | `Infra/Database` |
| middleware, handlers HTTP globais, formato dos erros (`ApiProblemDetails`), limite de tentativas de login (`LoginRateLimit`) e forwarded headers | `Infra/Http` |
| documento OpenAPI e Scalar | `Infra/OpenApi` |

### Dependências

| # | Regra |
|---|---|
| A1 | `Modules.*.Domain` usa só `Shared`, a BCL e o que A4 permite. |
| A2 | `Modules.*.Application` acessa os dados do módulo só pelos ports de `Application.Persistence`. Não depende de `Infra` (do módulo ou global), EF Core, Npgsql nem ASP.NET Core. |
| A3 | Um módulo não depende da `Infra` nem da `Application.Persistence` de outro módulo. |
| A4 | Para usar outro módulo, a `Application` envia uma query dele (`ISender.Send`) e recebe DTOs; cada módulo grava só os próprios dados (`docs/adr/0011-comunicacao-entre-modulos-por-queries.md`). Exceção: o `Domain` e a `Application` de `People` usam também os value objects e enums de `Vaccines.Domain`. |
| A5 | O controller (`Infra.Drivers.Http`) monta o command ou a query e chama `ISender.Send`. Não depende de `Application.Persistence` nem de EF Core. |
| A6 | `Shared` não depende de `Core`, `Infra` nem `Modules`. `Core` não depende de `Infra`, `Modules`, EF Core nem ASP.NET Core. |
| A7 | Sufixos obrigatórios: `*Command`, `*Query`, `*Handler`, `*Validator`, `*Output`, `*Dto`, `*Dao`, `*Repository`, `*Controller`, `*Record` e `*Configuration`. |

### Convenções de código

- Um tipo público por arquivo, com namespace por arquivo. O build recusa namespace com bloco (IDE0161) ou diferente da pasta (IDE0130).
- Classes `sealed` por padrão. Command, query, DTO e output são `record`.
- Dependências pelo construtor primário.
- Métodos assíncronos com o sufixo `Async`, exceto o `Handle` do MediatR e as actions dos controllers.
- Métodos com corpo em bloco, mesmo com um só `return`. O corpo de expressão (`=>`) só aparece em propriedades e em extensões e helpers privados de uma expressão (`ListQueryRules`, `IsUniqueViolation`, `Normalize`). Nos testes, um construtor ou membro de uma expressão pode usar `=>`, como o construtor que cria o `_sut` e os helpers `Make...Input`.
- Comentários em inglês, só para o que o código não diz sozinho: a razão de uma escolha ou a regra que o trecho cumpre. Quando a fonte é um documento, o comentário cita a RN, a seção da TechSpec ou o ADR: `// Without a size, one page holds every vaccine (RN20).`
- Pasta do caso de uso: `<Verbo><Entidade>`. Verbos padrão: `Create`, `FindAll`, `FindById` e `Delete`; verbos do domínio quando existem: `Register`, `Correct`, `SignIn`, `RefreshSession` e `SignOut`.
- Ids gerados com `Guid.CreateVersion7()`.
- O handler aplica `Trim` nos nomes antes de chamar o domínio; senhas passam sem alteração.

## Frontend

Estrutura completa na TechSpec §11.1. Pastas em `frontend/src/app/`:

| Artefato | Pasta |
|---|---|
| service HTTP de um recurso (`<Recurso>ApiService`) | `core/api/<recurso>-api.service.ts` |
| contrato da API (`*Dto`, `IPagination<T>`, `ProblemDetails`) | `core/models/<recurso>.model.ts` |
| sessão, guard e interceptor | `core/auth`, `core/guards`, `core/interceptors` |
| segmentos de rota (`RoutesEnum`) e `routePath()` | `core/routes` |
| service global sem HTTP (tema) | `core/services` |
| função utilitária pura | `core/utils` |
| layout de um módulo (`<Módulo>Layout`) e as rotas dele | `modules/<módulo>/<módulo>.ts` e `modules/<módulo>/<módulo>.routes.ts` |
| página | `modules/<módulo>/pages/<página>/` |
| componente usado por uma página só | `modules/<módulo>/pages/<página>/components/<componente>/` |
| componente, página ou pipe usado por mais de um módulo ou página | `shared/components`, `shared/pages`, `shared/pipes` |

Regras:

- Componente: classe sem sufixo (`PeopleList`), arquivos `<nome>.ts`, `.html` e `.scss` e `ChangeDetectionStrategy.OnPush`. O `standalone: true` fica de fora, porque já é o padrão.
- Membros: `public readonly` para `input()` e `output()`, `protected readonly` para o que o HTML do componente usa e `private readonly` para as dependências, sempre por `inject()`.
- Estado em signals (`signal` e `computed`), alterado com `set` ou `update`. Um observable vira signal com `toSignal` quando o HTML precisa dele.
- HTML com `@if`, `@for` e `@switch`, bindings de `class` e `style` no lugar de `ngClass` e `ngStyle` e eventos e atributos do host no objeto `host` do decorator, no lugar de `@HostBinding` e `@HostListener`.
- Formulários reativos tipados, com os campos de `shared/components/inputs` e o `submit-button`.
- Caminhos de rota por `routePath(RoutesEnum...)`, sem string solta.
- A API é chamada por `environment.apiUrl`, um caminho relativo (`/api/v1`).
- O access token vive só num signal do `AuthSessionService`.
- Erros da API passam por `core/utils/api-error.ts`: os `errors` do ProblemDetails vão para os campos, e o resto vai para o `ToastService`.
- Cores, espaçamentos e raios vêm das variáveis de `_tokens.scss`, e toda tela funciona nos temas claro e escuro.
- Acessibilidade no nível AA das WCAG: rótulo em todo campo e botão de ícone, foco visível e contraste.
- Datas em `yyyy-MM-dd` na API e em `dd/MM/yyyy` na tela.
