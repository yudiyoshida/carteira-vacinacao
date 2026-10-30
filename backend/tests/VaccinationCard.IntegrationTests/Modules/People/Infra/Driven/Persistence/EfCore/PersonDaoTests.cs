using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.IntegrationTests.Modules.People.Infra.Driven.Persistence.EfCore;

// The DAO only reads, so the Arrange registers people through the use case.
public sealed class PersonDaoTests : IntegrationTest
{
    private readonly AsyncServiceScope _scope;
    private readonly PersonDao _sut;

    public PersonDaoTests(IntegrationFixture fixture) : base(fixture)
    {
        _scope = fixture.Factory.Services.CreateAsyncScope();
        _sut = new PersonDao(_scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Person_With_Cpf_Digits_When_Id_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(MakeCreatePersonInput() with { Cpf = "123.456.789-09" }, cancellationToken);

        // Act
        var person = await _sut.FindByIdAsync(created.Id, cancellationToken);

        // Assert
        person.ShouldBe(new PersonDto(created.Id, "Maria Silva", "12345678909"));
    }

    [Fact]
    public async Task Should_Return_Null_When_Id_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(MakeCreatePersonInput(), cancellationToken);

        // Act
        var person = await _sut.FindByIdAsync(Guid.CreateVersion7(), cancellationToken);

        // Assert
        person.ShouldBeNull();
    }

    [Fact]
    [Trait("RN", "RN03")]
    public async Task Should_Find_Cpf_When_A_Person_Has_It()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(MakeCreatePersonInput(), cancellationToken);

        // Act
        var exists = await _sut.ExistsByCpfAsync(Cpf.Create("123.456.789-09"), cancellationToken);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    [Trait("RN", "RN03")]
    public async Task Should_Not_Find_Cpf_When_No_Person_Has_It()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(MakeCreatePersonInput(), cancellationToken);

        // Act
        var exists = await _sut.ExistsByCpfAsync(Cpf.Create("111.444.777-35"), cancellationToken);

        // Assert
        exists.ShouldBeFalse();
    }

    private static CreatePersonCommand MakeCreatePersonInput() => new("Maria Silva", "12345678909");
}
