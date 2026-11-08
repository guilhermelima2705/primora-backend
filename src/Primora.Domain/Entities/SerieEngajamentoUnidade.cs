namespace Primora.Domain.Entities;

public sealed class SerieEngajamentoUnidade
{
    private SerieEngajamentoUnidade() { }

    public SerieEngajamentoUnidade(Guid versaoId, string unidadeId, int posicao, int valor)
    {
        VersaoId = Validacao.Identificador(versaoId, nameof(versaoId));
        UnidadeId = Validacao.Texto(unidadeId, nameof(unidadeId));
        Posicao = Validacao.Posicao(posicao, nameof(posicao));
        Valor = Validacao.NaoNegativo(valor, nameof(valor));
    }

    public Guid VersaoId { get; private set; }
    public string UnidadeId { get; private set; } = string.Empty;
    public int Posicao { get; private set; }
    public int Valor { get; private set; }

    public Unidade Unidade { get; private set; } = null!;
}
