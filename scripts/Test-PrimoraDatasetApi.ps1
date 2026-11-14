param(
    [Parameter(Mandatory)]
    [string]$DatasetPath,

    [string]$ApiBaseUrl = 'https://localhost:7246'
)

$ErrorActionPreference = 'Stop'
$dataset = [System.Text.Json.JsonDocument]::Parse([IO.File]::ReadAllText((Resolve-Path -LiteralPath $DatasetPath).Path))
try {
    foreach ($rota in @('unidades', 'clientes')) {
        $resposta = Invoke-WebRequest -Uri "$($ApiBaseUrl.TrimEnd('/'))/$rota"
        $campo = if ($rota -eq 'unidades') { 'tenants' } else { 'clientes' }
        $esperado = $dataset.RootElement.GetProperty($campo)
        $recebido = [System.Text.Json.JsonDocument]::Parse([string]$resposta.Content)
        try {
            if ($resposta.StatusCode -ne 200 -or
                -not [System.Text.Json.JsonElement]::DeepEquals($esperado, $recebido.RootElement)) {
                throw "Resposta de /$rota diverge do dataset."
            }
            if (@($resposta.Headers['X-Dataset-Gerado-Em']).Count -ne 1 -or
                $resposta.Headers['X-Dataset-Gerado-Em'][0] -ne $dataset.RootElement.GetProperty('geradoEm').GetString()) {
                throw "Data de geração de /$rota diverge do dataset."
            }
            Write-Output "/$rota HTTP 200, $($recebido.RootElement.GetArrayLength()) itens, campos e datas equivalentes."
        }
        finally { $recebido.Dispose() }
    }
}
finally { $dataset.Dispose() }
