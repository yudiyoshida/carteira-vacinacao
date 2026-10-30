using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Authentication.Application.Security;
using VaccinationCard.Api.Modules.Authentication.Application.Services;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Modules.Authentication.Application.Services;

public sealed class RefreshTokenSessionTests
{
    private const string RefreshToken = "refresh-token";

    // The SHA-256 of RefreshToken, in hexadecimal.
    private const string RefreshTokenHash = "0eb17643d4e9261163783a420859c92c7d212fa9624106a12b510afbec266120";

    private static readonly DateTimeOffset ExpiresAt = new(2025, 6, 22, 12, 0, 0, TimeSpan.Zero);

    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ISessionDao _dao = Substitute.For<ISessionDao>();
    private readonly RefreshTokenSession _sut;

    public RefreshTokenSessionTests() => _sut = new RefreshTokenSession(_tokenService, _dao);

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Return_Both_Tokens_When_Issuing_A_Session()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = Guid.CreateVersion7();
        ArrangeIssuedTokens(accountId);

        // Act
        var tokens = await _sut.IssueAsync(accountId, cancellationToken);

        // Assert
        tokens.ShouldBe(new SessionTokensDto("access-token", RefreshToken, ExpiresAt));
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Store_Only_The_Hash_Of_The_Refresh_Token_When_Issuing_A_Session()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = Guid.CreateVersion7();
        ArrangeIssuedTokens(accountId);

        // Act
        await _sut.IssueAsync(accountId, cancellationToken);

        // Assert
        await _dao.Received(1).SaveAsync(new SessionDto(accountId, RefreshTokenHash, ExpiresAt), cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Return_The_Account_When_Refresh_Token_Is_The_One_Of_The_Session()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = Guid.CreateVersion7();
        _tokenService.ValidateRefreshTokenAsync(RefreshToken).Returns(accountId);
        _dao.FindByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(new SessionDto(accountId, RefreshTokenHash, ExpiresAt));

        // Act
        var validated = await _sut.ValidateAsync(RefreshToken, cancellationToken);

        // Assert
        validated.ShouldBe(accountId);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Reject_Refresh_Token_Without_Reading_The_Session_When_The_Token_Is_Invalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        _tokenService.ValidateRefreshTokenAsync(RefreshToken).Returns((Guid?)null);

        // Act
        var act = () => _sut.ValidateAsync(RefreshToken, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_refresh_token");
        await _dao.DidNotReceive().FindByAccountIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // After a rotation or a new login, the session holds the hash of another token.
    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Reject_Refresh_Token_When_The_Session_Holds_Another_Hash()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = Guid.CreateVersion7();
        _tokenService.ValidateRefreshTokenAsync(RefreshToken).Returns(accountId);
        _dao.FindByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(new SessionDto(accountId, new string('0', 64), ExpiresAt));

        // Act
        var act = () => _sut.ValidateAsync(RefreshToken, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_refresh_token");
    }

    // After a logout, the account has no session.
    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Reject_Refresh_Token_When_The_Account_Has_No_Session()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = Guid.CreateVersion7();
        _tokenService.ValidateRefreshTokenAsync(RefreshToken).Returns(accountId);
        _dao.FindByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns((SessionDto?)null);

        // Act
        var act = () => _sut.ValidateAsync(RefreshToken, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<UnauthorizedException>(act);
        exception.Code.ShouldBe("auth.invalid_refresh_token");
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Delete_The_Session_When_Revoking()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = Guid.CreateVersion7();

        // Act
        await _sut.RevokeAsync(accountId, cancellationToken);

        // Assert
        await _dao.Received(1).DeleteAsync(accountId, cancellationToken);
    }

    private void ArrangeIssuedTokens(Guid accountId)
    {
        _tokenService.IssueAccessToken(accountId).Returns("access-token");
        _tokenService.IssueRefreshToken(accountId).Returns(new RefreshTokenDto(RefreshToken, ExpiresAt));
    }
}
