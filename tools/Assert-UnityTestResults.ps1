[CmdletBinding(DefaultParameterSetName = 'Platforms')]
param(
    [Parameter(ParameterSetName = 'Platforms')]
    [string]$ResultsRoot = 'TestResults/CI',
    [ValidateRange(1, 2147483647)][int]$MinPassedCount = 1,
    [Parameter(ParameterSetName = 'Platforms')]
    [ValidateSet('editmode', 'playmode')][string[]]$TestPlatform = @('editmode', 'playmode'),
    [Parameter(Mandatory = $true, ParameterSetName = 'Xml')][string]$XmlPath,
    [ValidateRange(0, 2147483647)][int]$MinProjectPassedCount = 0,
    [string[]]$AllowedVendorSkipPrefixes = @('UnityEngine.', 'Unity.', 'UnityEditor.'),
    [ValidateRange(0, 2147483647)][int]$MaxVendorSkippedCount = 10,
    [bool]$RequireSkipReason = $true
)

$ErrorActionPreference = 'Stop'
function Test-Prefix([string]$Value, [string[]]$Prefixes) {
    foreach ($prefix in $Prefixes) {
        if (-not [string]::IsNullOrWhiteSpace($prefix) -and $Value.StartsWith($prefix, [StringComparison]::Ordinal)) { return $true }
    }
    return $false
}
$reports = if ($PSCmdlet.ParameterSetName -eq 'Xml') {
    @(@{ Platform = 'xml'; Path = $XmlPath })
} else {
    @($TestPlatform | ForEach-Object { @{ Platform = $_.ToLowerInvariant(); Path = (Join-Path $ResultsRoot ($_.ToLowerInvariant() + '-results.xml')) } })
}
foreach ($entry in $reports) {
    $platform = $entry.Platform
    $path = $entry.Path
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
    $skipped = @($cases | Where-Object { $_.GetAttribute('result').StartsWith('Skipped', [StringComparison]::Ordinal) }).Count
    if ($cases.Count -ne $counts.total -or $passed -ne $counts.passed -or
        $skipped -ne $counts.skipped -or $passed + $skipped -ne $cases.Count -or
        $counts.total -ne $counts.passed + $counts.skipped) {
        throw "Unity $platform has inconsistent counters, missing cases, or unexpected test-case states: $path"
    }
    $projectPassed = 0; $vendorPassed = 0; $vendorSkipped = 0
    $projectSkips = @()
    foreach ($case in $cases) {
        $identities = @($case.GetAttribute('fullname'), $case.GetAttribute('classname'))
        $assemblies = @($case.SelectNodes('ancestor::test-suite[@type="Assembly"]') | ForEach-Object { $_.GetAttribute('name') })
        $identities += $assemblies
        $project = @($identities | Where-Object { Test-Prefix $_ @('SmallArena.Tests.') }).Count -gt 0
        # The two real repository asmdefs predate the SmallArena.Tests.* assembly convention.
        if (@($assemblies | Where-Object { [IO.Path]::GetFileNameWithoutExtension($_) -in @('SmallArena.EditMode.Tests', 'SmallArena.PlayMode.Tests') }).Count) { $project = $true }
        $display = $case.GetAttribute('fullname')
        if (-not $display) { $display = $case.GetAttribute('name') }
        if (-not $display) { $display = '(unnamed test)' }
        if ($case.GetAttribute('result') -eq 'Passed') {
            if ($project) { $projectPassed++ } else { $vendorPassed++ }
            continue
        }
        if ($project) { $projectSkips += $display; continue }
        $vendorSkipped++
        if (@($identities | Where-Object { Test-Prefix $_ $AllowedVendorSkipPrefixes }).Count -eq 0) {
            throw "Unapproved vendor skip: $display ($path)"
        }
        $reason = $case.SelectSingleNode('reason/message')
        if ($RequireSkipReason -and ($null -eq $reason -or [string]::IsNullOrWhiteSpace($reason.InnerText))) {
            throw "Missing vendor skip reason: $display ($path)"
        }
    }
    if ($projectSkips.Count -gt 0) { throw "Skipped project tests are forbidden: $($projectSkips -join ', ') ($path)" }
    if ($vendorSkipped -gt $MaxVendorSkippedCount) { throw "Vendor skip budget exceeded: $vendorSkipped > $MaxVendorSkippedCount ($path)" }
    if ($projectPassed -lt $MinProjectPassedCount) { throw "Unity $platform projectPassed=$projectPassed; project minimum=$MinProjectPassedCount ($path)" }
    Write-Output "$platform`: total=$($counts.total), passed=$passed, projectPassed=$projectPassed, vendorPassed=$vendorPassed, vendorSkipped=$vendorSkipped, failed=0, inconclusive=0; minimum=$MinPassedCount; projectMinimum=$MinProjectPassedCount"
}
