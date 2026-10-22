# Собирает ShotHost.exe в windows-app\build\. Нужен .NET SDK 8: https://dotnet.microsoft.com/download
# Обычная иконка только у релиза: $env:SHOTHOST_RELEASE = "1"; иначе сборка получает серую иконку разработки.
param([string]$Version = "0.0.0")
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

dotnet test Tests\ShotHost.Tests.csproj
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build ShotHost\ShotHost.csproj -c Release -p:Version=$Version
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Force build | Out-Null
Copy-Item ShotHost\bin\Release\net48\ShotHost.exe build\ShotHost.exe -Force
Write-Host "Готово: $PSScriptRoot\build\ShotHost.exe"
