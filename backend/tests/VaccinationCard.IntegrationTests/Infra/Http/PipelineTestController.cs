using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace VaccinationCard.IntegrationTests.Infra.Http;

// Routes that exist only in the tests of the HTTP pipeline, registered by PipelineTest.
[ApiController]
[Route("test")]
public sealed class PipelineTestController(ISender sender) : ControllerBase
{
    public const string Cpf = "123.456.789-09";

    [HttpGet("unexpected")]
    public IActionResult Unexpected() => throw new InvalidOperationException($"Falha ao gravar o CPF {Cpf}.");

    [HttpPost("echo")]
    public IActionResult Echo(EchoBody echo) => Ok(echo);

    [HttpPost("validated")]
    public async Task<IActionResult> Validated(SampleCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    public sealed record EchoBody(string Name, int Number);

    public sealed record SampleCommand(string Name) : IRequest<string>;

    public sealed class SampleCommandValidator : AbstractValidator<SampleCommand>
    {
        public SampleCommandValidator() =>
            RuleFor(command => command.Name).NotEmpty().WithMessage("O nome é obrigatório.");
    }

    public sealed class SampleCommandHandler : IRequestHandler<SampleCommand, string>
    {
        public Task<string> Handle(SampleCommand request, CancellationToken cancellationToken) =>
            Task.FromResult(request.Name);
    }
}
