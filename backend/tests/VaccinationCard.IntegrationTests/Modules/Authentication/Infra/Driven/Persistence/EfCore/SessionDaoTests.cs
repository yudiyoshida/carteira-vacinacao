using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.Accounts.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Infra.Driven.Persistence.EfCore;

namespace VaccinationCard.IntegrationTests.Modules.Authentication.Infra.Driven.Persistence.EfCore;

public sealed class SessionDaoTests : IntegrationTest
{
    private static readonly DateTimeOffset ExpiresAt = new(2025, 6, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly string FirstHash = new('a', 64);
    private static readonly string SecondHash = new('b', 64);

    private readonly AsyncServiceScope _scope;
    private readonly SessionDao _sut;

    public SessionDaoTests(IntegrationFixture fixture) : base(fixture)
    {
        _scope = fixture.Factory.Services.CreateAsyncScope();
        _sut = new SessionDao(_scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Should_Store_Session_When_Account_Has_None()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(cancellationToken);

        // Act
        await _sut.SaveAsync(new SessionDto(accountId, FirstHash, ExpiresAt), cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().SingleAsync(cancellationToken));
        record.AccountId.ShouldBe(accountId);
        record.RefreshTokenHash.ShouldBe(FirstHash);
        record.ExpiresAt.ShouldBe(ExpiresAt);
        record.UpdatedAt.ShouldNotBe(default);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Replace_Session_When_Account_Already_Has_One()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(cancellationToken);
        await _sut.SaveAsync(new SessionDto(accountId, FirstHash, ExpiresAt), cancellationToken);
        var laterExpiration = ExpiresAt.AddDays(1);

        // Act
        await _sut.SaveAsync(new SessionDto(accountId, SecondHash, laterExpiration), cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().SingleAsync(cancellationToken));
        record.RefreshTokenHash.ShouldBe(SecondHash);
        record.ExpiresAt.ShouldBe(laterExpiration);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_End_With_One_Session_When_Two_Saves_Run_At_The_Same_Time()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(cancellationToken);
        await using var otherScope = Fixture.Factory.Services.CreateAsyncScope();
        var other = new SessionDao(otherScope.ServiceProvider.GetRequiredService<AppDbContext>());

        // Act
        await Task.WhenAll(
            _sut.SaveAsync(new SessionDto(accountId, FirstHash, ExpiresAt), cancellationToken),
            other.SaveAsync(new SessionDto(accountId, SecondHash, ExpiresAt), cancellationToken));

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<SessionRecord>().SingleAsync(cancellationToken));
        record.RefreshTokenHash.ShouldBeOneOf(FirstHash, SecondHash);
    }

    [Fact]
    public async Task Should_Return_Session_When_Account_Has_One()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new SessionDto(await CreateAccountAsync(cancellationToken), FirstHash, ExpiresAt);
        await _sut.SaveAsync(input, cancellationToken);

        // Act
        var session = await _sut.FindByAccountIdAsync(input.AccountId, cancellationToken);

        // Assert
        session.ShouldBe(input);
    }

    [Fact]
    public async Task Should_Return_Null_When_Account_Has_No_Session()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(cancellationToken);

        // Act
        var session = await _sut.FindByAccountIdAsync(accountId, cancellationToken);

        // Assert
        session.ShouldBeNull();
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Remove_Session_When_Deleting()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(cancellationToken);
        await _sut.SaveAsync(new SessionDto(accountId, FirstHash, ExpiresAt), cancellationToken);

        // Act
        await _sut.DeleteAsync(accountId, cancellationToken);

        // Assert
        var session = await _sut.FindByAccountIdAsync(accountId, cancellationToken);
        session.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Remove_Session_When_Its_Account_Is_Removed()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync(cancellationToken);
        await _sut.SaveAsync(new SessionDto(accountId, FirstHash, ExpiresAt), cancellationToken);

        // Act
        await ExecuteDbContextAsync(db => db.Set<AccountRecord>().Where(x => x.Id == accountId).ExecuteDeleteAsync(cancellationToken));

        // Assert
        var session = await _sut.FindByAccountIdAsync(accountId, cancellationToken);
        session.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Violate_The_Foreign_Key_When_Account_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new SessionDto(Guid.CreateVersion7(), FirstHash, ExpiresAt);

        // Act
        var act = () => _sut.SaveAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<PostgresException>(act);
        exception.SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
    }

    private Task<Guid> CreateAccountAsync(CancellationToken cancellationToken) =>
        CreateAccountAsync("operador@demo.local", "Demo@2026!", cancellationToken);
}
