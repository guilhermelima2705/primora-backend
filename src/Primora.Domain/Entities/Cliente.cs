namespace Primora.Domain.Entities;

public sealed class Cliente
{
    private Cliente() { }

    public Cliente(Guid versaoId, string unidadeId, string id, string nome, int ordemOrigem)
    {
        VersaoId = Validacao.Identificador(versaoId, nameof(versaoId));
        UnidadeId = Validacao.Texto(unidadeId, nameof(unidadeId));
        Id = Validacao.Texto(id, nameof(id));
        Nome = Validacao.Texto(nome, nameof(nome));
        OrdemOrigem = Validacao.Ordem(ordemOrigem, nameof(ordemOrigem));
    }

    public Guid VersaoId { get; private set; }
    public string UnidadeId { get; private set; } = string.Empty;
    public string Id { get; private set; } = string.Empty;
    public string Nome { get; private set; } = string.Empty;
    public int OrdemOrigem { get; private set; }

    public Unidade Unidade { get; private set; } = null!;
    public ICollection<HistoricoSaudeCliente> HistoricoSaude { get; } = new List<HistoricoSaudeCliente>();
    public ICollection<EventoCliente> Eventos { get; } = new List<EventoCliente>();
}
