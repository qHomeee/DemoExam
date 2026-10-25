$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot '.local/connection-string.txt')) -and -not $env:DEMOFORMUHIN_CONNECTION_STRING) {
    & (Join-Path $PSScriptRoot 'Setup-Database.ps1')
}
& dotnet run --project (Join-Path $projectRoot 'DemoForMuhin.csproj')
if ($LASTEXITCODE -ne 0) { throw 'Приложение завершилось с ошибкой.' }
