# Teste de arquitetura

Exemplos reais em `backend/tests/VaccinationCard.ArchitectureTests/`: `LayerTests` (A1, A2, A5 e A6), `ModuleTests` (A3 e A4) e `NamingTests` (A7). Siga esses arquivos.

Um teste por regra de `.agents/rules/architecture.md`, com o nome começando pelo id da regra (`A2_...`). As regras usam padrões de namespace, então valem para um módulo novo sem mudar o teste.

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public sealed class LayerTests
{
    [Fact]
    public void A2_Application_Does_Not_Depend_On_Infra_Or_Frameworks()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^VaccinationCard\.Api\.Modules\.\w+\.Application(\..+)?$")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(
                @"^(VaccinationCard\.Api\.Infra|VaccinationCard\.Api\.Modules\.\w+\.Infra|Microsoft\.EntityFrameworkCore|Npgsql|Microsoft\.AspNetCore)(\..+)?$"))
            .WithoutRequiringPositiveResults();

        rule.Check(ApiArchitecture.Instance);
    }
}
```

- `ApiArchitecture.Instance` carrega a API com as dependências dela (`LoadAssembliesIncludingDependencies`). Uma regra só enxerga tipos de assemblies carregados: sem as dependências, um `ILogger` no domínio passaria em A1.
- Toda regra leva `WithoutRequiringPositiveResults()`. O ArchUnitNET reprova a regra cujo `That()` não acha tipo, e um módulo nem sempre tem todas as camadas.
- A3 e A4 valem por módulo: `ApiArchitecture.Modules` descobre os módulos pelos namespaces `Modules.<Módulo>`, e as regras de todos os módulos viram uma regra combinada, que relata todas as violações de uma vez.
- A7 confere o sufixo pela pasta e ignora os tipos aninhados, que o compilador gera para `async` e lambdas.
- Uma regra nova se prova vermelha uma vez, com uma violação proposital desfeita em seguida. Um tipo legítimo na mesma prova descarta falso positivo.
- `Check` vem do pacote `TngTech.ArchUnitNET.xUnitV3`.
