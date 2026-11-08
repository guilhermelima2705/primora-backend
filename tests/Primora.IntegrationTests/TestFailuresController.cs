using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Primora.IntegrationTests;

// Registrado somente pelo host de testes; não faz parte da API publicada.
[ApiController]
[Route("__tests")]
public sealed class TestFailuresController : ControllerBase
{
    [HttpGet("failure")]
    public IActionResult Failure() => throw new InvalidOperationException("sensitive-test-marker");

    [HttpGet("bad-request")]
    public IActionResult BadRequestFailure() => throw new BadHttpRequestException("sensitive-test-marker");
}
