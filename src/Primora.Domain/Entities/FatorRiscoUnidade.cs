namespace Primora.Domain.Entities;

public sealed class FatorRiscoUnidade
{
    private FatorRiscoUnidade() { }

    public FatorRiscoUnidade(Guid versaoId, string unidadeId, int ordemOrigem, string descricao)
    {
        VersaoId = Validacao.Identificador(versaoId, nameof(versaoId));
        UnidadeId = Validacao.Texto(unidadeId, nameof(unidadeId));
        OrdemOrigem = Validacao.Ordem(ordemOrigem, nameof(ordemOrigem));
        Descricao = Validacao.Texto(descricao, nameof(descricao));
    }

    public Guid VersaoId { get; private set; }
    public string UnidadeId { get; private set; } = string.Empty;
    public int OrdemOrigem { get; private set; }
    public string Descricao { get; private set; } = string.Empty;

    public Unidade Unidade { get; private set; } = null!;
}
