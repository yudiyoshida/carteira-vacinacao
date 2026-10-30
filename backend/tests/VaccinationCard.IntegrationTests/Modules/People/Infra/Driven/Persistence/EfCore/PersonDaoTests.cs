using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.IntegrationTests.Modules.People.Infra.Driven.Persistence.EfCore;

// The DAO only reads, so the Arrange registers people and vaccinations through the use cases. The tiebreaker test
// writes the records straight to the table, to pick their ids.
public sealed class PersonDaoTests : IntegrationTest
{
    // People out of name order, each with a valid CPF.
    private static readonly (string Name, string Cpf)[] UnorderedPeople =
    [
        ("Maria Souza", "52998224725"),
        ("João da Silva", "11144477735"),
        ("Ana Conceição", "93541134780"),
        ("José Pereira", "39053344705"),
        ("Bruna Álvares", "86243816060"),
    ];

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
    public async Task Should_Return_Vaccination_When_It_Belongs_To_The_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = await SendAsync(MakeCreatePersonInput(), cancellationToken);
        var (vaccineId, vaccinationId) = await RegisterFirstDoseAsync(person.Id, cancellationToken);

        // Act
        var vaccination = await _sut.FindVaccinationAsync(person.Id, vaccinationId, cancellationToken);

        // Assert
        vaccination.ShouldBe(new VaccinationDto(vaccinationId, vaccineId, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)));
    }

    [Fact]
    public async Task Should_Return_Null_When_Vaccination_Belongs_To_Another_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var owner = await SendAsync(MakeCreatePersonInput(), cancellationToken);
        var other = await SendAsync(MakeCreatePersonInput() with { Name = "Ana Conceição", Cpf = "93541134780" }, cancellationToken);
        var (_, vaccinationId) = await RegisterFirstDoseAsync(owner.Id, cancellationToken);

        // Act
        var vaccination = await _sut.FindVaccinationAsync(other.Id, vaccinationId, cancellationToken);

        // Assert
        vaccination.ShouldBeNull();
    }

    // Another person also has a vaccination, which stays out of the card data.
    [Fact]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Person_With_Cpf_Digits_And_Their_Vaccinations_When_Finding_Card_Data()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = await SendAsync(MakeCreatePersonInput() with { Cpf = "123.456.789-09" }, cancellationToken);
        var other = await SendAsync(MakeCreatePersonInput() with { Name = "Ana Conceição", Cpf = "93541134780" }, cancellationToken);
        var (vaccineId, firstId) = await RegisterFirstDoseAsync(person.Id, cancellationToken);
        var second = await SendAsync(new RegisterVaccinationCommand(person.Id, vaccineId, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 2, 15)), cancellationToken);
        await SendAsync(new RegisterVaccinationCommand(other.Id, vaccineId, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);

        // Act
        var data = await _sut.FindCardDataAsync(person.Id, cancellationToken);

        // Assert
        data.ShouldNotBeNull();
        data.Person.ShouldBe(new PersonDto(person.Id, "Maria Silva", "12345678909"));
        data.Vaccinations.ShouldBe(
        [
            new VaccinationDto(firstId, vaccineId, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)),
            new VaccinationDto(second.Id, vaccineId, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 2, 15)),
        ], ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Return_Person_Without_Vaccinations_When_Finding_Card_Data_Of_A_Person_Without_Vaccination()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = await SendAsync(MakeCreatePersonInput(), cancellationToken);

        // Act
        var data = await _sut.FindCardDataAsync(person.Id, cancellationToken);

        // Assert
        data.ShouldNotBeNull();
        data.Person.ShouldBe(new PersonDto(person.Id, "Maria Silva", "12345678909"));
        data.Vaccinations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Null_When_Finding_Card_Data_Of_A_Person_That_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(MakeCreatePersonInput(), cancellationToken);

        // Act
        var data = await _sut.FindCardDataAsync(Guid.CreateVersion7(), cancellationToken);

        // Assert
        data.ShouldBeNull();
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

    [Fact]
    [Trait("RN", "RN19")]
    public async Task Should_Return_People_Ordered_By_Name_When_Listing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateUnorderedPeopleAsync(cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(null, 1, 20, cancellationToken);

        // Assert
        page.Data.Select(person => person.Name).ShouldBe(["Ana Conceição", "Bruna Álvares", "João da Silva", "José Pereira", "Maria Souza"]);
    }

    [Fact]
    [Trait("RN", "RN26")]
    public async Task Should_Return_People_With_Cpf_Digits_When_Listing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(MakeCreatePersonInput() with { Cpf = "123.456.789-09" }, cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(null, 1, 20, cancellationToken);

        // Assert
        page.Data.ShouldBe([new PersonSummaryDto(created.Id, "Maria Silva", "12345678909")]);
    }

    [Theory]
    [Trait("RN", "RN19")]
    [InlineData("conceicao", "Ana Conceição")]
    [InlineData("JOÃO", "João da Silva")]
    [InlineData("joao", "João da Silva")]
    [InlineData("alvares", "Bruna Álvares")]
    [InlineData("SOUZA", "Maria Souza")]
    public async Task Should_Find_Person_When_Search_Is_Part_Of_The_Name_Without_Case_Or_Accents(string search, string name)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateUnorderedPeopleAsync(cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(search, 1, 20, cancellationToken);

        // Assert
        page.Data.Select(person => person.Name).ShouldBe([name]);
    }

    [Theory]
    [Trait("RN", "RN19")]
    [InlineData("%")]
    [InlineData("_")]
    public async Task Should_Treat_Wildcards_As_Text_When_Searching(string search)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateUnorderedPeopleAsync(cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(search, 1, 20, cancellationToken);

        // Assert
        page.Data.ShouldBeEmpty();
    }

    [Fact]
    [Trait("RN", "RN19")]
    public async Task Should_Return_Requested_Page_When_Page_And_Size_Are_Given()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateUnorderedPeopleAsync(cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(null, 2, 2, cancellationToken);

        // Assert
        page.CurrentPage.ShouldBe(2);
        page.ItemsPerPage.ShouldBe(2);
        page.TotalItems.ShouldBe(5);
        page.TotalPages.ShouldBe(3);
        page.Data.Select(person => person.Name).ShouldBe(["João da Silva", "José Pereira"]);
    }

    // Names repeat, and the id breaks the ties, so each person is on exactly one page (TechSpec §6.2). The records go in
    // against the order of their ids, so the order of insertion cannot pass for the tiebreaker.
    [Fact]
    [Trait("RN", "RN19")]
    public async Task Should_Break_Name_Ties_By_Id_When_Paging()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var registeredAt = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        Guid[] ids = [Guid.CreateVersion7(registeredAt), Guid.CreateVersion7(registeredAt.AddSeconds(1)), Guid.CreateVersion7(registeredAt.AddSeconds(2))];
        await InsertPersonAsync(ids[2], "25341895061", cancellationToken);
        await InsertPersonAsync(ids[1], "71460238001", cancellationToken);
        await InsertPersonAsync(ids[0], "12345678909", cancellationToken);

        // Act
        var first = await _sut.FindAllAsync(null, 1, 1, cancellationToken);
        var second = await _sut.FindAllAsync(null, 2, 1, cancellationToken);
        var third = await _sut.FindAllAsync(null, 3, 1, cancellationToken);

        // Assert
        first.Data.Concat(second.Data).Concat(third.Data).Select(person => person.Id).ShouldBe(ids);
    }

    private async Task CreateUnorderedPeopleAsync(CancellationToken cancellationToken)
    {
        foreach (var (name, cpf) in UnorderedPeople)
        {
            await SendAsync(MakeCreatePersonInput() with { Name = name, Cpf = cpf }, cancellationToken);
        }
    }

    // The first dose of a new vaccine, registered for the person through the use case.
    private async Task<(Guid VaccineId, Guid VaccinationId)> RegisterFirstDoseAsync(Guid personId, CancellationToken cancellationToken)
    {
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var vaccination = await SendAsync(new RegisterVaccinationCommand(personId, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        return (vaccine.Id, vaccination.Id);
    }

    // A namesake of "Maria Silva", written straight to the table, so the test picks the id.
    private Task<int> InsertPersonAsync(Guid id, string cpf, CancellationToken cancellationToken)
    {
        return ExecuteDbContextAsync(db =>
        {
            db.Add(new PersonRecord { Id = id, Name = "Maria Silva", Cpf = cpf });
            return db.SaveChangesAsync(cancellationToken);
        });
    }

    private static CreatePersonCommand MakeCreatePersonInput() => new("Maria Silva", "12345678909");
}
