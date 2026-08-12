[CmdletBinding()]
param(
    [ValidateSet('smoke', 'baseline')]
    [string] $Profile = 'smoke',
    [string] $BaseUrl = 'http://host.docker.internal:8082',
    [Parameter(Mandatory)]
    [string] $UserEmail,
    [Parameter(Mandatory)]
    [string] $UserPassword,
    [ValidateRange(0, 60)]
    [int] $ThinkTimeSeconds = 1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker Desktop é necessário para executar k6 sem instalação local. Inicie-o e tente novamente.'
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$scenarioPath = Join-Path $projectRoot 'performance/k6/patients-read.js'
$artifactsPath = Join-Path $projectRoot 'artifacts/performance'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$summaryFile = "patients-read-$Profile-$timestamp.json"

if (-not (Test-Path -LiteralPath $scenarioPath)) {
    throw "Cenário k6 não encontrado: $scenarioPath"
}

New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null

Get-Content -LiteralPath $scenarioPath -Raw |
    docker run --rm -i `
        -v "${artifactsPath}:/results" `
        grafana/k6 run `
        -e "BASE_URL=$BaseUrl" `
        -e "PERF_PROFILE=$Profile" `
        -e "PERF_USER_EMAIL=$UserEmail" `
        -e "PERF_USER_PASSWORD=$UserPassword" `
        -e "PERF_THINK_TIME_SECONDS=$ThinkTimeSeconds" `
        --summary-export="/results/$summaryFile" -

if ($LASTEXITCODE -ne 0) {
    throw "O k6 reprovou thresholds ou não conseguiu executar o cenário. Consulte $artifactsPath."
}

Write-Host "Teste concluído. Resumo salvo em: $(Join-Path $artifactsPath $summaryFile)"
