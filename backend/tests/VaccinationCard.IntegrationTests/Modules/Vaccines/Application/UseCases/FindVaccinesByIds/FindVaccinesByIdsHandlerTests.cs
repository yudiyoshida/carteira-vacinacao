using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccinesByIds;

namespace VaccinationCard.IntegrationTests.Modules.Vaccines.Application.UseCases.FindVaccinesByIds;

public sealed class FindVaccinesByIdsHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    // Hepatite B is stored but not asked for, and the new id matches no vaccine.
    [Fact]
    public async Task Should_Return_The_Vaccines_Of_The_Ids_In_Name_Order_When_They_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tripleViral = await SendAsync(new CreateVaccineCommand("Tríplice viral", 2, 0), cancellationToken);
        await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var bcg = await SendAsync(new CreateVaccineCommand("BCG", 1, 0), cancellationToken);
        var input = new FindVaccinesByIdsQuery([tripleViral.Id, Guid.CreateVersion7(), bcg.Id]);

        // Act
        var vaccines = await SendAsync(input, cancellationToken);

        // Assert
        vaccines.ShouldBe([new VaccineDto(bcg.Id, "BCG", 1, 0), new VaccineDto(tripleViral.Id, "Tríplice viral", 2, 0)]);
    }
}
