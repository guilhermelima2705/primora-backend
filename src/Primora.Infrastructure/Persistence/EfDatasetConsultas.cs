using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Primora.Application.Dataset;

namespace Primora.Infrastructure.Persistence;

internal sealed class EfDatasetConsultas(PrimoraDbContext database,
    IOptions<DatasetPublicacaoOptions> publicacao) : IDatasetConsultas
{
    public async Task<ListagemDataset<UnidadeDto>> ListarUnidadesAsync(CancellationToken cancellationToken)
    {
        var versao = await BuscarVersaoAsync(cancellationToken);
        if (versao is null) return new(null, []);

        var unidades = await database.Unidades.AsNoTracking().AsSplitQuery()
            .Where(unidade => unidade.VersaoId == versao.Id)
            .OrderBy(unidade => unidade.OrdemOrigem).ThenBy(unidade => unidade.Id)
            .Select(unidade => new UnidadeDto(unidade.Id, unidade.Nome, unidade.Porte,
                unidade.Indicadores!.CarteiraTotalClientes, unidade.Indicadores.ReceitaCarteira90d,
                unidade.Indicadores.EventosEngajamento7d, unidade.Indicadores.UsuariosAtivos7d,
                unidade.SerieEngajamento.OrderBy(ponto => ponto.Posicao).Select(ponto => ponto.Valor).ToList(),
                new FunilDto(unidade.Indicadores.FunilGerada90d, unidade.Indicadores.FunilVista90d,
                    unidade.Indicadores.FunilSucesso90d, unidade.Indicadores.FunilFalha90d),
                unidade.SerieFunil.OrderBy(ponto => ponto.Posicao).Select(ponto => ponto.Valor).ToList(),
                new DistribuicaoSaudeDto(unidade.Indicadores.Saudavel, unidade.Indicadores.Atencao,
                    unidade.Indicadores.Risco, unidade.Indicadores.Critico),
                unidade.Indicadores.RiscoAbsoluto,
                unidade.FatoresRisco.OrderBy(fator => fator.OrdemOrigem).Select(fator => fator.Descricao).ToList()))
            .ToListAsync(cancellationToken);

        return new(versao.GeradoEm, unidades);
    }

    public async Task<ListagemDataset<ClienteDto>> ListarClientesAsync(CancellationToken cancellationToken)
    {
        var versao = await BuscarVersaoAsync(cancellationToken);
        if (versao is null) return new(null, []);

        var clientes = await database.Clientes.AsNoTracking().AsSplitQuery()
            .Where(cliente => cliente.VersaoId == versao.Id)
            .OrderBy(cliente => cliente.OrdemOrigem).ThenBy(cliente => cliente.UnidadeId).ThenBy(cliente => cliente.Id)
            .Select(cliente => new ClienteDto(cliente.Id, cliente.Nome, cliente.UnidadeId, cliente.Unidade.Nome,
                cliente.HistoricoSaude.OrderBy(registro => registro.Data)
                    .Select(registro => new RegistroSaudeDto(registro.Data, registro.Score)).ToList(),
                cliente.Eventos.OrderBy(evento => evento.OrdemOrigem)
                    .Select(evento => new EventoDto(evento.Data, evento.Tipo, evento.Titulo, evento.Valor)).ToList()))
            .ToListAsync(cancellationToken);

        return new(versao.GeradoEm, clientes);
    }

    private Task<VersaoPublicada?> BuscarVersaoAsync(CancellationToken cancellationToken) =>
        database.VersoesDataset.AsNoTracking()
            .Where(versao => versao.HashSha256 == publicacao.Value.HashSha256)
            .Select(versao => new VersaoPublicada(versao.Id, versao.GeradoEm))
            .SingleOrDefaultAsync(cancellationToken);

    private sealed record VersaoPublicada(Guid Id, DateOnly GeradoEm);
}
