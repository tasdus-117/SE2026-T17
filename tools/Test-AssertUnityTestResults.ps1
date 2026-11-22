# No Pester dependency: exercise the public CLI and its real process exit code.
$ErrorActionPreference = 'Stop'
$engine = (Get-Process -Id $PID).Path
$validator = Join-Path $PSScriptRoot 'Assert-UnityTestResults.ps1'
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('SmallArena-XmlGate-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
$pass = '<test-case fullname="SmallArena.Tests.Example.One" result="Passed" />'
$skip = '<test-case fullname="Vendor.KnownIgnored" result="Skipped" label="Ignored" />'
function Report([string]$Attributes = 'total="1" passed="1" failed="0" inconclusive="0" skipped="0"', [string]$Cases = $pass, [string]$Result = 'Passed') {
    return "<test-run result='$Result' $Attributes><test-suite result='Passed'>$Cases</test-suite></test-run>"
}
$valid = Report
$fixtures = @(
    @{ Name = 'valid report'; Xml = $valid; Pass = $true },
    @{ Name = 'minimum exactly met'; Xml = (Report 'total="2" passed="2" failed="0" inconclusive="0" skipped="0"' ($pass + $pass)); Min = 2; Pass = $true },
    @{ Name = 'mixed pass and two vendor skips'; Xml = (Report 'total="3" passed="1" failed="0" inconclusive="0" skipped="2"' ($pass + $skip + $skip) 'Skipped:Ignored'); Pass = $true },
    @{ Name = 'failed test'; Xml = (Report 'total="1" passed="0" failed="1" inconclusive="0" skipped="0"' '<test-case result="Failed" />' 'Failed'); Diagnostic = 'minimum' },
    @{ Name = 'failure with passed tests'; Xml = (Report 'total="2" passed="1" failed="1" inconclusive="0" skipped="0"' ($pass + '<test-case result="Failed" />')); Diagnostic = 'unexpected run status' },
    @{ Name = 'all skipped'; Xml = (Report 'total="1" passed="0" failed="0" inconclusive="0" skipped="1"' $skip 'Skipped:Ignored'); Diagnostic = 'Zero/all-skipped' },
    @{ Name = 'zero tests'; Xml = (Report 'total="0" passed="0" failed="0" inconclusive="0" skipped="0"' ''); Diagnostic = 'minimum' },
    @{ Name = 'inconclusive'; Xml = (Report 'total="2" passed="1" failed="0" inconclusive="1" skipped="0"' ($pass + '<test-case result="Inconclusive" />')); Diagnostic = 'inconclusive' },
    @{ Name = 'empty XML'; Xml = ''; Diagnostic = 'Empty Unity results' },
    @{ Name = 'whitespace XML'; Xml = ' '; Diagnostic = 'Malformed Unity XML' },
    @{ Name = 'corrupted XML'; Xml = '<test-run'; Diagnostic = 'Malformed Unity XML' },
    @{ Name = 'wrong root'; Xml = '<test-results />'; Diagnostic = 'test-run root' },
    @{ Name = 'missing XML'; Missing = 'editmode'; Diagnostic = 'Missing Unity results' },
    @{ Name = 'missing second platform'; Missing = 'playmode'; Diagnostic = 'Missing Unity results' },
    @{ Name = 'corrupt second platform'; Xml = $valid; CorruptSecond = $true; Diagnostic = 'Malformed Unity XML' },
    @{ Name = 'directory instead of report'; Directory = $true; Diagnostic = 'Missing Unity results' },
    @{ Name = 'missing passed count'; Xml = (Report 'total="1" failed="0" inconclusive="0" skipped="0"'); Diagnostic = 'missing' },
    @{ Name = 'nonnumeric count'; Xml = (Report 'total="one" passed="1" failed="0" inconclusive="0" skipped="0"'); Diagnostic = 'Invalid' },
    @{ Name = 'negative count'; Xml = (Report 'total="1" passed="1" failed="-1" inconclusive="0" skipped="0"'); Diagnostic = 'Invalid' },
    @{ Name = 'overflow count'; Xml = (Report 'total="9999999999999999999999" passed="1" failed="0" inconclusive="0" skipped="0"'); Diagnostic = 'Invalid' },
    @{ Name = 'below minimum'; Xml = $valid; Min = 2; Diagnostic = 'minimum=2' },
    @{ Name = 'invalid minimum zero'; Xml = $valid; Min = 0; Diagnostic = 'MinPassedCount' },
    @{ Name = 'failed leaf hidden by counters'; Xml = (Report 'total="2" passed="2" failed="0" inconclusive="0" skipped="0"' ($pass + '<test-case result="Failed" />')); Diagnostic = 'inconsistent' },
    @{ Name = 'inconclusive leaf hidden by counters'; Xml = (Report 'total="2" passed="2" failed="0" inconclusive="0" skipped="0"' ($pass + '<test-case result="Inconclusive" />')); Diagnostic = 'inconsistent' },
    @{ Name = 'unknown leaf result'; Xml = (Report 'total="2" passed="1" failed="0" inconclusive="0" skipped="1"' ($pass + '<test-case result="Error" />')); Diagnostic = 'inconsistent' },
    @{ Name = 'missing leaves'; Xml = (Report 'total="1" passed="1" failed="0" inconclusive="0" skipped="0"' ''); Diagnostic = 'inconsistent' },
    @{ Name = 'mismatched total'; Xml = (Report 'total="2" passed="1" failed="0" inconclusive="0" skipped="0"'); Diagnostic = 'inconsistent' },
    @{ Name = 'failed setup suite'; Xml = $valid.Replace("test-suite result='Passed'", "test-suite result='Failed'"); Diagnostic = 'test-suite result' },
    @{ Name = 'errored aggregate'; Xml = $valid.Replace('<test-run ', '<test-run errors="1" '); Diagnostic = 'error state' },
    @{ Name = 'errored leaf'; Xml = $valid.Replace('<test-case ', '<test-case errored="1" '); Diagnostic = 'error state' },
    @{ Name = 'error label hidden by Passed'; Xml = $valid.Replace('<test-case ', '<test-case label="Error" '); Diagnostic = 'failure/error detail' },
    @{ Name = 'hidden setup failure detail'; Xml = $valid.Replace('</test-suite>', '<failure><message>Setup failed</message></failure></test-suite>'); Diagnostic = 'failure/error detail' },
    @{ Name = 'unexpected run status'; Xml = (Report -Result 'Error'); Diagnostic = 'unexpected run status' },
    @{ Name = 'DTD prohibited'; Xml = '<!DOCTYPE test-run [<!ENTITY x "content">]>' + $valid; Diagnostic = 'Malformed Unity XML' }
)
$failed = 0
try {
    foreach ($fixture in $fixtures) {
        foreach ($platform in @('editmode', 'playmode')) {
            $path = Join-Path $scratch ($platform + '-results.xml')
            if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Recurse -Force }
            if ($fixture.Missing -eq $platform) { continue }
            if ($fixture.Directory -and $platform -eq 'editmode') { New-Item -ItemType Directory -Path $path | Out-Null; continue }
            $xml = if ($fixture.ContainsKey('Xml')) { $fixture.Xml } else { $valid }
            if ($fixture.CorruptSecond -and $platform -eq 'playmode') { $xml = '<broken' }
            [IO.File]::WriteAllText($path, $xml)
        }
        $minimum = if ($fixture.ContainsKey('Min')) { $fixture.Min } else { 1 }
        try {
            $ErrorActionPreference = 'Continue'
            $messages = @(& $engine -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $validator -ResultsRoot $scratch -MinPassedCount $minimum 2>&1)
            $code = $LASTEXITCODE
        } finally { $ErrorActionPreference = 'Stop' }
        $expected = if ($fixture.Pass) { $code -eq 0 } else { $code -ne 0 -and ($messages -join "`n") -like ('*' + $fixture.Diagnostic + '*') }
        if (-not $expected) { $failed++; Write-Output "FAIL: $($fixture.Name) (exit=$code)`n$($messages -join "`n")" }
        else { Write-Output "PASS: $($fixture.Name) (exit=$code)" }
    }
} finally {
    # Only this invocation's newly created temporary directory is ever removed.
    $resolved = [IO.Path]::GetFullPath($scratch)
    if ((Split-Path -Parent $resolved) -ne [IO.Path]::GetTempPath().TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Unsafe temporary cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
Write-Output "XML validator regression: total=$($fixtures.Count), passed=$($fixtures.Count - $failed), failed=$failed; $($PSVersionTable.PSVersion)"
if ($failed -gt 0) { exit 1 }
exit 0
