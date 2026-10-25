param(
    [string]$PostgresBin,
    [string]$DbHost = '127.0.0.1',
    [int]$Port = 5432,
    [string]$AdminUser = 'postgres',
    [System.Security.SecureString]$AdminPassword
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$configFile = Join-Path $projectRoot '.local/connection-string.txt'
$projectFile = Join-Path $projectRoot 'DemoForMuhin.csproj'

if (-not (Test-Path -LiteralPath $configFile)) {
    if (-not $PostgresBin) {
        $registry = Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Services\postgresql-x64-18' -ErrorAction SilentlyContinue
        if ($registry.ImagePath -match '^"([^"]+pg_ctl.exe)"') {
            $PostgresBin = Split-Path $Matches[1] -Parent
        } else {
            $psqlCommand = Get-Command psql -ErrorAction SilentlyContinue
            if ($psqlCommand) { $PostgresBin = Split-Path $psqlCommand.Source -Parent }
        }
    }
    $psql = if ($PostgresBin) { Join-Path $PostgresBin 'psql.exe' } else { '' }
    if (-not $psql -or -not (Test-Path -LiteralPath $psql)) {
        throw 'psql.exe не найден. Укажите -PostgresBin с путём к папке bin установленного PostgreSQL.'
    }
    if (-not $AdminPassword) { $AdminPassword = Read-Host 'Пароль администратора PostgreSQL (не сохраняется)' -AsSecureString }
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminPassword)
    $previousPassword = $env:PGPASSWORD
    try {
        $env:PGPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
        $databaseExists = & $psql -X -w -h $DbHost -p $Port -U $AdminUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'demoformuhin'"
        if ($LASTEXITCODE -ne 0) { throw 'Не удалось подключиться к PostgreSQL. Проверьте адрес, порт, пользователя и пароль.' }
        $roleExists = & $psql -X -w -h $DbHost -p $Port -U $AdminUser -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname = 'demoformuhin_app'"
        if ($LASTEXITCODE -ne 0) { throw 'Не удалось проверить роль приложения.' }
        if ($databaseExists -or $roleExists) {
            throw 'БД demoformuhin или роль demoformuhin_app уже существуют. Скрипт не изменяет существующие объекты. Настройте .local/connection-string.txt вручную.'
        }
        $appPassword = [Guid]::NewGuid().ToString('N')
        $createRoleSql = "CREATE ROLE demoformuhin_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD '$appPassword';"
        $createRoleSql | & $psql -X -w -h $DbHost -p $Port -U $AdminUser -d postgres -v ON_ERROR_STOP=1 -q
        if ($LASTEXITCODE -ne 0) { throw 'Не удалось создать роль приложения.' }
        & $psql -X -w -h $DbHost -p $Port -U $AdminUser -d postgres -v ON_ERROR_STOP=1 -q -c 'CREATE DATABASE demoformuhin OWNER demoformuhin_app;'
        if ($LASTEXITCODE -ne 0) { throw 'Не удалось создать БД. Создана только роль demoformuhin_app; проверьте права администратора.' }
        New-Item -ItemType Directory -Path (Split-Path $configFile -Parent) -Force | Out-Null
        "Host=$DbHost;Port=$Port;Database=demoformuhin;Username=demoformuhin_app;Password=$appPassword" | Set-Content -LiteralPath $configFile -Encoding utf8
        Write-Host 'Созданы отдельная БД demoformuhin и роль demoformuhin_app с ограниченными правами.'
    } finally {
        $env:PGPASSWORD = $previousPassword
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
}
& dotnet run --project $projectFile -- --import-data
if ($LASTEXITCODE -ne 0) { throw 'Не удалось подготовить исходные данные.' }

