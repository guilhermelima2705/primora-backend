namespace Primora.Domain.Entities;

public sealed class HistoricoSaudeCliente
{
    private HistoricoSaudeCliente() { }

    public HistoricoSaudeCliente(Guid versaoId, string unidadeId, string clienteId, DateOnly data, int score)
    {
        VersaoId = Validacao.Identificador(versaoId, nameof(versaoId));
        UnidadeId = Validacao.Texto(unidadeId, nameof(unidadeId));
        ClienteId = Validacao.Texto(clienteId, nameof(clienteId));
        Data = data;
        Score = Validacao.Score(score, nameof(score));
    }

    public Guid VersaoId { get; private set; }
    public string UnidadeId { get; private set; } = string.Empty;
    public string ClienteId { get; private set; } = string.Empty;
    public DateOnly Data { get; private set; }
    public int Score { get; private set; }

    public Cliente Cliente { get; private set; } = null!;
}
