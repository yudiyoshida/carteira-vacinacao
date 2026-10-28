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

Todo módulo tem `Domain/`, `Application/` e `Infra/`, e registra as próprias dependências em `<Módulo>Module.cs` (`Add<Módulo>Module`), chamado pelo `Program.cs`. Entidade só existe onde há regra de negócio; um módulo CRUD lê e grava pelo DAO (`docs/adr/0002-ddd-light-so-onde-ha-regra.md`).

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
| implementação de port de segurança | `<M>/Infra/Driven/Security` |
| value object usado por mais de um módulo | `Shared/ValueObjects` |
| exceções de erro e envelope de paginação | `Shared/Errors`, `Shared/Pagination` |
| behavior do MediatR, relógio e validação comum | `Core/Behaviors`, `Core/Time`, `Core/Validation` |
| `AppDbContext`, migrations e seeds | `Infra/Database` |
| middleware e handlers HTTP globais | `Infra/Http` |

### Dependências

| # | Regra |
|---|---|
| A1 | `Modules.*.Domain` usa só `Shared`, a BCL e o que A4 permite. |
| A2 | `Modules.*.Application` acessa os dados do módulo só pelos ports de `Application.Persistence`. Não depende de `Infra` (do módulo ou global), EF Core, Npgsql nem ASP.NET Core. |
| A3 | Um módulo não depende da `Infra` nem da `Application.Persistence` de outro módulo. |
| A4 | Para usar outro módulo, a `Application` envia uma query dele (`ISender.Send`) e recebe DTOs; cada módulo grava só os próprios dados (`docs/adr/0011-comunicacao-entre-modulos-por-queries.md`). Exceção: o `Domain` e a `Application` de `People` usam também os value objects e enums de `Vaccines.Domain`. |
| A5 | O controller (`Infra.Drivers.Http`) monta o command ou a query e chama `ISender.Send`. Não depende de `Application.Persistence` nem de EF Core. |
| A6 | `Shared` não depende de `Core`, `Infra` nem `Modules`. `Core` não depende de `Infra`, `Modules`, EF Core nem ASP.NET Core. |
| A7 | Sufixos obrigatórios: `*Command`, `*Query`, `*Handler`, `*Validator`, `*Output`, `*Dto`, `*Dao`, `*Repository`, `*Controller`, `*Record`. |

### Convenções de código

- Um tipo público por arquivo, com namespace por arquivo.
- Classes `sealed` por padrão. Command, query, DTO e output são `record`.
- Dependências pelo construtor primário.
- Métodos assíncronos com o sufixo `Async`, exceto o `Handle` do MediatR e as actions dos controllers.
- Pasta do caso de uso: `<Verbo><Entidade>`. Verbos padrão: `Create`, `FindAll`, `FindById` e `Delete`; verbos do domínio quando existem: `Register`, `Correct`, `SignIn`, `RefreshSession` e `SignOut`.
- Ids gerados com `Guid.CreateVersion7()`.
- O handler aplica `Trim` nos nomes antes de chamar o domínio; senhas passam sem alteração.

## Frontend

Pastas em `frontend/src/app/`:

- `core/`: infraestrutura com uma instância só no app: autenticação, tratamento de erros HTTP e layout.
- `shared/`: pipes, componentes e modelos reutilizáveis, sem estado.
- `features/<feature>/`: telas de uma área, carregadas por rota lazy. O que duas features usam fica em `shared/`.

Regras:

- Standalone components, signals e zoneless; reactive forms tipados; estado em services com signals.
- Cada feature chama a API pelo próprio service, com caminhos relativos (`/api/v1/...`).
- O access token vive só num signal do `AuthService`.
- O ProblemDetails passa por `core/http`: os `errors` vão para os campos do formulário, e o resto vai para o snackbar.
- Datas em `yyyy-MM-dd` na API e em `dd/MM/yyyy` na tela.
