using System.Text.Json.Serialization;

namespace Primora.Application.Dataset;

public sealed record ClienteDto(string Id, string Nome, string TenantId, string TenantNome,
    IReadOnlyList<RegistroSaudeDto> HealthSeries, IReadOnlyList<EventoDto> Eventos);

public sealed record RegistroSaudeDto(DateOnly Data, int Score);

public sealed record EventoDto(DateTime Data, string Tipo, string Titulo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Valor);
