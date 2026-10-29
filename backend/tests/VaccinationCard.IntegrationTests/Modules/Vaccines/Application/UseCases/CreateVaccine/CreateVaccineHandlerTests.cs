using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.Vaccines.Application.UseCases.CreateVaccine;

public sealed class CreateVaccineHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Save_Vaccine_Without_Surrounding_Spaces_When_Input_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCreateVaccineInput() with { Name = "  Hepatite B  " };

        // Act
        var output = await SendAsync(input, cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<VaccineRecord>().SingleAsync(x => x.Id == output.Id, cancellationToken));
        record.Name.ShouldBe("Hepatite B");
        record.NormalizedName.ShouldBe("hepatite b");
        record.PrimaryDoses.ShouldBe((short)3);
        record.BoosterDoses.ShouldBe((short)0);
    }

    [Theory]
    [Trait("RN", "RN05")]
    [InlineData("hepatite b")]
    [InlineData("HEPATITE B")]
    [InlineData("  Hepatite B  ")]
    public async Task Should_Reject_Vaccine_When_Name_Differs_Only_By_Case_Or_Surrounding_Spaces(string name)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(MakeCreateVaccineInput(), cancellationToken);

        // Act
        var act = () => SendAsync(MakeCreateVaccineInput() with { Name = name }, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccine.name_already_registered");
    }

    private static CreateVaccineCommand MakeCreateVaccineInput() => new("Hepatite B", 3, 0);
}
