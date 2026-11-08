namespace Primora.Api.Contracts;

public sealed record HealthResponse(string Status, string Application, string Database);
