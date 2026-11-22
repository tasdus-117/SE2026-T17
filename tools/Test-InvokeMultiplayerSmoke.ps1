$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'MultiplayerSmokeChecks.ps1')
$passed = 0
function Check([string]$Name, [scriptblock]$Action, [bool]$ShouldFail = $false) {
    $caught = $false
    try { & $Action } catch { $caught = $true }
    if ($caught -ne $ShouldFail) { throw "FAIL: $Name (failure=$caught, expected=$ShouldFail)" }
    $script:passed++
    Write-Output "PASS: $Name"
}
Check 'exact marker' { if (-not (Test-SmokeMarker "[SMOKE_EVENT] HOST_STARTED role=host`n" 'HOST_STARTED')) { throw 'not found' } }
Check 'marker prefix rejected' { if (Test-SmokeMarker '[SMOKE_EVENT] HOST_STARTED_FAKE' 'HOST_STARTED') { throw 'false positive' } }
Check 'embedded marker rejected' { if (Test-SmokeMarker 'example [SMOKE_EVENT] HOST_STARTED' 'HOST_STARTED') { throw 'false positive' } }
Check 'complete synthetic lifecycle' { Assert-SmokeMarkers "[SMOKE_EVENT] HOST_STARTED`n[SMOKE_EVENT] COMBAT_VERIFIED`n[SMOKE_EVENT] CLEAN_SHUTDOWN`n" @('HOST_STARTED', 'COMBAT_VERIFIED', 'CLEAN_SHUTDOWN') }
Check 'missing marker' { Assert-SmokeMarkers '[SMOKE_EVENT] HOST_STARTED' @('HOST_STARTED','COMBAT_VERIFIED') } $true
Check 'empty/hang log' { Assert-SmokeMarkers '' @('CLEAN_SHUTDOWN') } $true
Check 'forward phase order' { Assert-SmokeOrder "[SMOKE_EVENT] MATCH_COUNTDOWN`n[SMOKE_EVENT] MATCH_IN_PROGRESS`n" @('MATCH_COUNTDOWN','MATCH_IN_PROGRESS') }
Check 'reversed phases rejected' { Assert-SmokeOrder "[SMOKE_EVENT] MATCH_IN_PROGRESS`n[SMOKE_EVENT] MATCH_COUNTDOWN`n" @('MATCH_COUNTDOWN','MATCH_IN_PROGRESS') } $true
foreach ($errorText in @('NullReferenceException: target', 'MissingReferenceException: object', 'System.InvalidOperationException: failure', 'Unhandled exception in player', 'Fatal error in player', 'Crash!!!', '[SMOKE_EVENT] FAILURE reason=deadline')) {
    Check ('reject ' + $errorText) { Assert-SmokeNoErrors $errorText } $true
}
Check 'normal diagnostic text' { Assert-SmokeNoErrors 'Mirror KCP connecting; Debug exception handling enabled' }
Check 'clean exit accepted' { Assert-SmokeExit 0 }
Check 'nonzero exit rejected' { Assert-SmokeExit 1 } $true
Check 'forced client exit accepted only explicitly' { Assert-SmokeExit -1 $true }
Check 'forced crash cannot be exit zero' { Assert-SmokeExit 0 $true } $true
Check 'deadline before bound' { Assert-SmokeDeadline 2 3 }
Check 'deadline at bound' { Assert-SmokeDeadline 3 3 } $true
Check 'independent watchdog expiry' { Assert-SmokeDeadline 0 90 $true } $true

if ($env:OS -eq 'Windows_NT') {
    if (-not ('SmallArena.Tools.SmokeProcessJob' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'SmokeProcessJob.cs') }
    $scratch = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) ('SmallArena-SmokeWatchdog-' + [guid]::NewGuid().ToString('N'))))
    if ((Split-Path -Parent $scratch) -ne [IO.Path]::GetTempPath().TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Unsafe temporary path.' }
    New-Item -ItemType Directory -Path $scratch | Out-Null
    $engine = (Get-Process -Id $PID).Path
    try {
        $job = New-Object SmallArena.Tools.SmokeProcessJob(2)
        $worker = $null
        try {
            $worker = $job.Start($engine, '-NoProfile -NonInteractive -Command "Start-Sleep -Seconds 60"', $scratch, (Join-Path $scratch 'hang.out'), (Join-Path $scratch 'hang.err'))
            if (-not $worker.WaitForExit(12000) -or -not $job.TimedOut -or $worker.ExitCode -ne 124) { throw 'Native watchdog failed to kill a hung child.' }
            Check 'real hung child killed by native watchdog (exit 124)' { Assert-SmokeExit $worker.ExitCode } $true
        } finally { $job.Dispose(); if ($null -ne $worker) { $worker.Dispose() } }
        $job = New-Object SmallArena.Tools.SmokeProcessJob(60)
        $worker = $null
        try {
            $worker = $job.Start($engine, '-NoProfile -NonInteractive -Command "Start-Sleep -Seconds 60"', $scratch, (Join-Path $scratch 'cleanup.out'), (Join-Path $scratch 'cleanup.err'))
            $job.Dispose()
            if (-not $worker.WaitForExit(5000)) { throw 'Job disposal left a live child.' }
            Check 'job disposal leaves no running child' { if (-not $worker.HasExited) { throw 'alive' } }
        } finally { $job.Dispose(); if ($null -ne $worker) { $worker.Dispose() } }
    } finally { Remove-Item -LiteralPath $scratch -Recurse -Force }
} else { Write-Output 'SKIP: native Windows Job Object tests; portable parser/exit/deadline tests ran.' }
Write-Output "Multiplayer smoke regression: passed=$passed, failed=0"
