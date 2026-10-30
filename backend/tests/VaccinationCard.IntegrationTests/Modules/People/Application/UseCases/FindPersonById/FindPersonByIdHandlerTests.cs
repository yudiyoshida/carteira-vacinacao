using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindPersonById;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.People.Application.UseCases.FindPersonById;

public sealed class FindPersonByIdHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Person_With_Formatted_Cpf_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var input = new FindPersonByIdQuery(created.Id);

        // Act
        var person = await SendAsync(input, cancellationToken);

        // Assert
        person.ShouldBe(new PersonDto(created.Id, "Maria Silva", "123.456.789-09"));
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new FindPersonByIdQuery(Guid.CreateVersion7());

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
    }
}
