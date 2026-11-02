using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

public sealed class VaccineDaoTests : IntegrationTest
{
    // The vaccines of PRD §9, out of name order.
    private static readonly string[] UnorderedNames =
    [
        "Tríplice viral",
        "BCG",
        "Poliomielite inativada (VIP)",
        "Hepatite B",
        "Febre amarela",
        "DTP",
        "Meningocócica C",
        "Pneumocócica 10-valente",
    ];

    private readonly AsyncServiceScope _scope;
    private readonly VaccineDao _sut;

    public VaccineDaoTests(IntegrationFixture fixture) : base(fixture)
    {
        _scope = fixture.Factory.Services.CreateAsyncScope();
        _sut = new VaccineDao(_scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Store_Normalized_Name_When_Saving_A_Vaccine()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSaveVaccineInput();

        // Act
        await _sut.SaveAsync(input, cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<VaccineRecord>().SingleAsync(x => x.Id == input.Id, cancellationToken));
        record.Name.ShouldBe("Hepatite B");
        record.NormalizedName.ShouldBe("hepatite b");
        record.PrimaryDoses.ShouldBe((short)3);
        record.BoosterDoses.ShouldBe((short)0);
        record.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task Should_Return_Vaccine_When_Id_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSaveVaccineInput();
        await _sut.SaveAsync(input, cancellationToken);

        // Act
        var vaccine = await _sut.FindByIdAsync(input.Id, cancellationToken);

        // Assert
        vaccine.ShouldBe(input);
    }

    [Fact]
    public async Task Should_Return_Null_When_Id_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveVaccineInput(), cancellationToken);

        // Act
        var vaccine = await _sut.FindByIdAsync(Guid.CreateVersion7(), cancellationToken);

