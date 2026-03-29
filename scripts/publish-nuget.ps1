# Push TigerClaw.Cli .nupkg to nuget.org (requires API key).
# Usage:
#   .\scripts\publish-nuget.ps1 -ApiKey $env:NUGET_API_KEY
#   .\scripts\publish-nuget.ps1 -ApiKey "oyj..." -Source https://api.nuget.org/v3/index.json
param(
    [Parameter(Mandatory = $true)]
    [string] $ApiKey,
    [string] $Source = "https://api.nuget.org/v3/index.json",
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root "artifacts\nuget"

& (Join-Path $root "scripts\pack-cli.ps1") -Configuration $Configuration

$nupkg = Get-ChildItem $outDir -Filter "TigerClaw.Cli.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $nupkg) {
    throw "No TigerClaw.Cli.*.nupkg found in $outDir"
}

Write-Host "Pushing $($nupkg.Name) to $Source ..."
dotnet nuget push $nupkg.FullName --api-key $ApiKey --source $Source --skip-duplicate
