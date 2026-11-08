namespace Primora.Domain.Entities;

public sealed class SerieFunilUnidade
{
    private SerieFunilUnidade() { }

    public SerieFunilUnidade(Guid versaoId, string unidadeId, int posicao, decimal valor)
    {
        VersaoId = Validacao.Identificador(versaoId, nameof(versaoId));
        UnidadeId = Validacao.Texto(unidadeId, nameof(unidadeId));
        Posicao = Validacao.Posicao(posicao, nameof(posicao));
        Valor = Validacao.Proporcao(valor, nameof(valor));
    }

    public Guid VersaoId { get; private set; }
    public string UnidadeId { get; private set; } = string.Empty;
    public int Posicao { get; private set; }
    public decimal Valor { get; private set; }

    public Unidade Unidade { get; private set; } = null!;
}
