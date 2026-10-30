using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.RefreshSession;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignOut;
using VaccinationCard.Api.Modules.Authentication.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.Authentication.Application.UseCases.SignOut;

public sealed class SignOutHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    private const string Email = "operador@demo.local";
    private const string Password = "Demo@2026!";

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Delete_The_Session_When_Signing_Out()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);
        await SendAsync(new SignInCommand(Email, Password), cancellationToken);

        // Act
        var output = await SendAsync(new SignOutCommand(accountId), cancellationToken);

        // Assert
        output.ShouldBe(new SuccessMessage("Sessão encerrada com sucesso."));
        var sessions = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().CountAsync(cancellationToken));
        sessions.ShouldBe(0);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Reject_The_Refresh_Token_When_The_Account_Signed_Out()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);
        var signedIn = await SendAsync(new SignInCommand(Email, Password), cancellationToken);
        await SendAsync(new SignOutCommand(accountId), cancellationToken);

        // Act
        var act = () => SendAsync(new RefreshSessionCommand(signedIn.RefreshToken), cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_refresh_token");
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Keep_The_Session_Of_Another_Account_When_Signing_Out()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);
        var otherAccountId = await CreateAccountAsync("outro@demo.local", Password, cancellationToken);
        await SendAsync(new SignInCommand(Email, Password), cancellationToken);
        await SendAsync(new SignInCommand("outro@demo.local", Password), cancellationToken);

        // Act
        await SendAsync(new SignOutCommand(accountId), cancellationToken);

        // Assert
        var session = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().SingleAsync(cancellationToken));
        session.AccountId.ShouldBe(otherAccountId);
    }

    // The access token stays valid after a logout, so a second logout finds nothing to delete.
    [Fact]
    public async Task Should_Return_Success_Message_When_The_Account_Has_No_Session()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var output = await SendAsync(new SignOutCommand(accountId), cancellationToken);

        // Assert
        output.ShouldBe(new SuccessMessage("Sessão encerrada com sucesso."));
    }
}
