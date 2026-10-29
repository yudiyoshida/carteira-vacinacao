using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace VaccinationCard.ArchitectureTests;

public sealed class LayerTests
{
    [Fact]
    public void A1_Domain_Depends_Only_On_Shared_And_Bcl()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^VaccinationCard\.Api\.Modules\.\w+\.Domain(\..+)?$")
            .Should().OnlyDependOnTypesThat().ResideInNamespaceMatching(@"^(System|VaccinationCard\.Api\.Shared|VaccinationCard\.Api\.Modules\.\w+\.Domain)(\..+)?$")
            .WithoutRequiringPositiveResults();

        rule.Check(ApiArchitecture.Instance);
    }

    [Fact]
    public void A2_Application_Does_Not_Depend_On_Infra_Or_Frameworks()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^VaccinationCard\.Api\.Modules\.\w+\.Application(\..+)?$")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^(VaccinationCard\.Api\.Infra|VaccinationCard\.Api\.Modules\.\w+\.Infra|Microsoft\.EntityFrameworkCore|Npgsql|Microsoft\.AspNetCore)(\..+)?$"))
            .WithoutRequiringPositiveResults();

        rule.Check(ApiArchitecture.Instance);
    }

    [Fact]
    public void A5_Controllers_Do_Not_Depend_On_Persistence_Or_Ef_Core()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^VaccinationCard\.Api\.Modules\.\w+\.Infra\.Drivers\.Http(\..+)?$")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^(VaccinationCard\.Api\.Modules\.\w+\.Application\.Persistence|VaccinationCard\.Api\.Infra\.Database|Microsoft\.EntityFrameworkCore)(\..+)?$"))
            .WithoutRequiringPositiveResults();

        rule.Check(ApiArchitecture.Instance);
    }

    [Fact]
    public void A6_Shared_Does_Not_Depend_On_Core_Infra_Or_Modules()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^VaccinationCard\.Api\.Shared(\..+)?$")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^VaccinationCard\.Api\.(Core|Infra|Modules)(\..+)?$"))
            .WithoutRequiringPositiveResults();

        rule.Check(ApiArchitecture.Instance);
    }

    [Fact]
    public void A6_Core_Does_Not_Depend_On_Infra_Modules_Or_Frameworks()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^VaccinationCard\.Api\.Core(\..+)?$")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^(VaccinationCard\.Api\.Infra|VaccinationCard\.Api\.Modules|Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore)(\..+)?$"))
            .WithoutRequiringPositiveResults();

        rule.Check(ApiArchitecture.Instance);
    }
}
