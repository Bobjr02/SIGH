<#
.SYNOPSIS
  Apaga SO os volumes deste projeto compose (banco e evidencias do SIGH local), pedindo confirmacao.
.DESCRIPTION
  Nao toca em outros projetos, volumes ou no banco de desenvolvimento. O .env.local e mantido.
#>
[CmdletBinding()]
param(
  [string]$Project = $(if ($env:SIGH_LOCAL_PROJECT) { $env:SIGH_LOCAL_PROJECT } else { 'sigh-local' })
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

Write-Host "Isto apaga TODOS os dados do SIGH local do projeto `"$Project`" (volumes ${Project}_db-data e ${Project}_evidences)."
$answer = Read-Host 'Digite APAGAR para confirmar'
if ($answer -cne 'APAGAR') {
  Write-Host 'Cancelado. Nada foi apagado.'
  exit 0
}

$envArgs = @()
if (Test-Path '.env.local') { $envArgs = @('--env-file', '.env.local') }
else { $env:SIGH_SA_PASSWORD = 'unused'; $env:SIGH_JWT_SECRET = 'unused'; $env:SIGH_ADMIN_PASSWORD = 'unused'; $env:SIGH_DEMO_PASSWORD = 'unused' }

& docker compose -p $Project -f compose.app.yml @envArgs down --volumes --remove-orphans
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host 'Dados apagados. Na proxima vez, scripts\local-up.ps1 recria tudo do zero.'
