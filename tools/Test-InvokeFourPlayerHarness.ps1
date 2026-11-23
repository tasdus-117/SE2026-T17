$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'FourPlayerHarnessChecks.ps1')
$passed = 0
function Check([string]$Name, [scriptblock]$Action, [bool]$ShouldFail = $false) {
    $caught = $false; $detail = ''
    try { & $Action } catch { $caught = $true; $detail = $_.Exception.Message }
    if ($caught -ne $ShouldFail) { throw "FAIL: $Name (failure=$caught, expected=$ShouldFail): $detail" }
    $script:passed++; Write-Output "PASS: $Name"
}
function Frames {
    $actors = @(0..3 | ForEach-Object { @{ slot = $_; hp = 10; damage = 1; life = 1; lifeState = 'Alive'; canAct = $true; result = 'Playing' } })
    $scores = @(0..3 | ForEach-Object { @{ participantId = "p$_"; slot = $_; rank = $_ + 1; kills = 0; deaths = 0; damage = 0; life = 1; lifeState = 'Alive'; connected = $true } })
    return @(0..3 | ForEach-Object { @{ phase = 'InProgress'; avatars = 4; localSlot = $_; session = 'match'; clock = 60; actors = $actors; scores = $scores; winner = '' } } | ConvertTo-Json -Depth 8 | ConvertFrom-Json)
}
Check 'runner PowerShell syntax' {
    $tokens = $null; $errors = $null
    [void][Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Invoke-FourPlayerHarness.ps1'), [ref]$tokens, [ref]$errors)
    if (@($errors).Count) { throw ($errors -join '; ') }
}
function FinalFrame {
    $f = (Frames)[0]; $f.phase = 'Ended'; $f.winner = 'p0'
    $f.scores[0].kills = 5; $f.scores[0].damage = 50; $f.scores[1].deaths = 5
    $f.scores[1].rank = 4; $f.scores[2].rank = 2; $f.scores[3].rank = 3
    foreach ($actor in $f.actors) { $actor.canAct = $false; $actor.result = if ($actor.slot -eq 0) { 'Victory' } else { 'Defeat' } }
    return $f
}
Check 'independent final accounting/ranking oracle' { Assert-FourPlayerFinalScores (FinalFrame) 1 }
Check 'incorrect actual damage rejected' { $f = FinalFrame; $f.scores[0].damage = 49; Assert-FourPlayerFinalScores $f 1 } $true
Check 'slot tie break rejected when reversed' { $f = FinalFrame; $f.scores[2].rank = 3; $f.scores[3].rank = 2; Assert-FourPlayerFinalScores $f 1 } $true
Check 'four matching observers' { Assert-FourPlayerConvergence (Frames) }
Check 'three observers rejected' { Assert-FourPlayerConvergence @((Frames)[0..2]) } $true
Check 'wrong phase rejected' { $f = Frames; $f[2].phase = 'Countdown'; Assert-FourPlayerConvergence $f } $true
Check 'wrong session rejected' { $f = Frames; $f[1].session = 'old'; Assert-FourPlayerConvergence $f } $true
Check 'ownership collision rejected' { $f = Frames; $f[3].localSlot = 0; Assert-FourPlayerConvergence $f } $true
Check 'clock tolerance accepted' { $f = Frames; $f[3].clock = 59.3; Assert-FourPlayerConvergence $f }
Check 'clock spread rejected' { $f = Frames; $f[3].clock = 58; Assert-FourPlayerConvergence $f } $true
Check 'nonfinite clock rejected' { $f = Frames; $f[0].clock = [double]::NaN; Assert-FourPlayerConvergence $f } $true
Check 'score desync rejected' { $f = Frames; $f[2].scores[0].kills = 1; Assert-FourPlayerConvergence $f } $true
Check 'HP desync rejected' { $f = Frames; $f[3].actors[1].hp = 9; Assert-FourPlayerConvergence $f } $true
Check 'out of bounds HP rejected' { $f = Frames; $f[0].actors[0].hp = 11; Assert-FourPlayerConvergence $f } $true
Check 'fresh life mismatch rejected' { $f = Frames; $f[2].actors[1].life = 2; Assert-FourPlayerConvergence $f } $true
Check 'winner desync rejected' { $f = Frames; $f[0].winner = 'p0'; Assert-FourPlayerConvergence $f } $true
Check 'exact event parsing' { Assert-FourPlayerEvents "{`"event`":`"MENU_ENTRY`"}`n{`"event`":`"CLEAN_SHUTDOWN`"}`n" @('MENU_ENTRY','CLEAN_SHUTDOWN') }
Check 'event prefix does not pass' { Assert-FourPlayerEvents '{"event":"CLEAN_SHUTDOWN_FAKE"}' @('CLEAN_SHUTDOWN') } $true
Check 'malformed event rejected' { Assert-FourPlayerEvents '{bad' @('MENU_ENTRY') } $true
Check 'empty/hung worker log rejected' { Assert-FourPlayerEvents '' @('CLEAN_SHUTDOWN') } $true
foreach ($message in @('NullReferenceException: target','MissingReferenceException: target','Unhandled exception','Fatal error','Crash!!!')) {
    Check ('runtime error ' + $message) { Assert-FourPlayerLog $message } $true
}
Check 'ordinary diagnostic accepted' { Assert-FourPlayerLog 'Mirror KCP connected; exception handling enabled' }
Check 'before deadline' { Assert-FourPlayerDeadline 1 2 }
Check 'at deadline rejected' { Assert-FourPlayerDeadline 2 2 } $true
Check 'watchdog expiry rejected' { Assert-FourPlayerDeadline 0 2 $true } $true
Check 'NaN elapsed rejected' { Assert-FourPlayerDeadline ([double]::NaN) 2 } $true
Check 'infinite elapsed rejected' { Assert-FourPlayerDeadline ([double]::PositiveInfinity) 2 } $true
Check 'disk space floor accepted' { Assert-FourPlayerStorage 512MB }
Check 'insufficient artifact storage rejected' { Assert-FourPlayerStorage 1MB } $true
Check 'explicit I/O failure remains infrastructure' { if ((Get-FourPlayerFailureCategory '[FOUR_PLAYER_FAILURE] Infrastructure: IOException: write') -ne 'Infrastructure') { throw 'wrong category' } }
Check 'product failure marker remains product' { if ((Get-FourPlayerFailureCategory '[FOUR_PLAYER_FAILURE] Product: invalid health') -ne 'Product') { throw 'wrong category' } }
$scratch = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) ('SmallArena-FourPlayer-' + [guid]::NewGuid().ToString('N'))))
if ((Split-Path -Parent $scratch) -ne [IO.Path]::GetTempPath().TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Unsafe temporary test path.' }
New-Item -ItemType Directory -Path $scratch | Out-Null
try {
    $file = Join-Path $scratch 'sample.json'; [IO.File]::WriteAllText($file, '{"value":1}')
    Check 'JSON parser reads a complete snapshot' { if ((Read-FourPlayerJson $file).value -ne 1) { throw 'wrong value' } }
    Check 'missing snapshot remains pending' { if ($null -ne (Read-FourPlayerJson (Join-Path $scratch 'absent'))) { throw 'not pending' } }
    Check 'missing snapshot directory remains pending' { if ($null -ne (Read-FourPlayerJson (Join-Path $scratch 'absent/child.json'))) { throw 'not pending' } }
    Check 'temporarily locked snapshot remains pending' {
        $locked = [IO.File]::Open($file, 'Open', 'ReadWrite', 'None')
        try { if ($null -ne (Read-FourPlayerJson $file)) { throw 'locked file was read' } } finally { $locked.Dispose() }
    }
    Check 'atomic command publication is immutable' {
        $commandPath = Join-Path $scratch 'command.json'
        Write-FourPlayerJson $commandPath @{ sequence = 1 }
        if ((Read-FourPlayerJson $commandPath).sequence -ne 1) { throw 'publication failed' }
    }
    Check 'duplicate sequence cannot overwrite committed command' { Write-FourPlayerJson (Join-Path $scratch 'command.json') @{ sequence = 2 } } $true
    $cursor = @{}; $streamFile = Join-Path $scratch 'stream.jsonl'
    [IO.File]::WriteAllText($streamFile, '{"sequence":1}')
    Check 'partial JSONL record remains pending' { if ($null -ne (Read-FourPlayerStream $streamFile $cursor)) { throw 'partial accepted' } }
    [IO.File]::AppendAllText($streamFile, "`n{`"sequence`":2}`n")
    Check 'complete JSONL records advance to latest' { if ((Read-FourPlayerStream $streamFile $cursor).sequence -ne 2) { throw 'wrong stream baseline' } }
    Check 'unchanged stream does not replay an old record' { if ($null -ne (Read-FourPlayerStream $streamFile $cursor)) { throw 'duplicate stream record' } }
    Check 'malformed earlier JSONL record cannot hide behind later success' {
        $bad = Join-Path $scratch 'bad-stream.jsonl'; [IO.File]::WriteAllText($bad, "{broken}`n{`"sequence`":3}`n")
        Read-FourPlayerStream $bad @{}
    } $true
    $prefix = Join-Path $scratch 'segment-'
    [IO.File]::WriteAllText(($prefix + '0.jsonl'), "{`"sequence`":1}`n")
    [IO.File]::WriteAllText(($prefix + '1.jsonl'), "{`"sequence`":2}`n")
    $segments = @{}
    Check 'rotating stream crosses a completed segment' { if ((Read-FourPlayerSegments $prefix $segments).sequence -ne 2 -or $segments.Segment -ne 1) { throw 'lost segment transition' } }
    Check 'completed rotating stream does not replay records' { if ($null -ne (Read-FourPlayerSegments $prefix $segments)) { throw 'replayed segment' } }
    $cache = @{}; $sample = [pscustomobject]@{ sequence = 2 }
    $null = Resolve-FourPlayerFrame $cache $sample 0
    Check 'brief read gap retains a validated sample' { if ((Resolve-FourPlayerFrame $cache $null .1).sequence -ne 2) { throw 'sample lost' } }
    Check 'older sample cannot replace a newer sample' { if ((Resolve-FourPlayerFrame $cache ([pscustomobject]@{sequence=1}) .2).sequence -ne 2) { throw 'sample regressed' } }
    Check 'stale cached sample fails bounded freshness' { Resolve-FourPlayerFrame $cache $null 10.01 } $true
    $hash = Get-FourPlayerContentHash $scratch
    [IO.File]::WriteAllText((Join-Path $scratch 'four-player-build.json'), '{"metadata":true}')
    Check 'sidecar excluded from content hash' { if ((Get-FourPlayerContentHash $scratch) -ne $hash) { throw 'sidecar changed hash' } }
    [IO.File]::WriteAllText($file, '{"value":2}')
    Check 'data modification changes build identity' { if ((Get-FourPlayerContentHash $scratch) -eq $hash) { throw 'tampering undetected' } }
    if ($env:OS -eq 'Windows_NT') {
        if (-not ('SmallArena.Tools.FourPlayerProcessJob' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'FourPlayerProcessJob.cs') }
        $engine = (Get-Process -Id $PID).Path
        $job = New-Object SmallArena.Tools.FourPlayerProcessJob(2)
        $owned = $null
        try {
            $owned = $job.Start($engine, '-NoProfile -NonInteractive -Command "Start-Sleep -Seconds 60"', $scratch, (Join-Path $scratch 'owned.out'), (Join-Path $scratch 'owned.err'))
            if (-not $owned.WaitForExit(12000)) { throw 'Native watchdog failed.' }
            Check 'real owned hang receives watchdog exit 124' { if ($owned.ExitCode -ne 124 -or -not $job.TimedOut) { throw "wrong watchdog exit=$($owned.ExitCode), expired=$($job.TimedOut)" } }
        } finally { $job.Dispose(); if ($null -ne $owned) { $owned.Dispose() } }
        $outside = Start-Process -FilePath $engine -ArgumentList '-NoProfile -NonInteractive -Command "Start-Sleep -Seconds 30"' -WindowStyle Hidden -PassThru
        $job = New-Object SmallArena.Tools.FourPlayerProcessJob(60)
        $owned = $null
        try {
            $owned = $job.Start($engine, '-NoProfile -NonInteractive -Command "Start-Sleep -Seconds 60"', $scratch, (Join-Path $scratch 'second.out'), (Join-Path $scratch 'second.err'))
            $job.Dispose()
            Check 'job disposal removes owned PID' { if (-not $owned.WaitForExit(5000)) { throw 'owned child survived' } }
            Check 'job disposal preserves a PID outside its membership' { if ($outside.HasExited) { throw 'unrelated child killed' } }
        } finally {
            $job.Dispose(); if ($null -ne $owned) { $owned.Dispose() }
            if (-not $outside.HasExited) { $outside.Kill(); $null = $outside.WaitForExit(5000) }; $outside.Dispose()
        }
    } else { Write-Output 'NOT TESTED: native Windows Job Object checks on non-Windows host.' }
} finally { Remove-Item -LiteralPath $scratch -Recurse -Force }
Write-Output "Four-player harness regression: passed=$passed, failed=0"
exit 0
