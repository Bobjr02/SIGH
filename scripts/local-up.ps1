<#
.SYNOPSIS
  Sobe o SIGH inteiro (banco, Mailpit, API e web) com dados FICTICIOS de demonstracao.
.DESCRIPTION
  Primeira execucao: gera o .env.local (fora do git) com segredos aleatorios. As seguintes o reaproveitam.
  Funciona no Windows PowerShell 5.1 e no PowerShell 7.
  Se o Windows bloquear o script, libere so para esta sessao:
    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.EXAMPLE
  scripts\local-up.ps1
#>
[CmdletBinding()]
param(
  [string]$Project = $(if ($env:SIGH_LOCAL_PROJECT) { $env:SIGH_LOCAL_PROJECT } else { 'sigh-local' })
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$envFile = '.env.local'

& docker compose version *> $null
if ($LASTEXITCODE -ne 0) {
  Write-Error "'docker compose' nao encontrado. Instale e abra o Rancher Desktop (motor dockerd/moby) e tente de novo."
  exit 1
}

function New-RandomText([int]$Length, [string]$Alphabet) {
  $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
  $bytes = New-Object byte[] ($Length * 4)
  $rng.GetBytes($bytes)
  $chars = for ($i = 0; $i -lt $Length; $i++) { $Alphabet[[BitConverter]::ToUInt32($bytes, $i * 4) % $Alphabet.Length] }
  $rng.Dispose()
  return -join $chars
}

# Letras e digitos + sufixo que garante maiuscula, minuscula, digito e simbolo (sem $, #, aspas, ; ou =).
function New-Secret([int]$Length) {
  return (New-RandomText $Length 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789') + '-Aa9'
}

if (-not (Test-Path $envFile)) {
  $stamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
  $lines = @(
    "# Gerado por scripts/local-up em $stamp. Fora do git (.gitignore: .env.*).",
    '# Segredos SO desta maquina. Apague este arquivo e rode scripts\local-reset.ps1 para gerar outros.',
    "SIGH_SA_PASSWORD=$(New-Secret 24)",
    "SIGH_JWT_SECRET=$(New-RandomText 64 '0123456789abcdef')",
    'SIGH_ADMIN_EMAIL=admin@demo.local',
    "SIGH_ADMIN_PASSWORD=$(New-Secret 16)",
    "SIGH_DEMO_PASSWORD=$(New-Secret 12)",
    'SIGH_WEB_PORT=8080',
    'SIGH_MAILPIT_PORT=8025'
  )
  # UTF-8 SEM BOM e fim de linha LF: o docker compose le o arquivo como .env.
  [System.IO.File]::WriteAllText((Join-Path (Get-Location) $envFile), (($lines -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding($false)))
  Write-Host "Arquivo $envFile criado com segredos aleatorios."
}

$settings = @{}
foreach ($line in Get-Content $envFile) {
  if ($line -match '^\s*#' -or $line -notmatch '=') { continue }
  $parts = $line.Split('=', 2)
  $settings[$parts[0].Trim()] = $parts[1].Trim()
}
function Get-Setting([string]$Name, [string]$Default) {
  if ($settings.ContainsKey($Name) -and $settings[$Name]) { return $settings[$Name] } else { return $Default }
}

# Portas ocupadas por outro programa (se a stack deste projeto ja esta de pe, as portas sao dela).
$running = & docker compose -p $Project -f compose.app.yml --env-file $envFile ps -q web 2>$null
if (-not $running) {
  foreach ($name in @('SIGH_WEB_PORT', 'SIGH_MAILPIT_PORT')) {
    $port = [int](Get-Setting $name '0')
    if ($port -le 0) { continue }
    $client = New-Object System.Net.Sockets.TcpClient
    try {
      $inUse = $client.ConnectAsync('127.0.0.1', $port).Wait(500)
    } catch { $inUse = $false } finally { $client.Dispose() }
    if ($inUse) {
      Write-Error "A porta $port ($name) ja esta em uso por outro programa. Feche-o ou troque $name no $envFile."
      exit 1
    }
  }
}

Write-Host "Subindo o SIGH (projeto `"$Project`"). A primeira vez baixa as imagens e compila: pode levar alguns minutos..."
$watch = [System.Diagnostics.Stopwatch]::StartNew()
& docker compose -p $Project -f compose.app.yml --env-file $envFile up -d --build --wait --wait-timeout 1200
if ($LASTEXITCODE -ne 0) {
  Write-Error "A stack nao ficou saudavel. Veja: docker compose -p $Project -f compose.app.yml logs api"
  exit 1
}
$watch.Stop()
$elapsed = [int]$watch.Elapsed.TotalSeconds

$demoStatus = (& docker compose -p $Project -f compose.app.yml --env-file $envFile logs api 2>$null) |
  Select-String -Pattern 'Demo: (dados de demonstra|falha)' | Select-Object -Last 1

$webPort = Get-Setting 'SIGH_WEB_PORT' '8080'
$mailPort = Get-Setting 'SIGH_MAILPIT_PORT' '8025'
$demoPassword = Get-Setting 'SIGH_DEMO_PASSWORD' ''

Write-Host ''
Write-Host '============================================================'
Write-Host " SIGH no ar em ${elapsed}s  -  AMBIENTE DE DEMONSTRACAO (dados ficticios)"
Write-Host '============================================================'
Write-Host " Abra:            http://localhost:$webPort"
Write-Host " Administrador:   $(Get-Setting 'SIGH_ADMIN_EMAIL' 'admin@demo.local')"
Write-Host " Senha do admin:  $(Get-Setting 'SIGH_ADMIN_PASSWORD' '')"
Write-Host '                  (no primeiro acesso o sistema pede para trocar a senha)'
Write-Host ''
Write-Host " Usuarios de demonstracao (todos com a senha: $demoPassword)"
Write-Host '   auxiliar@demo.local            RH - Registro'
Write-Host '   encarregado@demo.local         RH - Aprovador'
Write-Host '   juridico@demo.local            Juridico'
Write-Host '   gestor.industrial@demo.local   Gestor da USGA131'
Write-Host '   gestor.agricola@demo.local     Gestor da USGA137 e USGA141'
Write-Host '   preposto@demo.local            RH - Registro + RH - Aprovador'
Write-Host ''
Write-Host " E-mails (Mailpit): http://localhost:$mailPort"
Write-Host ' Parar:  scripts\local-down.ps1     Zerar:  scripts\local-reset.ps1'
Write-Host '============================================================'

if (-not $demoStatus) {
  Write-Warning 'Nao encontrei no log da API a confirmacao dos dados de demonstracao.'
} elseif ($demoStatus.Line -match 'falha') {
  Write-Warning "Os dados de demonstracao falharam. Veja: docker compose -p $Project -f compose.app.yml logs api"
}
