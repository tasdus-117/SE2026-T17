param(
    [string]$Player = 'TestResults/M2/Standalone/SmallArenaM2.exe',
    [string]$Results = 'TestResults/M2/StandaloneSmoke',
    [ValidateSet('', 'host', 'client')][string]$Teardown = ''
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$playerPath = [IO.Path]::GetFullPath((Join-Path $projectRoot $Player))
$resultsRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot $Results))
if (-not $resultsRoot.StartsWith((Join-Path $projectRoot 'TestResults') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Smoke results must remain inside this workspace TestResults directory.'
}
if (-not (Test-Path -LiteralPath $playerPath)) { throw 'Build the instrumented standalone player first.' }
$caseDirectory = Join-Path $resultsRoot ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $caseDirectory -Force | Out-Null
$socket = New-Object System.Net.Sockets.UdpClient(0)
$selectedPort = $socket.Client.LocalEndPoint.Port
$socket.Dispose()
$workers = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'

function Start-Worker([string]$Role) {
    $arguments = '-batchmode -nographics --lobby-smoke ' + $Role + ' --lobby-port ' + $selectedPort +
        ' --lobby-results "' + $caseDirectory + '" -logFile "' + (Join-Path $caseDirectory ($Role + '.log')) + '"'
    if ($Teardown) { $arguments += ' --menu-teardown ' + $Teardown }
    $worker = Start-Process -FilePath $playerPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $workers.Add($worker)
    Write-Output $worker
}
function Wait-Evidence([string]$Name, [System.Diagnostics.Process]$Worker, [bool]$Json = $false) {
    $path = Join-Path $caseDirectory $Name
    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-Path -LiteralPath $path) {
            if (-not $Json) { return }
            try { return (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json) }
            catch [System.ArgumentException] { }
        }
        if ($Worker.HasExited) { throw ($Name + ': worker exited early; inspect ' + $caseDirectory) }
        Start-Sleep -Milliseconds 200
    }
    throw ('Timed out waiting for ' + $Name + '; inspect ' + $caseDirectory)
}
try {
    $smokeHostProcess = Start-Worker 'host'
    Wait-Evidence 'host.started' $smokeHostProcess
    $smokeClientProcess = Start-Worker 'client'
    $hostReport = Wait-Evidence 'host.json' $smokeHostProcess $true
    $clientReport = Wait-Evidence 'client.json' $smokeClientProcess $true
    foreach ($report in @($hostReport, $clientReport)) {
        if (-not $report.success -or -not $report.openedFromMenu -or -not $report.noDebugHud -or
            $report.scene -ne 'Arena_MVP' -or $report.roomMembers -ne 2 -or $report.visiblePlayers -ne 2 -or
            $report.localPlayer -eq 0 -or @($report.errors).Count -ne 0) {
            throw ('Standalone assertion failed for ' + $report.role + '; inspect ' + $caseDirectory)
        }
    }
    if ($Teardown) {
        $hostMenu = Wait-Evidence 'host.menu.json' $smokeHostProcess $true
        $clientMenu = Wait-Evidence 'client.menu.json' $smokeClientProcess $true
        foreach ($report in @($hostMenu, $clientMenu)) {
            if (-not $report.success -or -not $report.networkStopped -or -not $report.freshManager -or
                -not $report.noIdentities -or -not $report.menuCursor -or $report.scene -ne 'MainMenu' -or @($report.errors).Count -ne 0) {
                throw ('Standalone menu teardown failed for ' + $report.role + '; inspect ' + $caseDirectory)
            }
        }
        if ($Teardown -eq 'client' -and -not $hostMenu.hostSurvivedClientLeave) { throw 'Host did not survive client leave.' }
        @($hostMenu, $clientMenu) | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $caseDirectory 'menu-summary.json') -Encoding utf8
        if ($Teardown -eq 'host') { Write-Output 'PASS: host leave automatically returned the guest to fresh MainMenu; no identities or runtime errors remained.' }
        else { Write-Output 'PASS: client leave preserved the host until its own return; both reached fresh MainMenu without identities or runtime errors.' }
    }
    foreach ($worker in $workers) {
        if (-not $worker.WaitForExit(15000)) { throw 'Smoke worker did not exit gracefully.' }
        if ($worker.ExitCode -ne 0) { throw ('Smoke worker exit code: ' + $worker.ExitCode) }
    }
    @($hostReport, $clientReport) | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $caseDirectory 'summary.json') -Encoding utf8
    Write-Output ('PASS: two standalone players used MainMenu Host/Join, Ready and Start over KCP port ' + $selectedPort + '.')
    Write-Output 'PASS: both entered Arena_MVP with two avatars, local ownership, no debug HUD and zero captured errors.'
    Write-Output ('Evidence: ' + $caseDirectory)
}
finally {
    # Only process objects created above are eligible for cleanup; never enumerate or stop user Editors/players.
    foreach ($worker in $workers) {
        try { if (-not $worker.HasExited) { $worker.Kill(); $worker.WaitForExit(5000) | Out-Null } }
        finally { $worker.Dispose() }
    }
}
