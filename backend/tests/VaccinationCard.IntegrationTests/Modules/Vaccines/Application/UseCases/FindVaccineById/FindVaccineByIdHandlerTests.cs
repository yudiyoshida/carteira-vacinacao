using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccineById;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.Vaccines.Application.UseCases.FindVaccineById;

public sealed class FindVaccineByIdHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Should_Return_Vaccine_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var input = new FindVaccineByIdQuery(created.Id);

        // Act
        var vaccine = await SendAsync(input, cancellationToken);

        // Assert
        vaccine.ShouldBe(new VaccineDto(created.Id, "Hepatite B", 3, 0));
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Vaccine_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new FindVaccineByIdQuery(Guid.CreateVersion7());

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccine.not_found");
    }
}
