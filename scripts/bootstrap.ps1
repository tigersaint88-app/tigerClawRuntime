# TigerClaw bootstrap - ensure directories and build
Set-Location $PSScriptRoot\..

New-Item -ItemType Directory -Force -Path data, workflows, skills, logs, artifacts | Out-Null
dotnet restore
dotnet build

Write-Host "Bootstrap complete. Run: .\scripts\run-cli.ps1 skills list"
