$secretPath = Join-Path $PSScriptRoot '..\.secrets\primora_app_password'
if (-not (Test-Path -LiteralPath $secretPath)) { throw 'Senha local do Primora não encontrada.' }
$databasePassword = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $secretPath)).Trim()
$env:ConnectionStrings__Primora = "Host=127.0.0.1;Port=5433;Database=primora;Username=primora_app;Password=$databasePassword"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Write-Host 'Conexão Primora carregada neste terminal: 127.0.0.1:5433 / primora / primora_app. Senha omitida.'
