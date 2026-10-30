using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationById;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.FindVaccinationById;

public sealed class FindVaccinationByIdHandlerTests
{
    private readonly IPersonDao _dao = Substitute.For<IPersonDao>();
    private readonly FindVaccinationByIdHandler _sut;

    public FindVaccinationByIdHandlerTests() => _sut = new FindVaccinationByIdHandler(_dao);

    [Fact]
    public async Task Should_Return_Vaccination_When_It_Belongs_To_The_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var vaccination = new VaccinationDto(Guid.CreateVersion7(), Guid.CreateVersion7(), new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15));
        var input = new FindVaccinationByIdQuery(Guid.CreateVersion7(), vaccination.Id);
        _dao.FindVaccinationAsync(input.PersonId, input.VaccinationId, Arg.Any<CancellationToken>()).Returns(vaccination);

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.ShouldBe(vaccination);
    }

    [Fact]
    public async Task Should_Throw_Person_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new FindVaccinationByIdQuery(Guid.CreateVersion7(), Guid.CreateVersion7());
        _dao.FindVaccinationAsync(input.PersonId, input.VaccinationId, Arg.Any<CancellationToken>()).Returns((VaccinationDto?)null);
        _dao.FindByIdAsync(input.PersonId, Arg.Any<CancellationToken>()).Returns((PersonDto?)null);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
    }

    [Fact]
    public async Task Should_Throw_Vaccination_Not_Found_When_Person_Exists_Without_It()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new FindVaccinationByIdQuery(Guid.CreateVersion7(), Guid.CreateVersion7());
        _dao.FindVaccinationAsync(input.PersonId, input.VaccinationId, Arg.Any<CancellationToken>()).Returns((VaccinationDto?)null);
        _dao.FindByIdAsync(input.PersonId, Arg.Any<CancellationToken>()).Returns(new PersonDto(input.PersonId, "Maria Silva", "12345678909"));

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccination.not_found");
    }
}
