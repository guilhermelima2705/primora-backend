using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Primora.Domain.Entities;

namespace Primora.Application.Importacao;

public sealed class LeitorDataset
{
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowDuplicateProperties = false,
        MaxDepth = 32
    };

    public DatasetPreparado Preparar(ReadOnlyMemory<byte> conteudo, string nomeOrigem)
    {
        if (string.IsNullOrWhiteSpace(nomeOrigem)) throw Invalid("origem", "nome obrigatório");
        if (conteudo.IsEmpty) throw Invalid("$", "arquivo vazio");
        var hash = Convert.ToHexString(SHA256.HashData(conteudo.Span)).ToLowerInvariant();
        // O hash inclui todos os bytes originais; só o parser ignora um BOM UTF-8 opcional.
        var json = conteudo.Span.StartsWith(new byte[] { 0xef, 0xbb, 0xbf }) ? conteudo[3..] : conteudo;
        try
        {
            using var document = JsonDocument.Parse(json, JsonOptions);
            var root = document.RootElement;
            Object(root, "$", ["geradoEm", "tenants", "clientes"]);
            var date = Date(root, "geradoEm", "$");
            var units = Array(root, "tenants", "$");
            var clients = Array(root, "clientes", "$");
            if (units.GetArrayLength() == 0) throw Invalid("tenants", "ao menos uma unidade é obrigatória");
            var version = new VersaoDataset(Guid.NewGuid(), date, DateTimeOffset.UtcNow, hash, nomeOrigem, "1.0.0");
            var mappedUnits = new List<Unidade>();
            var indicators = new List<IndicadoresUnidade>();
            var engagement = new List<SerieEngajamentoUnidade>();
            var funnel = new List<SerieFunilUnidade>();
            var factors = new List<FatorRiscoUnidade>();
            var mappedClients = new List<Cliente>();
            var scores = new List<HistoricoSaudeCliente>();
            var events = new List<EventoCliente>();
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            var clientIds = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            foreach (var unit in units.EnumerateArray())
            {
                var path = $"tenants[{index}]";
                Object(unit, path, ["id", "nome", "porte", "carteiraTotalClientes", "receitaCarteira90d",
                    "engagementEvents7d", "activeUsers7d", "engagementTrend30d", "funnel90d", "funnelTrend30d",
                    "healthDistribution", "riscoAbsoluto", "riscoDrivers"]);
                var id = Text(unit, "id", path);
                var name = Text(unit, "nome", path);
                if (!names.TryAdd(id, name)) throw Invalid(path + ".id", "ID de unidade duplicado");
                mappedUnits.Add(new Unidade(version.Id, id, name, Text(unit, "porte", path), index + 1));
                var f = Property(unit, "funnel90d", path);
                Object(f, path + ".funnel90d", ["gerada", "vista", "sucesso", "falha"]);
                var h = Property(unit, "healthDistribution", path);
                Object(h, path + ".healthDistribution", ["saudavel", "atencao", "risco", "critico"]);
                indicators.Add(new IndicadoresUnidade(version.Id, id, Integer(unit, "carteiraTotalClientes", path),
                    Money(Property(unit, "receitaCarteira90d", path), path + ".receitaCarteira90d"),
                    Integer(unit, "engagementEvents7d", path), Integer(unit, "activeUsers7d", path),
                    Integer(f, "gerada", path), Integer(f, "vista", path), Integer(f, "sucesso", path), Integer(f, "falha", path),
                    Integer(h, "saudavel", path), Integer(h, "atencao", path), Integer(h, "risco", path), Integer(h, "critico", path),
                    Integer(unit, "riscoAbsoluto", path)));
                var engagementValues = Array(unit, "engagementTrend30d", path);
                var funnelValues = Array(unit, "funnelTrend30d", path);
                if (engagementValues.GetArrayLength() != 30 || funnelValues.GetArrayLength() != 30)
                    throw Invalid(path, "as séries de unidade devem conter 30 posições");
                var position = 1;
                foreach (var point in engagementValues.EnumerateArray())
                    engagement.Add(new SerieEngajamentoUnidade(version.Id, id, position++, Integer(point, path + ".engagementTrend30d")));
                position = 1;
                foreach (var point in funnelValues.EnumerateArray())
                    funnel.Add(new SerieFunilUnidade(version.Id, id, position++, Number(point, path + ".funnelTrend30d")));
                position = 1;
                foreach (var factor in Array(unit, "riscoDrivers", path).EnumerateArray())
                    factors.Add(new FatorRiscoUnidade(version.Id, id, position++, Text(factor, path + ".riscoDrivers")));
                index++;
            }
            index = 0;
            foreach (var client in clients.EnumerateArray())
            {
                var path = $"clientes[{index}]";
                Object(client, path, ["id", "nome", "tenantId", "tenantNome", "healthSeries", "eventos"]);
                var id = Text(client, "id", path);
                var unitId = Text(client, "tenantId", path);
                if (!clientIds.Add(id)) throw Invalid(path + ".id", "ID de cliente duplicado no dataset");
                if (!names.TryGetValue(unitId, out var unitName)) throw Invalid(path + ".tenantId", "unidade inexistente");
                if (!string.Equals(unitName, Text(client, "tenantNome", path), StringComparison.Ordinal))
                    throw Invalid(path + ".tenantNome", "nome diferente do cadastro da unidade");
                mappedClients.Add(new Cliente(version.Id, unitId, id, Text(client, "nome", path), index + 1));
                DateOnly? previous = null;
                foreach (var score in Array(client, "healthSeries", path).EnumerateArray())
                {
                    Object(score, path + ".healthSeries", ["data", "score"]);
                    var scoreDate = Date(score, "data", path);
                    if (previous.HasValue && scoreDate <= previous.Value)
                        throw Invalid(path + ".healthSeries", "datas devem ser únicas e estar em ordem crescente");
                    scores.Add(new HistoricoSaudeCliente(version.Id, unitId, id, scoreDate, Integer(score, "score", path)));
                    previous = scoreDate;
                }
                var order = 1;
                foreach (var item in Array(client, "eventos", path).EnumerateArray())
                {
                    var eventPath = $"{path}.eventos[{order - 1}]";
                    Object(item, eventPath, ["data", "tipo", "titulo"], ["valor"]);
                    var rawDate = Text(item, "data", eventPath);
                    if (!DateTime.TryParseExact(rawDate, ["yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"],
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var eventDate)
                        || eventDate.Kind != DateTimeKind.Unspecified || eventDate.Ticks % 10 != 0)
                        throw Invalid(eventPath + ".data", "horário sem fuso obrigatório, com precisão máxima de microssegundos");
                    decimal? value = null;
                    if (item.TryGetProperty("valor", out var amount) && amount.ValueKind != JsonValueKind.Null)
                        value = Money(amount, eventPath + ".valor");
                    events.Add(new EventoCliente(version.Id, unitId, id, order++, eventDate,
                        Text(item, "tipo", eventPath), Text(item, "titulo", eventPath), value));
                }
                index++;
            }
            return new DatasetPreparado(version, mappedUnits, indicators, engagement, funnel, factors, mappedClients, scores, events);
        }
        catch (JsonException) { throw Invalid("$", "JSON inválido ou propriedades duplicadas"); }
        catch (ArgumentException) { throw Invalid("$", "valores incompatíveis com as regras de integridade do domínio"); }
    }

    private static void Object(JsonElement value, string path, string[] required, string[]? optional = null)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid(path, "objeto obrigatório");
        foreach (var name in required) if (!value.TryGetProperty(name, out _)) throw Invalid(path + "." + name, "campo obrigatório");
        foreach (var field in value.EnumerateObject())
            if (!required.Contains(field.Name, StringComparer.Ordinal) && !(optional?.Contains(field.Name, StringComparer.Ordinal) ?? false))
                throw Invalid(path, "campo não reconhecido pelo contrato do dataset");
    }
    private static JsonElement Property(JsonElement value, string name, string path) =>
        value.TryGetProperty(name, out var field) ? field : throw Invalid(path + "." + name, "campo obrigatório");
    private static JsonElement Array(JsonElement value, string name, string path)
    {
        var field = Property(value, name, path);
        return field.ValueKind == JsonValueKind.Array ? field : throw Invalid(path + "." + name, "array obrigatório");
    }
    private static string Text(JsonElement value, string path) =>
        value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()! : throw Invalid(path, "texto obrigatório");
    private static string Text(JsonElement value, string name, string path) => Text(Property(value, name, path), path + "." + name);
    private static int Integer(JsonElement value, string path) => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
        ? number : throw Invalid(path, "inteiro obrigatório");
    private static int Integer(JsonElement value, string name, string path) => Integer(Property(value, name, path), path + "." + name);
    private static DateOnly Date(JsonElement value, string name, string path) =>
        DateOnly.TryParseExact(Text(value, name, path), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date : throw Invalid(path + "." + name, "data deve usar yyyy-MM-dd");
    private static decimal Money(JsonElement value, string path)
    {
        var number = Number(value, path);
        if (number < 0 || number > 9999999999999999.99m || decimal.Round(number, 2) != number)
            throw Invalid(path, "valor monetário deve caber em numeric(18,2), sem arredondamento");
        return number;
    }
    private static decimal Number(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number)) throw Invalid(path, "decimal obrigatório");
        if (Normalize(value.GetRawText(), path) != Normalize(number.ToString(CultureInfo.InvariantCulture), path))
            throw Invalid(path, "decimal não pode ser representado sem perda de precisão");
        return number;
    }
    private static (BigInteger Coefficient, long Scale) Normalize(string number, string path)
    {
        if (number.Length > 512) throw Invalid(path, "número fora da precisão suportada");
        var exponentIndex = number.IndexOfAny(['e', 'E']);
        long exponent = 0;
        if (exponentIndex >= 0)
        {
            if (!int.TryParse(number[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
                throw Invalid(path, "expoente fora da precisão suportada");
            exponent = parsed;
            number = number[..exponentIndex];
        }
        var dot = number.IndexOf('.');
        long scale = (dot < 0 ? 0 : number.Length - dot - 1) - exponent;
        var coefficient = BigInteger.Parse(number.Replace(".", string.Empty), CultureInfo.InvariantCulture);
        if (coefficient.IsZero) return (BigInteger.Zero, 0);
        while (coefficient % 10 == 0) { coefficient /= 10; scale--; }
        return (coefficient, scale);
    }
    private static DatasetInvalidoException Invalid(string path, string message) => new($"{path}: {message}.");
}
