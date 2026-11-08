using Primora.Domain.Entities;

namespace Primora.Application.Importacao;

public sealed record DatasetPreparado(VersaoDataset Versao, IReadOnlyList<Unidade> Unidades,
    IReadOnlyList<IndicadoresUnidade> Indicadores, IReadOnlyList<SerieEngajamentoUnidade> Engajamento,
    IReadOnlyList<SerieFunilUnidade> Funil, IReadOnlyList<FatorRiscoUnidade> Fatores,
    IReadOnlyList<Cliente> Clientes, IReadOnlyList<HistoricoSaudeCliente> Scores, IReadOnlyList<EventoCliente> Eventos)
{
    public ContagensDataset Contagens => new(1, Unidades.Count, Indicadores.Count, Clientes.Count,
        Scores.Count, Eventos.Count, Engajamento.Count, Funil.Count, Fatores.Count);

    public IEnumerable<object> EntidadesDependentes => Unidades.Cast<object>().Concat(Indicadores)
        .Concat(Engajamento).Concat(Funil).Concat(Fatores).Concat(Clientes).Concat(Scores).Concat(Eventos);
}
