param(
    [string]$Player = 'Builds/WindowsStandalone/SmallArena.exe',
    [ValidateRange(20, 180)][int]$TimeoutSeconds = 90,
    [string]$Results = 'TestResults/MultiplayerSmoke'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'MultiplayerSmokeChecks.ps1')
if (-not ('SmallArena.Tools.SmokeProcessJob' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'SmokeProcessJob.cs') }
$root = Split-Path -Parent $PSScriptRoot
$playerPath = (Get-Item -LiteralPath (Join-Path $root $Player)).FullName
$resultsRoot = [IO.Path]::GetFullPath((Join-Path $root $Results))
if (-not $resultsRoot.StartsWith((Join-Path $root 'TestResults') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Smoke evidence must stay under workspace TestResults.' }
function Free-Port {
    $socket = New-Object Net.Sockets.UdpClient
    $socket.Client.ExclusiveAddressUse = $true
    $socket.Client.Bind((New-Object Net.IPEndPoint([Net.IPAddress]::Loopback, 0)))
    try { return $socket.Client.LocalEndPoint.Port } finally { $socket.Dispose() }
}
foreach ($scenario in @('Combat', 'Disconnect')) {
    $directory = Join-Path (Join-Path $resultsRoot $scenario) ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $port = Free-Port
    $job = New-Object SmallArena.Tools.SmokeProcessJob($TimeoutSeconds)
    $workers = @{}
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $forced = $false
    function Start-Worker([string]$Role) {
        $mode = if ($scenario -eq 'Combat') { 'combat-duel' } else { 'hold' }
        $auto = if ($Role -eq 'host') { '--auto-host --auto-start' } else { '--auto-join 127.0.0.1' }
        $arguments = '-batchmode -nographics -silent-crashes ' + $auto + ' --auto-ready --expected-players 2 --port ' + $port +
            ' --smoke-action ' + $mode + ' --max-runtime-seconds ' + ($TimeoutSeconds - 5) +
            ' -logFile "' + (Join-Path $directory ($Role + '.log')) + '"'
        $process = $job.Start($playerPath, $arguments, (Split-Path -Parent $playerPath),
            (Join-Path $directory ($Role + '.stdout.log')), (Join-Path $directory ($Role + '.stderr.log')))
        $workers[$Role] = $process
        $process.Id | Set-Content -LiteralPath (Join-Path $directory ($Role + '.pid'))
    }
    function Read-Worker([string]$Role) {
        $text = Read-SmokeLog (Join-Path $directory ($Role + '.log'))
        $text += "`n" + (Read-SmokeLog (Join-Path $directory ($Role + '.stdout.log')))
        $text += "`n" + (Read-SmokeLog (Join-Path $directory ($Role + '.stderr.log')))
        Assert-SmokeNoErrors $text
        return $text
    }
    function Wait-Marker([string]$Role, [string]$Marker) {
        while ($true) {
            Assert-SmokeDeadline $clock.Elapsed.TotalSeconds $TimeoutSeconds $job.TimedOut
            $text = Read-Worker $Role
            if (Test-SmokeMarker $text $Marker) { return }
            if ($workers[$Role].HasExited) { throw "$Role exited before $Marker; see $directory" }
            Start-Sleep -Milliseconds 100
        }
    }
    try {
        Start-Worker host
        Wait-Marker host HOST_STARTED
        Start-Worker client
        foreach ($role in @('host', 'client')) { Wait-Marker $role MATCH_IN_PROGRESS }
        if ($scenario -eq 'Disconnect') {
            if ($workers.client.HasExited) { throw 'Client exited before crash simulation.' }
            $workers.client.Kill(); $forced = $true
            if (-not $workers.client.WaitForExit(5000)) { throw 'Client crash simulation failed.' }
            Wait-Marker host PEER_DISCONNECTED
        }
        foreach ($role in @('host', 'client')) {
            if ($forced -and $role -eq 'client') { continue }
            Wait-Marker $role CLEAN_SHUTDOWN
            while (-not $workers[$role].HasExited) {
                Assert-SmokeDeadline $clock.Elapsed.TotalSeconds $TimeoutSeconds $job.TimedOut
                Start-Sleep -Milliseconds 100
            }
        }
        foreach ($role in @('host', 'client')) {
            $text = Read-Worker $role
            $required = @('GAME_ENTRY', 'CLIENT_CONNECTED', 'LOBBY_READY', 'AVATARS_SPAWNED', 'MATCH_COUNTDOWN', 'MATCH_IN_PROGRESS')
            if ($scenario -eq 'Combat') { $required += @('MOVEMENT_VERIFIED', 'COMBAT_VERIFIED', 'CLEAN_SHUTDOWN') }
            elseif ($role -eq 'host') { $required += @('PEER_DISCONNECTED', 'SERVER_RESILIENT', 'CLEAN_SHUTDOWN') }
            Assert-SmokeMarkers $text $required
            $order = @('GAME_ENTRY', 'CLIENT_CONNECTED', 'LOBBY_READY', 'MATCH_COUNTDOWN', 'MATCH_IN_PROGRESS')
            if ($scenario -eq 'Combat') { $order += @('COMBAT_VERIFIED', 'CLEAN_SHUTDOWN') }
            elseif ($role -eq 'host') { $order += @('PEER_DISCONNECTED', 'CLEAN_SHUTDOWN') }
            Assert-SmokeOrder $text $order
            Assert-SmokeExit $workers[$role].ExitCode ($forced -and $role -eq 'client')
        }
        Assert-SmokeDeadline $clock.Elapsed.TotalSeconds $TimeoutSeconds $job.TimedOut
        $summary = [pscustomobject]@{ Scenario = $scenario; Port = $port; HostPID = $workers.host.Id; ClientPID = $workers.client.Id;
            HostExit = $workers.host.ExitCode; ClientExit = $workers.client.ExitCode; ForcedClient = $forced; Seconds = $clock.Elapsed.TotalSeconds;
            Result = 'Passed'; LeftoverWorkers = 0; PortReleased = $true }
    }
    finally {
        $job.Dispose()
        foreach ($worker in $workers.Values) {
            try {
                if (-not $worker.WaitForExit(5000)) { throw "Owned process $($worker.Id) survived Job Object teardown." }
            } finally { $worker.Dispose() }
        }
        $probe = New-Object Net.Sockets.UdpClient
        try {
            $probe.Client.ExclusiveAddressUse = $true
            $probe.Client.Bind((New-Object Net.IPEndPoint([Net.IPAddress]::Any, $port)))
            Write-Output "CLEANUP: all owned workers exited; UDP $port released."
        } finally { $probe.Dispose() }
    }
    $summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $directory 'result.json')
    Write-Output "PASS: $scenario over real KCP UDP $port; evidence=$directory"
}
