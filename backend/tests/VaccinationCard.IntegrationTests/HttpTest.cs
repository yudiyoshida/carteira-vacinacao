namespace VaccinationCard.IntegrationTests;

public abstract class HttpTest(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    protected HttpClient Client { get; } = fixture.Factory.CreateClient();

    public override ValueTask DisposeAsync()
    {
        Client.Dispose();
        return base.DisposeAsync();
    }
}
