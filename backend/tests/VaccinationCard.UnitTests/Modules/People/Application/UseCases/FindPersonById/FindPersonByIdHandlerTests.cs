using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindPersonById;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.FindPersonById;

public sealed class FindPersonByIdHandlerTests
{
    private readonly IPersonDao _dao = Substitute.For<IPersonDao>();
    private readonly FindPersonByIdHandler _sut;

    public FindPersonByIdHandlerTests() => _sut = new FindPersonByIdHandler(_dao);

    [Fact]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Person_With_Formatted_Cpf_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = new PersonDto(Guid.CreateVersion7(), "Maria Silva", "12345678909");
        var input = new FindPersonByIdQuery(person.Id);
        _dao.FindByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.ShouldBe(person with { Cpf = "123.456.789-09" });
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new FindPersonByIdQuery(Guid.CreateVersion7());
        _dao.FindByIdAsync(input.Id, Arg.Any<CancellationToken>()).Returns((PersonDto?)null);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
    }
}
