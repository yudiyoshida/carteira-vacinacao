using MediatR;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.UseCases.FindAccountById;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Authentication.Application.Security;
using VaccinationCard.Api.Modules.Authentication.Application.Services;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.RefreshSession;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Modules.Authentication.Application.UseCases.RefreshSession;

public sealed class RefreshSessionHandlerTests
{
    private const string RefreshToken = "refresh-token";
    private const string NewRefreshToken = "new-refresh-token";

    // The SHA-256 of each token above, in hexadecimal.
    private const string RefreshTokenHash = "0eb17643d4e9261163783a420859c92c7d212fa9624106a12b510afbec266120";
    private const string NewRefreshTokenHash = "c40dd1765d767caae2588f0ee1de9181d8a44cc9306261eb2c9e526351188338";

    private static readonly DateTimeOffset ExpiresAt = new(2025, 6, 22, 12, 0, 0, TimeSpan.Zero);

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ISessionDao _sessionDao = Substitute.For<ISessionDao>();
    private readonly RefreshSessionHandler _sut;

    public RefreshSessionHandlerTests() => _sut = new RefreshSessionHandler(_sender, new RefreshTokenSession(_tokenService, _sessionDao));

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Issue_A_New_Pair_And_Replace_The_Hash_When_Refresh_Token_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeRefreshSessionInput();
        var accountId = Guid.CreateVersion7();
        ArrangeValidRefreshToken(accountId);
        _sender.Send(new FindAccountByIdQuery(accountId), Arg.Any<CancellationToken>()).Returns(new AccountDto(accountId, "operador@demo.local"));
        _tokenService.IssueAccessToken(accountId).Returns("new-access-token");
        _tokenService.IssueRefreshToken(accountId).Returns(new RefreshTokenDto(NewRefreshToken, ExpiresAt));

        // Act
        var tokens = await _sut.Handle(input, cancellationToken);

        // Assert
        tokens.ShouldBe(new SessionTokensDto("new-access-token", NewRefreshToken, ExpiresAt));
        await _sessionDao.Received(1).SaveAsync(new SessionDto(accountId, NewRefreshTokenHash, ExpiresAt), cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Reject_Renewal_Without_Replacing_The_Session_When_Refresh_Token_Is_Invalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeRefreshSessionInput();
        _tokenService.ValidateRefreshTokenAsync(RefreshToken).Returns((Guid?)null);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_refresh_token");
        await _sessionDao.DidNotReceive().SaveAsync(Arg.Any<SessionDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Reject_Renewal_Without_Replacing_The_Session_When_The_Account_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeRefreshSessionInput();
        var accountId = Guid.CreateVersion7();
        ArrangeValidRefreshToken(accountId);
        _sender.Send(new FindAccountByIdQuery(accountId), Arg.Any<CancellationToken>()).Returns((AccountDto?)null);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_refresh_token");
        await _sessionDao.DidNotReceive().SaveAsync(Arg.Any<SessionDto>(), Arg.Any<CancellationToken>());
    }

    private void ArrangeValidRefreshToken(Guid accountId)
    {
        _tokenService.ValidateRefreshTokenAsync(RefreshToken).Returns(accountId);
        _sessionDao.FindByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(new SessionDto(accountId, RefreshTokenHash, ExpiresAt));
    }

    private static RefreshSessionCommand MakeRefreshSessionInput() => new(RefreshToken);
}
