using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Shared.Errors;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.CreatePerson;

public sealed class CreatePersonHandlerTests
{
    private readonly IPersonDao _dao = Substitute.For<IPersonDao>();
    private readonly IPersonRepository _repository = Substitute.For<IPersonRepository>();
    private readonly CreatePersonHandler _sut;

    public CreatePersonHandlerTests() => _sut = new CreatePersonHandler(_dao, _repository);

    [Fact]
    public async Task Should_Save_Person_And_Return_Its_Id_When_Cpf_Is_New()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCreatePersonInput();

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.Id.Version.ShouldBe(7);
        await _repository.Received(1).SaveAsync(
            Arg.Is<Person>(person => person.Id == output.Id && person.Name == "Maria Silva" && person.Cpf == Cpf.Create("12345678909")),
            cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN01")]
    public async Task Should_Trim_Name_When_Name_Has_Surrounding_Spaces()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCreatePersonInput() with { Name = "  Maria Silva  " };

        // Act
        await _sut.Handle(input, cancellationToken);

        // Assert
        await _repository.Received(1).SaveAsync(Arg.Is<Person>(person => person.Name == "Maria Silva"), cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN02")]
    public async Task Should_Keep_Only_The_Cpf_Digits_When_Cpf_Has_The_Mask()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCreatePersonInput() with { Cpf = "123.456.789-09" };

        // Act
        await _sut.Handle(input, cancellationToken);

        // Assert
        await _dao.Received(1).ExistsByCpfAsync(Arg.Is<Cpf>(cpf => cpf.Value == "12345678909"), cancellationToken);
        await _repository.Received(1).SaveAsync(Arg.Is<Person>(person => person.Cpf.Value == "12345678909"), cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN03")]
    public async Task Should_Reject_Person_When_Cpf_Is_Already_Registered()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCreatePersonInput();
        _dao.ExistsByCpfAsync(Cpf.Create(input.Cpf), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("person.cpf_already_registered");
        await _repository.DidNotReceive().SaveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    private static CreatePersonCommand MakeCreatePersonInput() => new("Maria Silva", "12345678909");
}
