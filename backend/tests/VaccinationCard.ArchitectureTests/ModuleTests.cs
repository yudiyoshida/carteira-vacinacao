using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace VaccinationCard.ArchitectureTests;

public sealed class ModuleTests
{
    private const string Queries = @"^VaccinationCard\.Api\.Modules\.\w+\.Application\.UseCases\.\w+\.\w+Query$";
    private const string Dtos = @"^VaccinationCard\.Api\.Modules\.\w+\.Application\.Dtos\.\w+Dto$";

    // A4 exception: People also uses the value objects and enums of Vaccines.Domain.
    private static readonly Dictionary<string, string> DomainTypesFromAnotherModule = new()
    {
        ["People"] = @"^VaccinationCard\.Api\.Modules\.Vaccines\.Domain\.(ValueObjects|Enums)\..+$",
    };

    [Fact]
    public void A3_Module_Does_Not_Depend_On_Infra_Or_Persistence_Of_Another_Module() =>
        CheckEveryModule(module => Types()
            .That()
            .ResideInNamespaceMatching($@"^VaccinationCard\.Api\.Modules\.{module}(\..+)?$")
            .Should()
            .NotDependOnAny(Types().That().ResideInNamespaceMatching($@"{AnotherModule(module)}\.(Infra|Application\.Persistence)(\..+)?$"))
            .WithoutRequiringPositiveResults()
        );

    [Fact]
    public void A4_Module_Uses_Only_Queries_And_Dtos_Of_Another_Module() =>
        CheckEveryModule(module => Types()
            .That()
            .ResideInNamespaceMatching($@"^VaccinationCard\.Api\.Modules\.{module}\.(Domain|Application)(\..+)?$")
            .Should()
            .NotDependOnAny(Types().That().ResideInNamespaceMatching($@"{AnotherModule(module)}(\..+)?$")
            .And()
            .DoNotHaveFullNameMatching(AllowedFromAnotherModule(module)))
            .WithoutRequiringPositiveResults()
        );

    // One combined rule reports the violations of every module, not just the first failing one.
    private static void CheckEveryModule(Func<string, IArchRule> ruleFor)
    {
        var rules = ApiArchitecture.Modules.Select(ruleFor).ToList();
        if (rules.Count > 0)
        {
            rules.Aggregate((combined, rule) => combined.And(rule)).Check(ApiArchitecture.Instance);
        }
    }

    private static string AnotherModule(string module) => $@"^VaccinationCard\.Api\.Modules\.(?!{module}(\.|$))\w+";

    private static string AllowedFromAnotherModule(string module) =>
        DomainTypesFromAnotherModule.TryGetValue(module, out var domainTypes)
            ? $"({Queries})|({Dtos})|({domainTypes})"
            : $"({Queries})|({Dtos})";
}
