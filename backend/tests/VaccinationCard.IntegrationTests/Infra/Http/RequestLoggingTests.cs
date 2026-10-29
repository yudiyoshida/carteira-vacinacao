using VaccinationCard.Api.Infra.Http;

namespace VaccinationCard.IntegrationTests.Infra.Http;

public sealed class RequestLoggingTests(IntegrationFixture fixture) : PipelineTest(fixture)
{
    [Fact]
    [Trait("RN", "RN25")]
    public async Task Should_Log_Request_Without_Query_String_When_Url_Has_A_Search_Term()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await Client.GetAsync("/health?search=Maria", cancellationToken);

        // Assert
        var records = Logs.GetSnapshot();
        var request = records.Single(record => record.Category == typeof(RequestLoggingMiddleware).FullName);
        request.GetStructuredStateValue("Method").ShouldBe("GET");
        request.GetStructuredStateValue("Path").ShouldBe("/health");
        request.GetStructuredStateValue("StatusCode").ShouldBe("200");
        request.GetStructuredStateValue("ElapsedMilliseconds").ShouldNotBeNull();
        records.ShouldNotContain(record => record.Message.Contains("Maria") || record.Message.Contains("search="));
    }

    [Fact]
    public async Task Should_Log_The_Error_Status_When_Request_Fails()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await Client.GetAsync("/test/unexpected", cancellationToken);

        // Assert
        var request = Logs.GetSnapshot().Single(record => record.Category == typeof(RequestLoggingMiddleware).FullName);
        request.GetStructuredStateValue("Path").ShouldBe("/test/unexpected");
        request.GetStructuredStateValue("StatusCode").ShouldBe("500");
    }
}
