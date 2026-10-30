using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Accounts.Application.UseCases.FindAccountById;

namespace VaccinationCard.UnitTests.Modules.Accounts.Application.UseCases.FindAccountById;

public sealed class FindAccountByIdHandlerTests
{
    private readonly IAccountDao _dao = Substitute.For<IAccountDao>();
    private readonly FindAccountByIdHandler _sut;

    public FindAccountByIdHandlerTests() => _sut = new FindAccountByIdHandler(_dao);

    [Fact]
    public async Task Should_Return_Account_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = new AccountDto(Guid.CreateVersion7(), "operador@demo.local");
        _dao.FindByIdAsync(expected.Id, Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        var account = await _sut.Handle(new FindAccountByIdQuery(expected.Id), cancellationToken);

        // Assert
        account.ShouldBe(expected);
    }

    [Fact]
    public async Task Should_Return_Null_When_Account_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var account = await _sut.Handle(new FindAccountByIdQuery(Guid.CreateVersion7()), cancellationToken);

        // Assert
        account.ShouldBeNull();
    }
}
