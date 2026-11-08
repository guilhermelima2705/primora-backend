using System.Text.Json;
using System.Text.Json.Nodes;

namespace Primora.Tests.Fixtures;

internal static class DatasetFixture
{
    public const decimal PreciseRatio = 0.1234567890123456789012345678m;

    public static JsonObject Create(string? tag = null)
    {
        tag ??= Guid.NewGuid().ToString("N");
        JsonObject Unit(string id, string name) => new()
        {
            ["id"] = id, ["nome"] = name, ["porte"] = "media", ["carteiraTotalClientes"] = 10,
            ["receitaCarteira90d"] = 12345.67m, ["engagementEvents7d"] = 123, ["activeUsers7d"] = 2,
            ["engagementTrend30d"] = new JsonArray(Enumerable.Range(1, 30).Select(x => (JsonNode?)JsonValue.Create(x)).ToArray()),
            ["funnel90d"] = new JsonObject { ["gerada"] = 10, ["vista"] = 8, ["sucesso"] = 4, ["falha"] = 3 },
            ["funnelTrend30d"] = new JsonArray(Enumerable.Range(1, 30).Select(_ => (JsonNode?)JsonValue.Create(PreciseRatio)).ToArray()),
            ["healthDistribution"] = new JsonObject { ["saudavel"] = 3, ["atencao"] = 3, ["risco"] = 2, ["critico"] = 2 },
            ["riscoAbsoluto"] = 4, ["riscoDrivers"] = new JsonArray("Fator de teste A", "Fator de teste B")
        };
        var firstName = "Unidade A " + tag;
        return new JsonObject
        {
            ["geradoEm"] = "2026-09-01",
            ["tenants"] = new JsonArray(Unit("unidade-a", firstName), Unit("unidade-b", "Unidade B " + tag)),
            ["clientes"] = new JsonArray(new JsonObject
            {
                ["id"] = "cliente-a", ["nome"] = "Cliente de teste", ["tenantId"] = "unidade-a", ["tenantNome"] = firstName,
                ["healthSeries"] = new JsonArray(new JsonObject { ["data"] = "2026-08-01", ["score"] = 47 },
                    new JsonObject { ["data"] = "2026-09-01", ["score"] = 52 }),
                ["eventos"] = new JsonArray(
                    new JsonObject { ["data"] = "2026-09-01T09:12:00", ["tipo"] = "compra", ["titulo"] = "Pedido de teste", ["valor"] = 98.76m },
                    new JsonObject { ["data"] = "2026-09-01T09:12:00", ["tipo"] = "nota", ["titulo"] = "Nota no mesmo horário" })
            })
        };
    }

    public static byte[] Bytes(JsonObject document) => JsonSerializer.SerializeToUtf8Bytes(document);

    public static JsonObject Invalid(string scenario)
    {
        var document = Create();
        var unit = document["tenants"]![0]!.AsObject();
        var client = document["clientes"]![0]!.AsObject();
        switch (scenario)
        {
            case "missing-root": document.Remove("geradoEm"); break;
            case "unknown-root": document["extra"] = true; break;
            case "null-array": document["tenants"] = null; break;
            case "no-units": document["tenants"]!.AsArray().Clear(); break;
            case "non-object": document["tenants"]![0] = 1; break;
            case "duplicate-unit": document["tenants"]!.AsArray().Add(unit.DeepClone()); break;
            case "duplicate-client": document["clientes"]!.AsArray().Add(client.DeepClone()); break;
            case "invalid-tenant": client["tenantId"] = "inexistente"; break;
            case "tenant-name": client["tenantNome"] = "Nome divergente"; break;
            case "missing-counter": unit.Remove("activeUsers7d"); break;
            case "unknown-nested": unit["healthDistribution"]!["extra"] = 1; break;
            case "money-scale": unit["receitaCarteira90d"] = 123.456m; break;
            case "money-overflow": unit["receitaCarteira90d"] = 10000000000000000m; break;
            case "money-string": unit["receitaCarteira90d"] = "123.45"; break;
            case "negative-counter": unit["engagementEvents7d"] = -1; break;
            case "distribution": unit["healthDistribution"]!["saudavel"] = 4; break;
            case "absolute-risk": unit["riscoAbsoluto"] = 5; break;
            case "series-length": unit["engagementTrend30d"]!.AsArray().RemoveAt(0); break;
            case "ratio-range": unit["funnelTrend30d"]![0] = 1.01m; break;
            case "decimal-loss": unit["funnelTrend30d"]![0] = JsonNode.Parse("0.123456789012345678901234567891"); break;
            case "score-range": client["healthSeries"]![0]!["score"] = 101; break;
            case "duplicate-date": client["healthSeries"]![1]!["data"] = "2026-08-01"; break;
            case "date-order": client["healthSeries"]![0]!["data"] = "2026-10-01"; break;
            case "event-timezone": client["eventos"]![0]!["data"] = "2026-09-01T09:12:00Z"; break;
            case "event-precision": client["eventos"]![0]!["data"] = "2026-09-01T09:12:00.0000001"; break;
            case "late-invalid-event": client["eventos"]![1]!.AsObject().Remove("titulo"); break;
            default: throw new ArgumentException("Cenário de teste desconhecido.", nameof(scenario));
        }
        return document;
    }
}
