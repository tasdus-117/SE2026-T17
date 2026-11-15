param([string]$ResultsRoot = 'TestResults/CI')
$ErrorActionPreference = 'Stop'
foreach ($platform in @('editmode', 'playmode')) {
    $path = Join-Path $ResultsRoot ($platform + '-results.xml')
    if (-not (Test-Path -LiteralPath $path)) { throw ('Missing Unity results: ' + $path) }
    [xml]$report = Get-Content -LiteralPath $path -Raw
    $run = $report.'test-run'
    if ($null -eq $run -or [int]$run.total -le 0 -or [int]$run.failed -ne 0 -or [int]$run.inconclusive -ne 0 -or
        $run.result -notin @('Passed', 'Skipped:Ignored') -or
        $report.SelectNodes('//test-case[@result="Failed" or @result="Inconclusive"]').Count -gt 0) {
        throw ('Unity ' + $platform + ' has failing, inconclusive, or invalid results: ' + $path)
    }
    Write-Output ($platform + ': passed=' + $run.passed + ', failed=' + $run.failed + ', skipped=' + $run.skipped)
}
