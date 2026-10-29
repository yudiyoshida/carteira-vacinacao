using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace VaccinationCard.IntegrationTests.Infra.Http;

// The API plus the routes of PipelineTestController, with the logs collected. The tests read and write no data, so
// there is no database reset.
public abstract class PipelineTest : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    protected PipelineTest(IntegrationFixture fixture)
    {
        _factory = fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddFakeLogging());
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddApplicationPart(typeof(PipelineTestController).Assembly);
                services.AddTransient<IRequestHandler<PipelineTestController.SampleCommand, string>, PipelineTestController.SampleCommandHandler>();
                services.AddTransient<IValidator<PipelineTestController.SampleCommand>, PipelineTestController.SampleCommandValidator>();
            });
        });
        Client = _factory.CreateClient();
    }

    protected HttpClient Client { get; }

    protected FakeLogCollector Logs => _factory.Services.GetFakeLogCollector();

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
