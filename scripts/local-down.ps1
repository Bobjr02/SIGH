<#
.SYNOPSIS
  Para o SIGH local. Os dados (banco e evidencias) ficam guardados nos volumes do projeto.
#>
[CmdletBinding()]
param(
  [string]$Project = $(if ($env:SIGH_LOCAL_PROJECT) { $env:SIGH_LOCAL_PROJECT } else { 'sigh-local' })
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

$envArgs = @()
if (Test-Path '.env.local') { $envArgs = @('--env-file', '.env.local') }
else { $env:SIGH_SA_PASSWORD = 'unused'; $env:SIGH_JWT_SECRET = 'unused'; $env:SIGH_ADMIN_PASSWORD = 'unused'; $env:SIGH_DEMO_PASSWORD = 'unused' }

& docker compose -p $Project -f compose.app.yml @envArgs down
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host 'SIGH parado. Os dados foram mantidos. Para subir de novo: scripts\local-up.ps1'
