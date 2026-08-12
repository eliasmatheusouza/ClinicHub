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
    [int] $ThinkTimeSeconds = 1,
    [switch] $CaptureResources,
    [ValidateRange(1, 60)]
    [int] $ResourceSampleIntervalSeconds = 2,
    [string[]] $ResourceContainerNames = @(
        'clinichub-api-1',
        'clinichub-sqlserver-1',
        'clinichub-redis-1',
        'clinichub-rabbitmq-1',
        'clinichub-notifications-worker-1'
    )
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
$resourceFile = "patients-read-$Profile-$timestamp-resources.jsonl"

if (-not (Test-Path -LiteralPath $scenarioPath)) {
    throw "Cenário k6 não encontrado: $scenarioPath"
}

New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null

$resourceJob = $null
$resourcePath = Join-Path $artifactsPath $resourceFile

if ($CaptureResources) {
    $runningContainers = @(docker ps --format '{{.Names}}')
    $missingContainers = @($ResourceContainerNames | Where-Object { $_ -notin $runningContainers })

    if ($missingContainers.Count -gt 0) {
        throw "Não foi possível capturar recursos. Contêineres ausentes: $($missingContainers -join ', '). Inicie a stack do ClinicHub ou informe -ResourceContainerNames."
    }

    New-Item -ItemType File -Path $resourcePath -Force | Out-Null
    $dockerCommand = (Get-Command docker -ErrorAction Stop).Source

    $resourceJob = Start-Job -Name 'clinichub-performance-resource-capture' -ScriptBlock {
        param(
            [string] $DockerCommand,
            [string[]] $ContainerNames,
            [string] $OutputPath,
            [int] $SampleIntervalSeconds
        )

        while ($true) {
            $capturedAtUtc = [DateTime]::UtcNow.ToString('o')
            $samples = & $DockerCommand stats --no-stream --format '{{json .}}' $ContainerNames

            foreach ($sample in $samples) {
                try {
                    $stats = $sample | ConvertFrom-Json -ErrorAction Stop
                    [pscustomobject]@{
                        capturedAtUtc = $capturedAtUtc
                        container     = $stats.Name
                        cpuPercent    = $stats.CPUPerc
                        memoryUsage   = $stats.MemUsage
                        memoryPercent = $stats.MemPerc
                        networkIo     = $stats.NetIO
                        blockIo       = $stats.BlockIO
                        pids          = $stats.PIDs
                    } | ConvertTo-Json -Compress | Add-Content -LiteralPath $OutputPath -Encoding utf8
                }
                catch {
                    Write-Error "Não foi possível interpretar a amostra do Docker: $sample"
                }
            }

            Start-Sleep -Seconds $SampleIntervalSeconds
        }
    } -ArgumentList $dockerCommand, $ResourceContainerNames, $resourcePath, $ResourceSampleIntervalSeconds
}

$k6ExitCode = 1

try {
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

    $k6ExitCode = $LASTEXITCODE
}
finally {
    if ($null -ne $resourceJob) {
        Stop-Job -Job $resourceJob -ErrorAction SilentlyContinue
        Receive-Job -Job $resourceJob -ErrorAction SilentlyContinue | Out-Null
        Remove-Job -Job $resourceJob -Force -ErrorAction SilentlyContinue
    }
}

if ($k6ExitCode -ne 0) {
    throw "O k6 reprovou thresholds ou não conseguiu executar o cenário. Consulte $artifactsPath."
}

Write-Host "Teste concluído. Resumo salvo em: $(Join-Path $artifactsPath $summaryFile)"

if ($CaptureResources) {
    Write-Host "Amostras de recursos salvas em: $resourcePath"
}
