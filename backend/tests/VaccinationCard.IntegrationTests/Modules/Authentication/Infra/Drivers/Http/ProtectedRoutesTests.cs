using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using VaccinationCard.Api.Modules.Authentication.Application.Security;

namespace VaccinationCard.IntegrationTests.Modules.Authentication.Infra.Drivers.Http;

public sealed partial class ProtectedRoutesTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string ProtectedUrl = "/api/v1/vaccines";

    // The only routes without the access token (RN21, TechSpec §7.1). Scalar stands for the page and its own files.
    private static readonly string[] AnonymousRoutes =
    [
        "api/v1/auth/login",
        "api/v1/auth/refresh",
        "health",
        "openapi/{documentName}.json",
        "scalar",
    ];

    [Fact]
    [Trait("RN", "RN21")]
    public void Should_Allow_Anonymous_Access_Only_To_The_Public_Routes_When_Listing_The_Routes()
    {
        // Act
        var anonymous = RouteEndpoints()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => Path(endpoint))
            .Select(path => path.StartsWith("scalar/", StringComparison.Ordinal) ? "scalar" : path)
            .Distinct();

        // Assert
        anonymous.ShouldBe(AnonymousRoutes, ignoreOrder: true);
    }

    // The routes come from the API itself, so a new route enters this check without a change here.
    [Fact]
    [Trait("RN", "RN21")]
    public async Task Should_Return_Unauthorized_When_Calling_Any_Other_Route_Without_A_Token()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var requests = RouteEndpoints()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .SelectMany(endpoint => endpoint.Metadata.GetRequiredMetadata<IHttpMethodMetadata>().HttpMethods
                .Select(method => (Method: method, Url: $"/{RouteParameter().Replace(Path(endpoint), Guid.CreateVersion7().ToString())}")))
            .ToList();
        requests.ShouldNotBeEmpty();

        foreach (var (method, url) in requests)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), url);

            // Act
            var response = await AnonymousClient.SendAsync(request, cancellationToken);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{method} {url}");
            var problem = await ReadProblemAsync(response, cancellationToken);
            problem.GetProperty("code").GetString().ShouldBe("auth.unauthenticated", $"{method} {url}");
        }
    }

    [Fact]
    [Trait("RN", "RN21")]
    public async Task Should_Answer_The_Challenge_Format_When_There_Is_No_Token()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await AnonymousClient.GetAsync(ProtectedUrl, cancellationToken);

        // Assert
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("title").GetString().ShouldBe("Não autorizado");
        problem.GetProperty("detail").GetString().ShouldBe("Autenticação necessária.");
        problem.GetProperty("instance").GetString().ShouldBe(ProtectedUrl);
        response.Headers.WwwAuthenticate.ToString().ShouldBe("Bearer");
        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
    }

    [Fact]
    [Trait("RN", "RN21")]
    public async Task Should_Accept_Request_When_Access_Token_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync(ProtectedUrl, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("RN", "RN21")]
    public async Task Should_Return_Unauthorized_When_Token_Is_Malformed()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await SendWithTokenAsync("not-a-token", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("auth.unauthenticated");
    }

    // The refresh token has a secret of its own, so it does not open the routes (ADR-0007).
    [Fact]
    [Trait("RN", "RN21")]
    public async Task Should_Return_Unauthorized_When_Token_Is_A_Refresh_Token()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var refreshToken = Fixture.Factory.Services.GetRequiredService<ITokenService>().IssueRefreshToken(Guid.CreateVersion7());

        // Act
        var response = await SendWithTokenAsync(refreshToken.Token, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // The fallback policy also covers a request without a route, so an anonymous client does not discover the routes.
    [Theory]
    [Trait("RN", "RN21")]
    [InlineData("GET", "/api/v1/does-not-exist")]
    [InlineData("PUT", "/api/v1/vaccines")]
    public async Task Should_Return_Unauthorized_When_An_Anonymous_Client_Asks_For_Something_Without_A_Route(string method, string url)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        // Act
        var response = await AnonymousClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("auth.unauthenticated");
    }

    [Theory]
    [InlineData("GET", "/api/v1/does-not-exist", HttpStatusCode.NotFound, "route.not_found")]
    [InlineData("PUT", "/api/v1/vaccines", HttpStatusCode.MethodNotAllowed, "route.method_not_allowed")]
    public async Task Should_Answer_The_Route_Error_When_The_Client_Has_A_Token(string method, string url, HttpStatusCode status, string code)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        // Act
        var response = await Client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(status);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe(code);
    }

    // 15 minutes of validity (RN23) and 30 seconds of clock skew (TechSpec §9).
    [Theory]
    [Trait("RN", "RN23")]
    [InlineData(0, HttpStatusCode.OK)]
    [InlineData(930, HttpStatusCode.OK)]
    [InlineData(931, HttpStatusCode.Unauthorized)]
    public async Task Should_Apply_The_Validity_Of_The_Access_Token_When_Time_Passes(int seconds, HttpStatusCode status)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(ApiFactory.Today, new TimeOnly(12, 0), TimeSpan.Zero));
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(timeProvider)));
        using var client = factory.CreateAuthenticatedClient();
        timeProvider.Advance(TimeSpan.FromSeconds(seconds));

        // Act
        var response = await client.GetAsync(ProtectedUrl, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task Should_Refuse_To_Start_When_A_Secret_Is_Too_Short()
    {
        // Arrange
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:AccessTokenSecret", "short"));

        // Act
        var act = () => factory.CreateClient();

        // Assert
        Should.Throw<OptionsValidationException>(act);
    }

    private IEnumerable<RouteEndpoint> RouteEndpoints() =>
        Fixture.Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

    // Only the routes of the controllers come without the slash at the start.
    private static string Path(RouteEndpoint endpoint) => endpoint.RoutePattern.RawText!.TrimStart('/');

    private async Task<HttpResponseMessage> SendWithTokenAsync(string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ProtectedUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await AnonymousClient.SendAsync(request, cancellationToken);
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteParameter();
}
