using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;
using VaccinationCard.Api.Modules.Authentication.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.Authentication.Application.UseCases.SignIn;

public sealed class SignInHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    private const string Email = "operador@demo.local";
    private const string Password = "Demo@2026!";

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Store_Only_The_Hash_Of_The_Refresh_Token_When_Signing_In()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var tokens = await SendAsync(new SignInCommand(Email, Password), cancellationToken);

        // Assert
        var session = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().SingleAsync(cancellationToken));
        session.AccountId.ShouldBe(accountId);
        session.RefreshTokenHash.ShouldBe(Sha256(tokens.RefreshToken));
        session.ExpiresAt.ShouldBe(tokens.RefreshTokenExpiresAt);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Replace_The_Previous_Session_When_Signing_In_Again()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);
        var first = await SendAsync(new SignInCommand(Email, Password), cancellationToken);

        // Act
        var second = await SendAsync(new SignInCommand(Email, Password), cancellationToken);

        // Assert
        var session = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().SingleAsync(cancellationToken));
        session.AccountId.ShouldBe(accountId);
        session.RefreshTokenHash.ShouldBe(Sha256(second.RefreshToken));
        session.RefreshTokenHash.ShouldNotBe(Sha256(first.RefreshToken));
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Keep_The_Session_Of_Another_Account_When_Signing_In()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);
        await CreateAccountAsync("outro@demo.local", Password, cancellationToken);
        await SendAsync(new SignInCommand("outro@demo.local", Password), cancellationToken);

        // Act
        await SendAsync(new SignInCommand(Email, Password), cancellationToken);

        // Assert
        var sessions = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().CountAsync(cancellationToken));
        sessions.ShouldBe(2);
    }

    [Theory]
    [Trait("RN", "RN22")]
    [InlineData(Email, "Senha@Errada1")]
    [InlineData("outro@demo.local", Password)]
    public async Task Should_Throw_Unauthorized_Without_A_Session_When_Credentials_Are_Invalid(string email, string password)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var act = () => SendAsync(new SignInCommand(email, password), cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_credentials");
        var sessions = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().CountAsync(cancellationToken));
        sessions.ShouldBe(0);
    }

    private static string Sha256(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
