using Microsoft.AspNetCore.Mvc;
using Primora.Api.Contracts;
using Primora.Application.Health;

namespace Primora.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(IApplicationHealthService health) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        var report = await health.CheckAsync(cancellationToken);
        if (!report.DatabaseAvailable)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Serviço indisponível",
                detail: "O banco de dados não está disponível.");

        return Ok(new HealthResponse("healthy", "available", "available"));
    }
}
