namespace Primora.Application.Importacao;

public sealed record ContagensDataset(int Versoes, int Unidades, int Indicadores, int Clientes,
    int Scores, int Eventos, int PontosEngajamento, int PontosFunil, int FatoresRisco);

public sealed record ResultadoImportacao(Guid VersaoId, string HashSha256, bool NovaVersao, ContagensDataset Contagens);
