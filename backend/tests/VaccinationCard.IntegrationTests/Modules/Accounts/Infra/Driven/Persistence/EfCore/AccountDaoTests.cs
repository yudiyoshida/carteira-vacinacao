using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Infra.Driven.Persistence.EfCore;

namespace VaccinationCard.IntegrationTests.Modules.Accounts.Infra.Driven.Persistence.EfCore;

public sealed class AccountDaoTests : IntegrationTest
{
    private readonly AsyncServiceScope _scope;
    private readonly AccountDao _sut;

    public AccountDaoTests(IntegrationFixture fixture) : base(fixture)
    {
        _scope = fixture.Factory.Services.CreateAsyncScope();
        _sut = new AccountDao(_scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Should_Store_Normalized_Email_When_Saving_An_Account()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSaveAccountInput() with { Email = "  Operador@Demo.Local  " };

        // Act
        await _sut.SaveAsync(input, cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<AccountRecord>().SingleAsync(x => x.Id == input.Id, cancellationToken));
        record.Email.ShouldBe("operador@demo.local");
        record.PasswordHash.ShouldBe(input.PasswordHash);
        record.CreatedAt.ShouldNotBe(default);
    }

    [Theory]
    [InlineData("operador@demo.local")]
    [InlineData("OPERADOR@DEMO.LOCAL")]
    [InlineData("  operador@demo.local  ")]
    public async Task Should_Return_Credentials_When_Email_Matches_Without_Case_And_Spaces(string email)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSaveAccountInput();
        await _sut.SaveAsync(input, cancellationToken);

        // Act
        var credentials = await _sut.FindCredentialsByEmailAsync(email, cancellationToken);

        // Assert
        credentials.ShouldBe(input);
    }

    [Fact]
    public async Task Should_Return_Null_When_Email_Is_Not_Registered()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveAccountInput(), cancellationToken);

        // Act
        var credentials = await _sut.FindCredentialsByEmailAsync("outro@demo.local", cancellationToken);

        // Assert
        credentials.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_Account_When_Id_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSaveAccountInput();
        await _sut.SaveAsync(input, cancellationToken);

        // Act
        var account = await _sut.FindByIdAsync(input.Id, cancellationToken);

        // Assert
        account.ShouldBe(new AccountDto(input.Id, input.Email));
    }

    [Fact]
    public async Task Should_Return_Null_When_Id_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveAccountInput(), cancellationToken);

        // Act
        var account = await _sut.FindByIdAsync(Guid.CreateVersion7(), cancellationToken);

        // Assert
        account.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Violate_The_Unique_Index_When_Email_Differs_Only_By_Case()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveAccountInput(), cancellationToken);
        var repeated = MakeSaveAccountInput() with { Email = "OPERADOR@DEMO.LOCAL" };

        // Act
        var act = () => _sut.SaveAsync(repeated, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<DbUpdateException>(act);
        exception.IsUniqueViolation(AccountRecordConfiguration.EmailIndex).ShouldBeTrue();
    }

    private static AccountCredentialsDto MakeSaveAccountInput() => new(Guid.CreateVersion7(), "operador@demo.local", "hash");
}
