using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace VaccinationCard.IntegrationTests;

public abstract class HttpTest(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    protected HttpClient Client { get; } = fixture.Factory.CreateAuthenticatedClient();

    // Each test calls from an IP of its own, so the login attempts of one test do not count against another (RN24).
    protected HttpClient AnonymousClient { get; } = fixture.Factory.CreateClientFromIp(new IPAddress(Guid.CreateVersion7().ToByteArray()));

    public override ValueTask DisposeAsync()
    {
        Client.Dispose();
        AnonymousClient.Dispose();
        return base.DisposeAsync();
    }

    // Every error answers a ProblemDetails as application/problem+json (TechSpec §8.1).
    protected static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }
}
