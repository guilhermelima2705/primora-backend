namespace Primora.Application.Dataset;

public interface IDatasetConsultas
{
    Task<ListagemDataset<UnidadeDto>> ListarUnidadesAsync(CancellationToken cancellationToken);
    Task<ListagemDataset<ClienteDto>> ListarClientesAsync(CancellationToken cancellationToken);
}

public sealed record ListagemDataset<T>(DateOnly? GeradoEm, IReadOnlyList<T> Itens);
