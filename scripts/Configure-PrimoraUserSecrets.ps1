$ErrorActionPreference = 'Stop'
$apiProject = Join-Path $PSScriptRoot '..\src\Primora.Api\Primora.Api.csproj'
$secretPath = Join-Path $PSScriptRoot '..\.secrets\primora_app_password'
if (-not (Test-Path -LiteralPath $secretPath)) { throw 'Senha local do Primora não encontrada.' }
$databasePassword = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $secretPath)).Trim()
$connection = "Host=127.0.0.1;Port=5433;Database=primora;Username=primora_app;Password=$databasePassword"
# O segredo é enviado pela entrada padrão, sem incluí-lo na linha de comando ou no projeto.
@{ 'ConnectionStrings:Primora' = $connection } | ConvertTo-Json -Compress | dotnet user-secrets set --project $apiProject
if ($LASTEXITCODE -ne 0) { throw 'Não foi possível configurar os User Secrets da API.' }
Write-Host 'Conexão da API configurada nos User Secrets deste usuário. Senha omitida.'
