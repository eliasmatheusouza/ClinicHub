[CmdletBinding()]
param(
    [string]$HostUrl = 'http://localhost:9000',
    [string]$ProjectKey = 'clinichub',
    [string]$ProjectName = 'ClinicHub',
    [string]$Token = $env:SONAR_TOKEN
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw 'Informe -Token ou defina a variável de ambiente SONAR_TOKEN. Gere o token no SonarQube local; não o adicione ao Git.'
}

function Invoke-DotnetCommand {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "O comando 'dotnet $($Arguments -join ' ')' falhou com código $LASTEXITCODE."
    }
}

Invoke-DotnetCommand @('tool', 'restore')

$runId = Get-Date -Format 'yyyyMMddHHmmss'
$resultsDirectory = "artifacts/sonarqube-tests/$runId"

$commonArguments = @(
    "/k:$ProjectKey",
    "/n:$ProjectName",
    "/d:sonar.host.url=$HostUrl",
    "/d:sonar.token=$Token",
    "/d:sonar.cs.opencover.reportsPaths=$resultsDirectory/**/coverage.opencover.xml",
    "/d:sonar.cs.vstest.reportsPaths=$resultsDirectory/*.trx",
    '/d:sonar.exclusions=**/bin/**,**/obj/**,**/node_modules/**,**/dist/**,**/TestResults/**',
    '/d:sonar.scanner.scanAll=false',
    '/d:sonar.qualitygate.wait=true',
    '/d:sonar.qualitygate.timeout=300'
)

Invoke-DotnetCommand (@('sonarscanner', 'begin') + $commonArguments)
Invoke-DotnetCommand @('build', 'ClinicHub.sln', '--configuration', 'Release', '--no-restore', '--no-incremental')
Invoke-DotnetCommand @('test', 'ClinicHub.sln', '--configuration', 'Release', '--no-restore', '--collect', 'XPlat Code Coverage', '--logger', 'trx', '--settings', 'coverlet.sonarqube.runsettings', '--results-directory', $resultsDirectory)
Invoke-DotnetCommand (@('sonarscanner', 'end', "/d:sonar.token=$Token"))
