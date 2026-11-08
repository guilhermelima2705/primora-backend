using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Primora.Application;
using Primora.Application.Importacao;
using Primora.Infrastructure;

if (args.Length == 1 && args[0] is "--help" or "-h")
{
    Console.WriteLine("Uso: dotnet run --project src/Primora.Importador -- <caminho-do-json>");
    return 0;
}
if (args.Length != 1)
{
    Console.Error.WriteLine("Informe exatamente um caminho de arquivo JSON. Use --help para instruções.");
    return 2;
}
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, signal) => { signal.Cancel = true; cancellation.Cancel(); };
try
{
    var content = await File.ReadAllBytesAsync(args[0], cancellation.Token);
    var builder = Host.CreateApplicationBuilder();
    // Saída resumida: nunca registrar configuração, SQL ou credenciais no console do importador.
    builder.Logging.ClearProviders();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure();
    using var host = builder.Build();
    await using var scope = host.Services.CreateAsyncScope();
    var importer = scope.ServiceProvider.GetRequiredService<IDatasetImportador>();
    var result = await importer.ImportarAsync(content, Path.GetFileName(args[0]), cancellation.Token);
    Console.WriteLine(result.NovaVersao ? "Importação concluída." : "Conteúdo já importado. Nenhum registro foi duplicado.");
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
    return 0;
}
catch (DatasetInvalidoException error)
{
    Console.Error.WriteLine("Dataset rejeitado: " + error.Message);
    return 2;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Importação cancelada.");
    return 130;
}
catch (Exception)
{
    Console.Error.WriteLine("Não foi possível concluir a importação. Verifique o caminho, a configuração e o PostgreSQL. O hash permite repetir a operação sem duplicar dados.");
    return 1;
}
