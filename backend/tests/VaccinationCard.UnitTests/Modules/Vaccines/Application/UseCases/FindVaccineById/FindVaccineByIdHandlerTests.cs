using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccineById;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Modules.Vaccines.Application.UseCases.FindVaccineById;

public sealed class FindVaccineByIdHandlerTests
{
    private readonly IVaccineDao _dao = Substitute.For<IVaccineDao>();
    private readonly FindVaccineByIdHandler _sut;

    public FindVaccineByIdHandlerTests() => _sut = new FindVaccineByIdHandler(_dao);

    [Fact]
    public async Task Should_Return_Vaccine_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var vaccine = new VaccineDto(Guid.CreateVersion7(), "Hepatite B", 3, 0);
        var input = new FindVaccineByIdQuery(vaccine.Id);
        _dao.FindByIdAsync(vaccine.Id, Arg.Any<CancellationToken>()).Returns(vaccine);

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.ShouldBe(vaccine);
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Vaccine_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new FindVaccineByIdQuery(Guid.CreateVersion7());
        _dao.FindByIdAsync(input.Id, Arg.Any<CancellationToken>()).Returns((VaccineDto?)null);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccine.not_found");
    }
}
