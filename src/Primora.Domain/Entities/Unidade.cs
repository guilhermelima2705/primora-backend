namespace Primora.Domain.Entities;

public sealed class Unidade
{
    private Unidade() { }

    public Unidade(Guid versaoId, string id, string nome, string porte, int ordemOrigem)
    {
        VersaoId = Validacao.Identificador(versaoId, nameof(versaoId));
        Id = Validacao.Texto(id, nameof(id));
        Nome = Validacao.Texto(nome, nameof(nome));
        Porte = Validacao.Texto(porte, nameof(porte));
        OrdemOrigem = Validacao.Ordem(ordemOrigem, nameof(ordemOrigem));
    }

    public Guid VersaoId { get; private set; }
    public string Id { get; private set; } = string.Empty;
    public string Nome { get; private set; } = string.Empty;
    public string Porte { get; private set; } = string.Empty;
    public int OrdemOrigem { get; private set; }

    public VersaoDataset Versao { get; private set; } = null!;
    public IndicadoresUnidade? Indicadores { get; private set; }
    public ICollection<SerieEngajamentoUnidade> SerieEngajamento { get; } = new List<SerieEngajamentoUnidade>();
    public ICollection<SerieFunilUnidade> SerieFunil { get; } = new List<SerieFunilUnidade>();
    public ICollection<FatorRiscoUnidade> FatoresRisco { get; } = new List<FatorRiscoUnidade>();
    public ICollection<Cliente> Clientes { get; } = new List<Cliente>();
}
