namespace Primora.Domain.Entities;

public sealed class VersaoDataset
{
    private VersaoDataset() { }

    public VersaoDataset(Guid id, DateOnly geradoEm, DateTimeOffset importadoEm, string hashSha256, string nomeOrigem, string versaoImportador)
    {
        Id = Validacao.Identificador(id, nameof(id));
        GeradoEm = geradoEm;
        ImportadoEm = Validacao.Utc(importadoEm, nameof(importadoEm));
        HashSha256 = Validacao.Hash(hashSha256);
        NomeOrigem = Validacao.Texto(nomeOrigem, nameof(nomeOrigem));
        VersaoImportador = Validacao.Texto(versaoImportador, nameof(versaoImportador));
    }

    public Guid Id { get; private set; }
    public DateOnly GeradoEm { get; private set; }
    public DateTimeOffset ImportadoEm { get; private set; }
    public string HashSha256 { get; private set; } = string.Empty;
    public string NomeOrigem { get; private set; } = string.Empty;
    public string VersaoImportador { get; private set; } = string.Empty;

    public ICollection<Unidade> Unidades { get; } = new List<Unidade>();
}
