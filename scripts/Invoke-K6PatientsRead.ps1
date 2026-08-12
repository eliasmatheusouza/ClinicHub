[CmdletBinding()]
param(
    [ValidateSet('patients-read', 'appointments-lifecycle')]
    [string] $Scenario = 'patients-read',
    [ValidateSet('smoke', 'baseline')]
    [string] $Profile = 'smoke',
    [string] $BaseUrl = 'http://host.docker.internal:8082',
    [Parameter(Mandatory)]
    [string] $UserEmail,
    [Parameter(Mandatory)]
    [string] $UserPassword,
    [string] $AccessToken,
    [ValidateSet('warm', 'cold')]
    [string] $CacheState = 'warm',
    [string] $RedisContainerName = 'clinichub-redis-1',
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
$scenarioPath = Join-Path $projectRoot "performance/k6/$Scenario.js"
$artifactsPath = Join-Path $projectRoot 'artifacts/performance'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$summaryFile = "$Scenario-$Profile-$timestamp.json"
$resourceFile = "$Scenario-$Profile-$timestamp-resources.jsonl"

if (-not (Test-Path -LiteralPath $scenarioPath)) {
    throw "Cenário k6 não encontrado: $scenarioPath"
}

New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null

function Reset-PatientListCache {
    $runningContainers = @(docker ps --format '{{.Names}}')
    if ($RedisContainerName -notin $runningContainers) {
        throw "Não foi possível preparar cache frio. Contêiner Redis ausente: $RedisContainerName."
    }

    $cacheKeys = @(docker exec $RedisContainerName redis-cli --scan --pattern 'patients:list:*')
    if ($LASTEXITCODE -ne 0) {
        throw 'Não foi possível listar as chaves de cache de pacientes no Redis.'
    }

    foreach ($cacheKey in $cacheKeys) {
        docker exec $RedisContainerName redis-cli DEL $cacheKey | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Não foi possível remover a chave de cache de pacientes: $cacheKey"
        }
    }

    Write-Host "Cache frio preparado: $($cacheKeys.Count) chave(s) de patients:list removida(s)."
}

function Warm-PatientListCache {
    $hostBaseUrl = $BaseUrl -replace 'host\.docker\.internal', 'localhost'
    $token = $AccessToken
    if ([string]::IsNullOrWhiteSpace($token)) {
        $loginPayload = @{ email = $UserEmail; password = $UserPassword } | ConvertTo-Json -Compress
        $login = Invoke-RestMethod -Uri "$hostBaseUrl/api/auth/login" -Method Post -ContentType 'application/json' -Body $loginPayload
        $token = $login.accessToken
    }

    if ([string]::IsNullOrWhiteSpace($token)) {
        throw 'Não foi possível preparar cache quente: nenhum access token disponível.'
    }

    $response = Invoke-WebRequest -Uri "$hostBaseUrl/api/patients?page=1&pageSize=20" `
        -Headers @{ Authorization = "Bearer $token" } `
        -UseBasicParsing

    if ($response.StatusCode -ne 200) {
        throw "Não foi possível preparar cache quente: listagem retornou HTTP $($response.StatusCode)."
    }

    Write-Host 'Cache quente preparado: listagem autenticada de pacientes executada antes do k6.'
}

if ($Scenario -eq 'patients-read') {
    if ($CacheState -eq 'cold') {
        Reset-PatientListCache
    }
    else {
        Warm-PatientListCache
    }
}

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
            -e "PERF_ACCESS_TOKEN=$AccessToken" `
            -e "PERF_CACHE_STATE=$CacheState" `
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
