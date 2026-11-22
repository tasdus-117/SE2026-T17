param(
    [string]$ResultsRoot = 'TestResults/CI',
    [ValidateRange(1, 2147483647)][int]$MinPassedCount = 1,
    [ValidateSet('editmode', 'playmode')][string[]]$TestPlatform = @('editmode', 'playmode')
)

$ErrorActionPreference = 'Stop'
foreach ($platform in $TestPlatform) {
    $platform = $platform.ToLowerInvariant()
    $path = Join-Path $ResultsRoot ($platform + '-results.xml')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing Unity results: $path" }
    if ((Get-Item -LiteralPath $path).Length -eq 0) { throw "Empty Unity results: $path" }
    $settings = New-Object System.Xml.XmlReaderSettings
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = $null
    try {
        $reader = [System.Xml.XmlReader]::Create((Get-Item -LiteralPath $path).FullName, $settings)
        $report = New-Object System.Xml.XmlDocument
        $report.XmlResolver = $null
        $report.Load($reader)
    } catch { throw "Malformed Unity XML ($platform): $path. $($_.Exception.Message)" }
    finally { if ($null -ne $reader) { $reader.Dispose() } }

    $run = $report.DocumentElement
    if ($run.Name -ne 'test-run') { throw "Expected NUnit test-run root: $path" }
    $counts = @{}
    foreach ($name in @('total', 'passed', 'failed', 'inconclusive', 'skipped')) {
        $count = 0L
        if (-not [long]::TryParse($run.GetAttribute($name), [ref]$count) -or $count -lt 0) {
            throw "Invalid or missing '$name' count in $path"
        }
        $counts[$name] = $count
    }
    if ($counts.passed -lt $MinPassedCount) {
        throw "Unity $platform passed=$($counts.passed); minimum=$MinPassedCount. Zero/all-skipped runs are not success: $path"
    }
    if ($counts.failed -ne 0 -or $counts.inconclusive -ne 0 -or
        $run.GetAttribute('result') -notin @('Passed', 'Skipped:Ignored')) {
        throw "Unity $platform has failed, inconclusive, or unexpected run status: $path"
    }
    foreach ($node in $report.SelectNodes('//*[@errors or @errored]')) {
        foreach ($attribute in @('errors', 'errored')) {
            if ($node.HasAttribute($attribute) -and $node.GetAttribute($attribute) -ne '0') {
                throw "Unexpected error state ($attribute): $path"
            }
        }
    }
    # Aggregate success must not hide failed setup/teardown or failing leaves.
    if ($report.SelectNodes('//failure|//*[@label="Error" or @label="Invalid" or @label="Cancelled"]').Count -gt 0) {
        throw "Unexpected failure/error detail: $path"
    }
    if ($report.SelectNodes('//test-suite[@result and @result!="Passed" and @result!="Skipped" and @result!="Skipped:Ignored"]').Count -gt 0) {
        throw "Unexpected test-suite result: $path"
    }
    $cases = @($report.SelectNodes('//test-case'))
    $passed = @($cases | Where-Object { $_.GetAttribute('result') -eq 'Passed' }).Count
    $skipped = @($cases | Where-Object { $_.GetAttribute('result') -in @('Skipped', 'Skipped:Ignored') }).Count
    if ($cases.Count -ne $counts.total -or $passed -ne $counts.passed -or
        $skipped -ne $counts.skipped -or $passed + $skipped -ne $cases.Count -or
        $counts.total -ne $counts.passed + $counts.skipped) {
        throw "Unity $platform has inconsistent counters, missing cases, or unexpected test-case states: $path"
    }
    $projectPassed = @($cases | Where-Object { $_.GetAttribute('result') -eq 'Passed' -and $_.GetAttribute('fullname').StartsWith('SmallArena.Tests.') }).Count
    Write-Output "$platform`: total=$($counts.total), passed=$passed (project=$projectPassed), failed=0, inconclusive=0, skipped=$skipped; minimum=$MinPassedCount"
}
