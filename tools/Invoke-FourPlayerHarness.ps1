param(
    [string]$Player = 'TestResults/FourPlayerHarness/ValidationProject/Builds/WindowsStandalone/SmallArena.exe',
    [ValidateRange(1, 10)][int]$Rounds = 3,
    [ValidateRange(0, 65535)][int]$Port = 0,
    [ValidateRange(60, 3600)][int]$TimeoutSeconds = 900,
    [ValidateRange(5, 180)][int]$StepTimeoutSeconds = 90,
    [string]$ExpectedCommit = '8fba1c127f650615faec4f14b34b16da7552c858'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'FourPlayerHarnessChecks.ps1')
if ($env:OS -ne 'Windows_NT') { throw 'Infrastructure: standalone harness requires Windows 10+/Server 2016+.' }
if (-not ('SmallArena.Tools.FourPlayerProcessJob' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'FourPlayerProcessJob.cs') }
$root = Split-Path -Parent $PSScriptRoot
$playerPath = [IO.Path]::GetFullPath((Join-Path $root $Player))
$buildDirectory = Split-Path -Parent $playerPath
$identity = Read-FourPlayerJson (Join-Path $buildDirectory 'four-player-build.json')
if ($null -eq $identity -or -not $identity.Instrumented -or $identity.Commit -ne $ExpectedCommit) { throw 'Infrastructure: missing/mismatched instrumented build identity.' }
$binaryHash = (Get-FileHash -LiteralPath $playerPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($binaryHash -ne $identity.BinarySHA256 -or (Get-FourPlayerContentHash $buildDirectory) -ne $identity.ContentSHA256) { throw 'Infrastructure: executable/data content hashes do not match the build identity.' }
$run = [guid]::NewGuid().ToString('N')
$directory = Join-Path $root ('TestResults/FourPlayerHarness/Runs/' + $run)
Assert-FourPlayerStorage ([IO.DriveInfo]::new([IO.Path]::GetPathRoot($directory)).AvailableFreeSpace)
New-Item -ItemType Directory -Path $directory -Force | Out-Null
$socket = New-Object Net.Sockets.UdpClient(0)
if ($Port -eq 0) { $Port = $socket.Client.LocalEndPoint.Port }
$socket.Dispose()
function Assert-PortReleased {
    $udp = New-Object Net.Sockets.UdpClient([Net.Sockets.AddressFamily]::InterNetwork)
    try { $udp.Client.ExclusiveAddressUse = $true; $udp.Client.Bind((New-Object Net.IPEndPoint([Net.IPAddress]::Any, $Port))) }
    finally { $udp.Dispose() }
}
Assert-PortReleased
$workers = @{}; $commands = @{}; $seen = @{}; $streams = @{}; $frameCaches = @{}; $observations = New-Object Collections.Generic.List[object]
$summary = [ordered]@{ Run = $run; Commit = $ExpectedCommit; BinarySHA256 = $binaryHash; ContentSHA256 = $identity.ContentSHA256;
    BuildToolingSHA256 = $identity.ToolingSHA256; Oracle = 'PENDING_TV2_ORACLE (#24)'; Topology = '1 host + 3 independent clients; localhost KCP';
    Port = $Port; RoundsRequested = $Rounds; RoundsCompleted = 0; Result = 'Failed'; FailureCategory = ''; Error = ''; Cleanup = $false; Workers = @(); Observations = @() }
$summary.RunnerSources = @('Invoke-FourPlayerHarness.ps1','FourPlayerHarnessChecks.ps1','FourPlayerProcessJob.cs','Test-InvokeFourPlayerHarness.ps1') |
    ForEach-Object { @{ File = ('tools/' + $_); SHA256 = (Get-FileHash (Join-Path $PSScriptRoot $_) -Algorithm SHA256).Hash.ToLowerInvariant() } }
$sourceArchive = Join-Path $directory 'runner-source'; New-Item -ItemType Directory -Path $sourceArchive | Out-Null
foreach ($entry in $summary.RunnerSources) { Copy-Item -LiteralPath (Join-Path $root $entry.File) -Destination (Join-Path $sourceArchive (Split-Path -Leaf $entry.File)) }
$buildSourceArchive = Join-Path $directory 'build-source'; New-Item -ItemType Directory -Path $buildSourceArchive | Out-Null
foreach ($entry in $identity.ToolingFiles) {
    $source = Join-Path $root $entry.File
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.SHA256) { throw 'Infrastructure: build instrumentation sources changed; rebuild before running.' }
    Copy-Item -LiteralPath $source -Destination (Join-Path $buildSourceArchive (Split-Path -Leaf $source))
}
$identity | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'build-identity.json') -Encoding UTF8
$watch = [Diagnostics.Stopwatch]::StartNew()
$job = New-Object SmallArena.Tools.FourPlayerProcessJob($TimeoutSeconds)
function Frame([int]$Id) {
    $prefix = Join-Path $directory ("stream-$Id-")
    if (-not $streams.ContainsKey($Id)) { $streams[$Id] = @{}; $frameCaches[$Id] = @{} }
    $frame = Read-FourPlayerSegments $prefix $streams[$Id]
    if ($null -ne $frame) {
        if ($frame.run -ne $run -or $frame.commit -ne $ExpectedCommit -or $frame.tooling -ne $identity.ToolingSHA256 -or $frame.process -ne $workers[$Id].Id) { throw "Infrastructure: stale/wrong worker identity $Id" }
        if ($frame.fault) {
            $kind = if ($frame.fault.StartsWith('Infrastructure:')) { 'Infrastructure' } else { 'Product' }
            throw "${kind}: worker $Id reported $($frame.fault)"
        }
        $sourceFile = Get-Item -LiteralPath ($prefix + $streams[$Id].Segment + '.jsonl')
        if (((Get-Date).ToUniversalTime() - $sourceFile.LastWriteTimeUtc).TotalSeconds -gt 10) { throw "Infrastructure: worker $Id artifact stream is stale." }
        if (-not $seen.ContainsKey($Id) -or $frame.sequence -gt $seen[$Id]) {
            $seen[$Id] = $frame.sequence
            $frame | ConvertTo-Json -Compress -Depth 8 | Add-Content -LiteralPath (Join-Path $directory ("observed-$Id.jsonl"))
        }
    }
    return Resolve-FourPlayerFrame $frameCaches[$Id] $frame $watch.Elapsed.TotalSeconds
}
function Actor([int]$Observer, [int]$Slot) { $f = Frame $Observer; if ($null -ne $f) { return @($f.actors | Where-Object slot -eq $Slot)[0] } }
function Wait-State([string]$Label, [scriptblock]$Predicate, [string]$Category = 'Product') {
    $step = [Diagnostics.Stopwatch]::StartNew()
    while ($true) {
        Assert-FourPlayerDeadline $watch.Elapsed.TotalSeconds $TimeoutSeconds $job.TimedOut
        foreach ($id in $workers.Keys) {
            if ($workers[$id].HasExited) {
                $logs = @("player-$id.log", "player-$id.stderr.log") | ForEach-Object { $p = Join-Path $directory $_; if (Test-Path -LiteralPath $p) { [IO.File]::ReadAllText($p) } }
                $kind = Get-FourPlayerFailureCategory ($logs -join "`n")
                throw "${kind}: worker $id exited early ($($workers[$id].ExitCode)); step=$Label; inspect its player/stderr logs."
            }
        }
        if (& $Predicate) { Write-Host "PASS: $Label"; return }
        if ($step.Elapsed.TotalSeconds -ge $StepTimeoutSeconds) {
            $available = @($workers.Keys | ForEach-Object { Frame $_ } | Where-Object { $null -ne $_ }).Count
            $kind = if ($available -lt $workers.Count) { 'Infrastructure' } else { $Category }
            throw "${kind}: step timeout: $Label; fresh worker snapshots=$available/$($workers.Count); inspect $directory"
        }
        Start-Sleep -Milliseconds 100
    }
}
function Send([int]$Id, [string]$Action, [hashtable]$Fields = @{}, [bool]$SkipAcknowledgement = $false) {
    if ($Action -in @('arrange','loot') -and ($null -eq $Fields['slot'] -or $Fields['slot'] -notin @(1,2,3))) { throw 'Infrastructure: fixture requires an explicitly observed non-host slot.' }
    if (-not $commands.ContainsKey($Id)) { $commands[$Id] = 0 }
    $commands[$Id]++
    $payload = @{ sequence = $commands[$Id]; action = $Action }
    foreach ($key in $Fields.Keys) { $payload[$key] = $Fields[$key] }
    $path = Join-Path $directory ("command-$Id-$($commands[$Id]).json")
    Write-FourPlayerJson $path $payload
    $payload | ConvertTo-Json -Compress | Add-Content -LiteralPath (Join-Path $directory ("commands-$Id.jsonl"))
    if (-not $SkipAcknowledgement) { Wait-State "worker $Id acknowledged $Action" { $f = Frame $Id; $null -ne $f -and $f.command -eq $commands[$Id] } }
}
function Converge([string]$Phase) {
    Wait-State "four observers converge: $Phase" {
        $frames = @(0..3 | ForEach-Object { Frame $_ } | Where-Object { $null -ne $_ })
        try { Assert-FourPlayerConvergence $frames $Phase; return $true }
        catch { if ($_.Exception.Message.StartsWith('Not converged:')) { return $false }; throw }
    }
    $frames = @(0..3 | ForEach-Object { Frame $_ }); $clock = $frames | Measure-Object clock -Minimum -Maximum
    $observations.Add(@{ Phase = $Phase; Session = $frames[0].session; ClockSpread = $clock.Maximum - $clock.Minimum; Frames = $frames })
}
function Lobby {
    Wait-State 'four lobby slots, no avatars/score/feed, all Not Ready' {
        @((0..3 | ForEach-Object { Frame $_ }) | Where-Object { $null -ne $_ -and $_.scene -eq 'MainMenu' -and $_.room -eq 'Waiting' -and $_.members.Count -eq 4 -and
            @($_.members | Where-Object IsReady).Count -eq 0 -and $_.avatars -eq 0 -and $_.identities -eq 0 -and $_.scores.Count -eq 0 -and $_.feedCount -eq 0 }).Count -eq 4
    }
}
function Start-Round {
    foreach ($id in 0..3) { Send $id 'ready' }
    Wait-State 'all four players Ready' { @((Frame 0).members | Where-Object IsReady).Count -eq 4 }
    Send 0 'start'
    Wait-State 'four avatars entered arena' { @((0..3 | ForEach-Object { Frame $_ }) | Where-Object { $_.scene -eq 'Arena_MVP' -and $_.avatars -eq 4 }).Count -eq 4 }
    Converge 'Countdown'
    Converge 'InProgress'
    Wait-State 'all four participants Alive and can act' { @((Frame 0).actors | Where-Object { $_.canAct -and $_.hp -eq 10 }).Count -eq 4 }
    Start-Sleep -Milliseconds 1100 # Baseline spawn protection is 1 second; do not bypass it.
}
function Pickup([int]$VictimSlot) {
    Send 0 'loot' @{ slot = $VictimSlot }
    Wait-State 'pickup owner within replicated interaction radius' {
        $f = Frame 1; $a = Actor 1 $VictimSlot
        $null -ne $a -and $f.lootCount -eq 1 -and [Math]::Pow($a.x - $f.lootPosition.x,2) + [Math]::Pow($a.y - $f.lootPosition.y,2) + [Math]::Pow($a.z - $f.lootPosition.z,2) -le 2.25
    }
    Send 1 'pickup'
    Wait-State 'atomic pickup replicated to four observers: damage 2, item gone' {
        @((0..3 | ForEach-Object { Frame $_ }) | Where-Object { $_.lootCount -eq 0 -and @($_.actors | Where-Object { $_.slot -eq $VictimSlot -and $_.damage -eq 2 }).Count -eq 1 }).Count -eq 4
    }
}
try {
    foreach ($id in 0..3) {
        $arguments = '-batchmode -nographics -silent-crashes --four-player-harness ' + $id + ' --four-results "' + $directory + '" --four-run ' + $run +
            ' --four-port ' + $Port + ' --four-address 127.0.0.1 --four-max-runtime ' + $TimeoutSeconds + ' -logFile "' + (Join-Path $directory ("player-$id.log")) + '"'
        $workers[$id] = $job.Start($playerPath, $arguments, $buildDirectory, (Join-Path $directory ("player-$id.stdout.log")), (Join-Path $directory ("player-$id.stderr.log")))
        $summary.Workers += @{ Role = $id; PID = $workers[$id].Id; Executable = $playerPath; Arguments = $arguments }
        Wait-State "process $id joined through MainMenu" { $f = Frame $id; $null -ne $f -and $f.connected -and $f.scene -eq 'MainMenu' -and $f.members.Count -eq $id + 1 } 'Infrastructure'
    }
    Wait-State 'client ownership slot observed before fixture commands' { (Frame 1).localSlot -in @(1,2,3) }
    $victimSlot = (Frame 1).localSlot
    if ($victimSlot -eq 0) { throw 'Product: client owns host slot.' }
    for ($round = 1; $round -le $Rounds; $round++) {
        Lobby; Start-Round
        # Freeze ownership only after the complete roster and match baseline have converged.
        $victimSlot = (Frame 1).localSlot
        if ($victimSlot -notin @(1,2,3)) { throw 'Product: invalid client participant binding.' }
        if (@((Frame 0).scores | Where-Object { $_.kills -ne 0 -or $_.deaths -ne 0 -or $_.damage -ne 0 -or $_.life -ne 1 }).Count) { throw 'Product: new round retained old score/life.' }
        Send 0 'arrange' @{ slot = $victimSlot }
        Pickup $victimSlot
        for ($kill = 1; $kill -le 5; $kill++) {
            Send 0 'arrange' @{ slot = $victimSlot }
            $life = (Actor 0 $victimSlot).life
            for ($hit = 1; $hit -le 10; $hit++) {
                Send 0 'attack'
                Wait-State "round $round kill $kill hit ${hit}: authoritative HP/score replication" {
                    $a = Actor 1 $victimSlot
                    $null -ne $a -and $a.hp -eq 10 - $hit -and @((Frame 1).scores | Where-Object { $_.slot -eq 0 -and $_.damage -eq (($kill - 1) * 10 + $hit) }).Count -eq 1
                }
                if ($hit -lt 10) { Start-Sleep -Milliseconds 1100 }
            }
            if ($kill -lt 5) {
                Wait-State 'carrier death resets damage and starts respawn state' { $a = Actor 0 $victimSlot; $a.lifeState -eq 'Respawning' -and $a.damage -eq 1 }
                $deathAt = (Frame 0).lastDeathAt
                Wait-State 'new life restored to HP10/base1' { $a = Actor 1 $victimSlot; $null -ne $a -and $a.life -gt $life -and $a.lifeState -eq 'Alive' -and $a.hp -eq 10 -and $a.damage -eq 1 }
                Start-Sleep -Milliseconds 1100
                Converge 'InProgress'
                if ($kill -eq 1) {
                    Wait-State 'weapon reappears only after carrier-death 30-second delay' {
                        $f = Frame 0
                        if ($f.lootCount -gt 0 -and $f.now - $deathAt -lt 30) { throw 'Product: weapon respawned early.' }
                        $f.lootCount -eq 1 -and $f.now - $deathAt -ge 30
                    }
                    Pickup $victimSlot
                }
            }
        }
        Converge 'Ended'
        foreach ($id in 0..3) { Assert-FourPlayerFinalScores (Frame $id) $victimSlot }
        $idleSlot = (Frame 2).localSlot; $frozen = Actor 0 $idleSlot
        Send 2 'move' @{ moveX = 1; moveZ = 0; seconds = .3 }
        Send 0 'attack'; Start-Sleep -Milliseconds 400
        $afterFreeze = Actor 0 $idleSlot
        if ([Math]::Abs($afterFreeze.x - $frozen.x) -gt .05 -or [Math]::Abs($afterFreeze.z - $frozen.z) -gt .05) { throw 'Product: movement escaped Ended freeze.' }
        Assert-FourPlayerFinalScores (Frame 0) $victimSlot
        $summary.RoundsCompleted = $round
        Write-Host "PASS: complete round $round; host 5 kills/50 damage; victory/defeat and rankings agree."
        Lobby
    }
    # A fourth partial match isolates teardown from the three completed acceptance rounds.
    Start-Round
    $leavingSlot = (Frame 3).localSlot
    Send 3 'leave'
    Wait-State 'departing client back at MainMenu with no network identities' { $f = Frame 3; -not $f.connected -and $f.scene -eq 'MainMenu' -and $f.identities -eq 0 }
    Wait-State 'host/remaining clients removed departed avatar and retained live match' {
        @((0..2 | ForEach-Object { Frame $_ }) | Where-Object { $_.avatars -eq 3 -and $_.phase -eq 'InProgress' -and $_.members.Count -eq 3 -and
            @($_.actors | Where-Object slot -eq $leavingSlot).Count -eq 0 -and @($_.scores | Where-Object { $_.slot -eq $leavingSlot -and $_.lifeState -eq 'Disconnected' -and -not $_.connected }).Count -eq 1 }).Count -eq 3 -and (Frame 0).serverConnections -eq 3
    }
    $clockBeforeLeave = (Frame 0).clock
    Wait-State 'remaining host clock continues after client leave' { (Frame 0).clock -lt $clockBeforeLeave - .3 }
    Send 0 'leave'
    Wait-State 'host leave returns remaining clients to MainMenu; no singleton session/identity leak' {
        @((0..3 | ForEach-Object { Frame $_ }) | Where-Object { -not $_.connected -and -not $_.server -and $_.scene -eq 'MainMenu' -and $_.identities -eq 0 -and $_.members.Count -eq 0 }).Count -eq 4
    }
    foreach ($id in 0..3) { Send $id 'shutdown' @{} $true }
    foreach ($id in 0..3) {
        if (-not $workers[$id].WaitForExit(15000) -or $workers[$id].ExitCode -ne 0) { throw "Product: worker $id failed clean exit." }
        Assert-FourPlayerLog ([IO.File]::ReadAllText((Join-Path $directory ("player-$id.log"))))
        Assert-FourPlayerEvents ([IO.File]::ReadAllText((Join-Path $directory ("events-$id.jsonl")))) @('STARTED','MENU_ENTRY','CLEAN_SHUTDOWN')
    }
    $summary.Result = 'Passed'
} catch {
    $summary.Error = $_.Exception.Message
    $summary.FailureCategory = if ($summary.Error.StartsWith('Product:')) { 'Product' } else { 'Infrastructure' }
    Write-Host $summary.Error
}
finally {
    try {
        $job.Dispose()
        foreach ($worker in $workers.Values) { if (-not $worker.HasExited) { throw 'Infrastructure: owned worker survived Job Object disposal.' } }
        Assert-PortReleased; $summary.Cleanup = $true
    } catch { $summary.Result = 'Failed'; $summary.FailureCategory = 'Infrastructure'; $summary.Error += '; Cleanup: ' + $_.Exception.Message }
    foreach ($worker in $workers.Values) { $worker.Dispose() }
    $summary.DurationSeconds = $watch.Elapsed.TotalSeconds; $summary.Observations = $observations.ToArray()
    $summary.FreeDiskBytesAtEnd = [IO.DriveInfo]::new([IO.Path]::GetPathRoot($directory)).AvailableFreeSpace
    $summary | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $directory 'summary.json') -Encoding UTF8
    Write-Host ('Evidence: ' + $directory)
}
if ($summary.Result -ne 'Passed' -or -not $summary.Cleanup) { exit 1 }
Write-Host 'PASS: four real processes; all exited 0; zero owned survivors; UDP port reusable. PENDING_TV2_ORACLE (#24).'
exit 0
