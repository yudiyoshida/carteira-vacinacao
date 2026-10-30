using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.DeletePerson;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.People.Application.UseCases.DeletePerson;

public sealed class DeletePersonHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    [Trait("RN", "RN04")]
    public async Task Should_Remove_Person_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var input = new DeletePersonCommand(created.Id);

        // Act
        var output = await SendAsync(input, cancellationToken);

        // Assert
        output.ShouldBe(new SuccessMessage("Pessoa removida com sucesso."));
        var exists = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().AnyAsync(x => x.Id == created.Id, cancellationToken));
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new DeletePersonCommand(Guid.CreateVersion7());

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
    }
}
