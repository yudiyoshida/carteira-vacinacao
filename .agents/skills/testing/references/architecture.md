# Teste de arquitetura

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

Um teste por regra de `.agents/rules/architecture.md`, com o nome começando pelo id da regra (`A2_...`). As regras usam padrões de namespace, então valem para um módulo novo sem mudar o teste.

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public sealed class LayerTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(Program).Assembly, typeof(DbContext).Assembly, typeof(ControllerBase).Assembly)
        .Build();

    [Fact]
    public void A2_Application_Does_Not_Depend_On_Ef_Core()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^VaccinationCard\.Api\.Modules\.\w+\.Application(\..+)?$")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore(\..+)?$"));

        rule.Check(Architecture);
    }
}
```

- O `ArchLoader` carrega também os assemblies das bibliotecas proibidas (EF Core, ASP.NET Core). Sem eles, a regra não enxerga os tipos externos e passa sem checar nada.
- Uma regra nova se prova vermelha uma vez, com uma violação proposital desfeita em seguida.
- `Check` vem do pacote `TngTech.ArchUnitNET.xUnitV3`.
