[CmdletBinding()]
param(
    [ValidateRange(0, 100)]
    [double]$MinimumLineRate = 70,

    [string[]]$TestProjects = @(
        'ClinicHub.Domain.Tests',
        'ClinicHub.Application.Tests'
    )
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()

foreach ($testProject in $TestProjects) {
    $resultsDirectory = Join-Path $repositoryRoot "tests/$testProject/TestResults"
    $coverageFile = Get-ChildItem -LiteralPath $resultsDirectory -Recurse -File -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $coverageFile) {
        $failures.Add("Nenhum relatório Cobertura foi encontrado para $testProject.")
        continue
    }

    [xml]$coverage = Get-Content -LiteralPath $coverageFile.FullName -Raw
    $lineRate = [double]$coverage.coverage.'line-rate' * 100
    Write-Host ("{0}: {1:N2}% de cobertura de linhas (meta: {2:N2}%)." -f $testProject, $lineRate, $MinimumLineRate)

    if ($lineRate -lt $MinimumLineRate) {
        $failures.Add(("$testProject ficou abaixo da meta: {0:N2}% < {1:N2}%." -f $lineRate, $MinimumLineRate))
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Quality gate de cobertura aprovado.'
