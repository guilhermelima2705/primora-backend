namespace Primora.Application.Importacao;

public interface IDatasetImportador
{
    Task<ResultadoImportacao> ImportarAsync(ReadOnlyMemory<byte> conteudo, string nomeOrigem,
        CancellationToken cancellationToken = default);
}
