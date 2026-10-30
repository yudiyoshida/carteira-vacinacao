using MediatR;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.UseCases.VerifyAccountCredentials;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Authentication.Application.Security;
using VaccinationCard.Api.Modules.Authentication.Application.Services;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Modules.Authentication.Application.UseCases.SignIn;

public sealed class SignInHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ISessionDao _sessionDao = Substitute.For<ISessionDao>();
    private readonly SignInHandler _sut;

    public SignInHandlerTests() => _sut = new SignInHandler(_sender, new RefreshTokenSession(_tokenService, _sessionDao));

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Open_The_Session_Of_The_Account_When_Credentials_Are_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSignInInput();
        var account = new AccountDto(Guid.CreateVersion7(), input.Email);
        var expiresAt = new DateTimeOffset(2025, 6, 22, 12, 0, 0, TimeSpan.Zero);
        _sender.Send(new VerifyAccountCredentialsQuery(input.Email, input.Password), Arg.Any<CancellationToken>()).Returns(account);
        _tokenService.IssueAccessToken(account.Id).Returns("access-token");
        _tokenService.IssueRefreshToken(account.Id).Returns(new RefreshTokenDto("refresh-token", expiresAt));

        // Act
        var tokens = await _sut.Handle(input, cancellationToken);

        // Assert
        tokens.ShouldBe(new SessionTokensDto("access-token", "refresh-token", expiresAt));
        await _sessionDao.Received(1).SaveAsync(Arg.Is<SessionDto>(session => session.AccountId == account.Id), cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN22")]
    public async Task Should_Reject_Login_Without_Opening_A_Session_When_Credentials_Are_Invalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSignInInput();
        _sender.Send(new VerifyAccountCredentialsQuery(input.Email, input.Password), Arg.Any<CancellationToken>()).Returns((AccountDto?)null);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_credentials");
        await _sessionDao.DidNotReceive().SaveAsync(Arg.Any<SessionDto>(), Arg.Any<CancellationToken>());
    }

    private static SignInCommand MakeSignInInput() => new("operador@demo.local", "Demo@2026!");
}
