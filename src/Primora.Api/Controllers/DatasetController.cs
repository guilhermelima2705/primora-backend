using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Primora.Application.Dataset;

namespace Primora.Api.Controllers;

[ApiController]
public sealed class DatasetController(IDatasetConsultas consultas) : ControllerBase
{
    public const string GeradoEmHeader = "X-Dataset-Gerado-Em";

    [HttpGet("/unidades")]
    [ProducesResponseType<IReadOnlyList<UnidadeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<UnidadeDto>>> ListarUnidades(CancellationToken cancellationToken) =>
        Responder(await consultas.ListarUnidadesAsync(cancellationToken));

    [HttpGet("/clientes")]
    [ProducesResponseType<IReadOnlyList<ClienteDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ClienteDto>>> ListarClientes(CancellationToken cancellationToken) =>
        Responder(await consultas.ListarClientesAsync(cancellationToken));

    private OkObjectResult Responder<T>(ListagemDataset<T> listagem)
    {
        if (listagem.GeradoEm is { } geradoEm)
            Response.Headers[GeradoEmHeader] = geradoEm.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return Ok(listagem.Itens);
    }
}
