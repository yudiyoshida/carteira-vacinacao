using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccinesByIds;

namespace VaccinationCard.UnitTests.Modules.Vaccines.Application.UseCases.FindVaccinesByIds;

public sealed class FindVaccinesByIdsHandlerTests
{
    private readonly IVaccineDao _dao = Substitute.For<IVaccineDao>();
    private readonly FindVaccinesByIdsHandler _sut;

    public FindVaccinesByIdsHandlerTests() => _sut = new FindVaccinesByIdsHandler(_dao);

    [Fact]
    public async Task Should_Return_The_Vaccines_The_Dao_Finds_When_Ids_Are_Given()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        VaccineDto[] vaccines = [new(Guid.CreateVersion7(), "BCG", 1, 0), new(Guid.CreateVersion7(), "Hepatite B", 3, 0)];
        var input = new FindVaccinesByIdsQuery([.. vaccines.Select(vaccine => vaccine.Id)]);
        _dao.FindByIdsAsync(input.Ids, Arg.Any<CancellationToken>()).Returns(vaccines);

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.ShouldBe(vaccines);
    }
}
