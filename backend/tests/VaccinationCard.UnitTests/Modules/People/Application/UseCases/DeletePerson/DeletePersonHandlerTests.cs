using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Application.UseCases.DeletePerson;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.DeletePerson;

public sealed class DeletePersonHandlerTests
{
    private readonly IPersonRepository _repository = Substitute.For<IPersonRepository>();
    private readonly DeletePersonHandler _sut;

    public DeletePersonHandlerTests() => _sut = new DeletePersonHandler(_repository);

    [Fact]
    [Trait("RN", "RN04")]
    public async Task Should_Return_Success_Message_When_Person_Is_Removed()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new DeletePersonCommand(Guid.CreateVersion7());
        _repository.DeleteAsync(input.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.ShouldBe(new SuccessMessage("Pessoa removida com sucesso."));
        await _repository.Received(1).DeleteAsync(input.Id, cancellationToken);
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new DeletePersonCommand(Guid.CreateVersion7());
        _repository.DeleteAsync(input.Id, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
    }
}
