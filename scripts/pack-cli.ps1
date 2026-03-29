# Pack TigerClaw CLI as a NuGet global tool (.nupkg under artifacts/nuget).
param(
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root "artifacts\nuget"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Push-Location $root
try {
    dotnet pack "src\TigerClaw.Cli\TigerClaw.Cli.csproj" -c $Configuration -o $outDir --nologo
    Write-Host "Package output: $outDir"
    Get-ChildItem $outDir -Filter "*.nupkg" | ForEach-Object { Write-Host "  $($_.Name)" }
}
finally {
    Pop-Location
}
