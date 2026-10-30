using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Modules.Accounts.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.RefreshSession;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;
using VaccinationCard.Api.Modules.Authentication.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.Authentication.Application.UseCases.RefreshSession;

public sealed class RefreshSessionHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    private const string Email = "operador@demo.local";
    private const string Password = "Demo@2026!";

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Replace_The_Hash_Of_The_Session_When_Refreshing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);
        var signedIn = await SendAsync(new SignInCommand(Email, Password), cancellationToken);

        // Act
        var renewed = await SendAsync(new RefreshSessionCommand(signedIn.RefreshToken), cancellationToken);

        // Assert
        var session = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().SingleAsync(cancellationToken));
        session.AccountId.ShouldBe(accountId);
        session.RefreshTokenHash.ShouldBe(Sha256(renewed.RefreshToken));
        session.RefreshTokenHash.ShouldNotBe(Sha256(signedIn.RefreshToken));
        session.ExpiresAt.ShouldBe(renewed.RefreshTokenExpiresAt);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Accept_The_New_Refresh_Token_When_The_Session_Was_Rotated()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);
        var signedIn = await SendAsync(new SignInCommand(Email, Password), cancellationToken);
        var renewed = await SendAsync(new RefreshSessionCommand(signedIn.RefreshToken), cancellationToken);

        // Act
        var tokens = await SendAsync(new RefreshSessionCommand(renewed.RefreshToken), cancellationToken);

        // Assert
        tokens.RefreshToken.ShouldNotBe(renewed.RefreshToken);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Reject_The_Previous_Refresh_Token_When_The_Session_Was_Rotated()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);
        var signedIn = await SendAsync(new SignInCommand(Email, Password), cancellationToken);
        await SendAsync(new RefreshSessionCommand(signedIn.RefreshToken), cancellationToken);

        // Act
        var act = () => SendAsync(new RefreshSessionCommand(signedIn.RefreshToken), cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_refresh_token");
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Reject_The_Refresh_Token_When_A_New_Login_Replaced_The_Session()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);
        var first = await SendAsync(new SignInCommand(Email, Password), cancellationToken);
        await SendAsync(new SignInCommand(Email, Password), cancellationToken);

        // Act
        var act = () => SendAsync(new RefreshSessionCommand(first.RefreshToken), cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_refresh_token");
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Reject_The_Refresh_Token_When_The_Account_Was_Removed()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);
        var signedIn = await SendAsync(new SignInCommand(Email, Password), cancellationToken);
        await ExecuteDbContextAsync(db => db.Set<AccountRecord>().Where(x => x.Id == accountId).ExecuteDeleteAsync(cancellationToken));

        // Act
        var act = () => SendAsync(new RefreshSessionCommand(signedIn.RefreshToken), cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_refresh_token");
    }

    private static string Sha256(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
