using Primora.Api.Controllers;
using Primora.Api.ErrorHandling;
using Primora.Application;
using Primora.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    var frontendOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (frontendOrigins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
        uri.Scheme is not ("https" or "http") || origin != uri.GetLeftPart(UriPartial.Authority)))
        throw new InvalidOperationException("Configure Cors:AllowedOrigins com origens HTTP(S) exatas.");

    options.AddDefaultPolicy(policy => policy.WithOrigins(frontendOrigins)
        .WithMethods(HttpMethods.Get)
        .WithExposedHeaders(DatasetController.GeradoEmHeader));
});
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
// O proxy gerenciado termina o TLS e redireciona o HTTP público.
if (!app.Environment.IsDevelopment() &&
    !app.Configuration.GetValue<bool>("Hosting:TlsTerminatedByProxy"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseCors();
app.MapControllers();
app.Run();

public partial class Program;
