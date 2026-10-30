using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.UseCases.VerifyAccountCredentials;

namespace VaccinationCard.IntegrationTests.Modules.Accounts.Application.UseCases.VerifyAccountCredentials;

public sealed class VerifyAccountCredentialsHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    private const string Email = "operador@demo.local";
    private const string Password = "Demo@2026!";

    [Fact]
    [Trait("RN", "RN22")]
    public async Task Should_Return_Account_When_Credentials_Are_Right()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var account = await SendAsync(new VerifyAccountCredentialsQuery(Email, Password), cancellationToken);

        // Assert
        account.ShouldBe(new AccountDto(accountId, Email));
    }

    [Theory]
    [InlineData("OPERADOR@DEMO.LOCAL")]
    [InlineData("  operador@demo.local  ")]
    public async Task Should_Return_Account_When_Email_Differs_By_Case_Or_Spaces(string email)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var account = await SendAsync(new VerifyAccountCredentialsQuery(email, Password), cancellationToken);

        // Assert
        account.ShouldBe(new AccountDto(accountId, Email));
    }

    [Fact]
    [Trait("RN", "RN22")]
    public async Task Should_Return_Null_When_Password_Is_Wrong()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var account = await SendAsync(new VerifyAccountCredentialsQuery(Email, "Senha@Errada1"), cancellationToken);

        // Assert
        account.ShouldBeNull();
    }

    [Fact]
    [Trait("RN", "RN22")]
    public async Task Should_Return_Null_When_Email_Is_Not_Registered()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var account = await SendAsync(new VerifyAccountCredentialsQuery("outro@demo.local", Password), cancellationToken);

        // Assert
        account.ShouldBeNull();
    }
}
