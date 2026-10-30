using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.People.Application.UseCases.CreatePerson;

public sealed class CreatePersonHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    [Trait("RN", "RN01")]
    [Trait("RN", "RN02")]
    public async Task Should_Save_Trimmed_Name_And_Cpf_Digits_When_Input_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCreatePersonInput() with { Name = "  Maria Silva  ", Cpf = "123.456.789-09" };

        // Act
        var output = await SendAsync(input, cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().SingleAsync(x => x.Id == output.Id, cancellationToken));
        record.Name.ShouldBe("Maria Silva");
        record.Cpf.ShouldBe("12345678909");
    }

    [Theory]
    [Trait("RN", "RN03")]
    [InlineData("12345678909")]
    [InlineData("123.456.789-09")]
    [InlineData(" 123 456 789 09 ")]
    public async Task Should_Reject_Person_When_Cpf_Is_Already_Registered_In_Any_Format(string cpf)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(MakeCreatePersonInput(), cancellationToken);

        // Act
        var act = () => SendAsync(MakeCreatePersonInput() with { Name = "Maria Souza", Cpf = cpf }, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("person.cpf_already_registered");
    }

    private static CreatePersonCommand MakeCreatePersonInput() => new("Maria Silva", "12345678909");
}