        // Assert
        vaccine.ShouldBeNull();
    }

    // DTP is stored but not asked for, the new id matches no vaccine, and neither the storage order nor the order of
    // the ids is the name order.
    [Fact]
    [Trait("RN", "RN17")]
    public async Task Should_Return_The_Vaccines_Of_The_Ids_Ordered_By_Name_When_Finding_By_Ids()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tripleViral = MakeSaveVaccineInput() with { Name = "Tríplice viral", PrimaryDoses = 2 };
        var hepatitisB = MakeSaveVaccineInput();
        var bcg = MakeSaveVaccineInput() with { Name = "BCG", PrimaryDoses = 1 };
        var dtp = MakeSaveVaccineInput() with { Name = "DTP", BoosterDoses = 2 };
        foreach (var vaccine in new[] { tripleViral, hepatitisB, bcg, dtp })
        {
            await _sut.SaveAsync(vaccine, cancellationToken);
        }

        // Act
        var vaccines = await _sut.FindByIdsAsync([tripleViral.Id, Guid.CreateVersion7(), bcg.Id, hepatitisB.Id], cancellationToken);

        // Assert
        vaccines.ShouldBe([bcg, hepatitisB, tripleViral]);
    }

    [Fact]
    public async Task Should_Return_No_Vaccine_When_Finding_By_No_Id()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveVaccineInput(), cancellationToken);

        // Act
        var vaccines = await _sut.FindByIdsAsync([], cancellationToken);

        // Assert
        vaccines.ShouldBeEmpty();
    }

    [Theory]
    [Trait("RN", "RN05")]
    [InlineData("Hepatite B")]
    [InlineData("hepatite b")]
    [InlineData("HEPATITE B")]
    [InlineData("  Hepatite B  ")]
    public async Task Should_Find_Name_When_It_Differs_Only_By_Case_Or_Surrounding_Spaces(string name)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveVaccineInput(), cancellationToken);

        // Act
        var exists = await _sut.ExistsByNameAsync(name, cancellationToken);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Not_Find_Name_When_No_Vaccine_Has_It()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveVaccineInput(), cancellationToken);

        // Act
        var exists = await _sut.ExistsByNameAsync("Hepatite A", cancellationToken);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Throw_Conflict_When_Unique_Name_Index_Is_Violated()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveVaccineInput(), cancellationToken);
        var input = MakeSaveVaccineInput() with { Name = "HEPATITE B" };

        // Act
        var act = () => _sut.SaveAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccine.name_already_registered");
    }

    [Theory]
    [Trait("RN", "RN06")]
    [InlineData(0, 0, "ck_vaccines_primary_doses")]
    [InlineData(11, 0, "ck_vaccines_primary_doses")]
    [InlineData(1, -1, "ck_vaccines_booster_doses")]
    [InlineData(1, 6, "ck_vaccines_booster_doses")]
    public async Task Should_Reject_Scheme_When_Doses_Break_A_Database_Check(int primaryDoses, int boosterDoses, string constraint)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSaveVaccineInput() with { PrimaryDoses = primaryDoses, BoosterDoses = boosterDoses };

        // Act
        var act = () => _sut.SaveAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<DbUpdateException>(act);
        var postgres = exception.InnerException.ShouldBeOfType<PostgresException>();
        postgres.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        postgres.ConstraintName.ShouldBe(constraint);
    }

    [Fact]
    [Trait("RN", "RN20")]
    public async Task Should_Return_Vaccines_Ordered_By_Name_When_Listing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveVaccinesAsync(UnorderedNames, cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(null, 1, null, cancellationToken);

        // Assert
        page.Data.Select(vaccine => vaccine.Name).ShouldBe(
        [
            "BCG",
            "DTP",
            "Febre amarela",
            "Hepatite B",
            "Meningocócica C",
            "Pneumocócica 10-valente",
            "Poliomielite inativada (VIP)",
            "Tríplice viral",
        ]);
    }

    [Theory]
    [Trait("RN", "RN20")]
    [InlineData("patite", "Hepatite B")]
    [InlineData("HEPA", "Hepatite B")]
    [InlineData("triplice", "Tríplice viral")]
    [InlineData("TRÍPLICE", "Tríplice viral")]
    [InlineData("fébre", "Febre amarela")]
    public async Task Should_Find_Vaccines_When_Search_Is_Part_Of_The_Name_Without_Case_Or_Accents(string search, string name)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveVaccinesAsync(UnorderedNames, cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(search, 1, null, cancellationToken);

        // Assert
        page.Data.Select(vaccine => vaccine.Name).ShouldBe([name]);
    }

    [Theory]
    [Trait("RN", "RN20")]
    [InlineData("%", "Teste 100%")]
    [InlineData("_", "Covid_19")]
    [InlineData(@"\", @"A\B")]
    public async Task Should_Treat_Wildcards_As_Text_When_Searching(string search, string name)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveVaccinesAsync(["Teste 100%", "Teste 1000", "Covid_19", "Covid-19", @"A\B", "AB"], cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(search, 1, null, cancellationToken);

        // Assert
        page.Data.Select(vaccine => vaccine.Name).ShouldBe([name]);
    }

    [Fact]
    [Trait("RN", "RN20")]
    public async Task Should_Return_Every_Vaccine_In_One_Page_When_Size_Is_Missing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveVaccinesAsync(UnorderedNames, cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(null, 1, null, cancellationToken);

        // Assert
        page.CurrentPage.ShouldBe(1);
        page.ItemsPerPage.ShouldBe(8);
        page.TotalItems.ShouldBe(8);
        page.TotalPages.ShouldBe(1);
        page.Data.Count.ShouldBe(8);
    }

    [Fact]
    public async Task Should_Return_Requested_Page_When_Page_And_Size_Are_Given()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveVaccinesAsync(UnorderedNames, cancellationToken);

        // Act
        var page = await _sut.FindAllAsync(null, 2, 3, cancellationToken);

        // Assert
        page.CurrentPage.ShouldBe(2);
        page.ItemsPerPage.ShouldBe(3);
        page.TotalItems.ShouldBe(8);
        page.TotalPages.ShouldBe(3);
        page.Data.Select(vaccine => vaccine.Name).ShouldBe(["Hepatite B", "Meningocócica C", "Pneumocócica 10-valente"]);
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(int.MaxValue, 100)]
    [InlineData(2, null)]
    public async Task Should_Return_Empty_Page_When_Page_Is_Beyond_The_Last(int page, int? size)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveVaccinesAsync(UnorderedNames, cancellationToken);

        // Act
        var result = await _sut.FindAllAsync(null, page, size, cancellationToken);

        // Assert
        result.CurrentPage.ShouldBe(page);
        result.TotalItems.ShouldBe(8);
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Empty_Envelope_When_Search_Finds_No_Vaccine()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveVaccinesAsync(UnorderedNames, cancellationToken);

        // Act
        var page = await _sut.FindAllAsync("rotavírus", 1, null, cancellationToken);

        // Assert
        page.CurrentPage.ShouldBe(1);
        page.ItemsPerPage.ShouldBe(0);
        page.TotalItems.ShouldBe(0);
        page.TotalPages.ShouldBe(0);
        page.Data.ShouldBeEmpty();
    }

    private async Task SaveVaccinesAsync(string[] names, CancellationToken cancellationToken)
    {
        foreach (var name in names)
        {
            await _sut.SaveAsync(MakeSaveVaccineInput() with { Name = name }, cancellationToken);
        }
    }

    private static VaccineDto MakeSaveVaccineInput() => new(Guid.CreateVersion7(), "Hepatite B", 3, 0);
}
