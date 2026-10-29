using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace VaccinationCard.ArchitectureTests;

public sealed class NamingTests
{
    [Theory]
    [InlineData(@"Application\.UseCases\.\w+", "Command|Query|Handler|Validator|Output")]
    [InlineData(@"Application\.Dtos", "Dto")]
    [InlineData(@"Application\.Persistence\.Dao", "Dao")]
    [InlineData(@"Application\.Persistence\.Repository", "Repository")]
    [InlineData(@"Infra\.Drivers\.Http", "Controller")]
    [InlineData(@"Infra\.Driven\.Persistence\.EfCore", "Record|Configuration|Dao|Repository")]
    public void A7_Types_Have_The_Suffix_Of_Their_Folder(string folder, string suffixes)
    {
        IArchRule rule = Types()
            .That()
            .ResideInNamespaceMatching($@"^VaccinationCard\.Api\.Modules\.\w+\.{folder}$")
            .And()
            .AreNotNested()
            .Should()
            .HaveNameMatching($"({suffixes})$")
            .WithoutRequiringPositiveResults();

        rule.Check(ApiArchitecture.Instance);
    }
}
