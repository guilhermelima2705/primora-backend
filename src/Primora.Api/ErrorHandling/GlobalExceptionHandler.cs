using Microsoft.AspNetCore.Diagnostics;

namespace Primora.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var status = exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode : StatusCodes.Status500InternalServerError;
        // Não incluir mensagem de exceção, conexão ou credenciais nos logs/respostas.
        logger.LogError("Falha HTTP {Status}. Tipo: {ExceptionType}. TraceId: {TraceId}",
            status, exception.GetType().Name, context.TraceIdentifier);
        await Results.Problem(statusCode: status,
            title: status == 500 ? "Erro interno" : "Requisição inválida",
            detail: status == 500 ? "Ocorreu um erro inesperado." : "Não foi possível processar a requisição.",
            extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
            .ExecuteAsync(context);
        return true;
    }
}
