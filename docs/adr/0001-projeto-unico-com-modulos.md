# Projeto único organizado por módulos

O backend é um único projeto ASP.NET Core (`VaccinationCard.Api`), dividido em módulos de feature (`People`, `Vaccines`, `Accounts`, `Authentication`). Cada módulo tem as pastas `Domain`, `Application` e `Infra`. Como o compilador não separa camadas dentro de um projeto, testes de arquitetura (ArchUnitNET) garantem a direção das dependências e rodam no CI.

## Opções consideradas

- **Um projeto por camada** (Domain, Application, Infrastructure, Api), com os módulos como pastas: o compilador garante as camadas, mas cada módulo fica espalhado por quatro projetos.
- **Projeto único com módulos** (escolhida): cada módulo fica coeso numa pasta só, e as regras de dependência passam a depender dos testes de arquitetura.

## Consequências

- Uma regra de dependência quebrada falha no `dotnet test`, não no `dotnet build`.
- `internal` não isola camadas. A criação de entidades passa pelas factories por convenção.
