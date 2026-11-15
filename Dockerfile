FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/Primora.Api/Primora.Api.csproj src/Primora.Api/
COPY src/Primora.Application/Primora.Application.csproj src/Primora.Application/
COPY src/Primora.Domain/Primora.Domain.csproj src/Primora.Domain/
COPY src/Primora.Infrastructure/Primora.Infrastructure.csproj src/Primora.Infrastructure/
RUN dotnet restore src/Primora.Api/Primora.Api.csproj

COPY src/ src/
RUN dotnet publish src/Primora.Api/Primora.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14 AS runtime
WORKDIR /app
COPY --from=build /app/publish/ ./
USER $APP_UID
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "exec dotnet Primora.Api.dll --urls \"http://0.0.0.0:${PORT:-10000}\""]
