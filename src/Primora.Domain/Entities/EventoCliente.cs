namespace Primora.Domain.Entities;

public sealed class EventoCliente
{
    private EventoCliente() { }

    public EventoCliente(Guid versaoId, string unidadeId, string clienteId, int ordemOrigem, DateTime data, string tipo, string titulo, decimal? valor)
    {
        VersaoId = Validacao.Identificador(versaoId, nameof(versaoId));
        UnidadeId = Validacao.Texto(unidadeId, nameof(unidadeId));
        ClienteId = Validacao.Texto(clienteId, nameof(clienteId));
        OrdemOrigem = Validacao.Ordem(ordemOrigem, nameof(ordemOrigem));
        Data = Validacao.SemFuso(data, nameof(data));
        Tipo = Validacao.Texto(tipo, nameof(tipo));
        Titulo = Validacao.Texto(titulo, nameof(titulo));
        Valor = valor.HasValue ? Validacao.NaoNegativo(valor.Value, nameof(valor)) : null;
    }

    public Guid VersaoId { get; private set; }
    public string UnidadeId { get; private set; } = string.Empty;
    public string ClienteId { get; private set; } = string.Empty;
    public int OrdemOrigem { get; private set; }
    public DateTime Data { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public string Titulo { get; private set; } = string.Empty;
    public decimal? Valor { get; private set; }

    public Cliente Cliente { get; private set; } = null!;
}
