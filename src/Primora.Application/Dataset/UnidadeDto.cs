namespace Primora.Application.Dataset;

public sealed record UnidadeDto(string Id, string Nome, string Porte, int CarteiraTotalClientes,
    decimal ReceitaCarteira90d, int EngagementEvents7d, int ActiveUsers7d,
    IReadOnlyList<int> EngagementTrend30d, FunilDto Funnel90d, IReadOnlyList<decimal> FunnelTrend30d,
    DistribuicaoSaudeDto HealthDistribution, int RiscoAbsoluto, IReadOnlyList<string> RiscoDrivers);

public sealed record FunilDto(int Gerada, int Vista, int Sucesso, int Falha);
public sealed record DistribuicaoSaudeDto(int Saudavel, int Atencao, int Risco, int Critico);
