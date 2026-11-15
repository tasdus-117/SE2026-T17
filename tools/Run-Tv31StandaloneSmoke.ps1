param(
    [string]$Player = 'TestResults/TV31/Standalone/SmallArenaTV31.exe',
    [string]$Results = 'TestResults/TV31/StandaloneSmoke',
    [ValidateSet('host', 'client')][string]$Faller = 'client'
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
    $arguments += ' --arena-faller ' + $Faller
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
            $report.scene -ne 'Arena_MVP' -or $report.roomMembers -ne 2 -or $report.visiblePlayers -ne 2 -or $report.visibleLoot -ne 1 -or
            $report.localPlayer -eq 0 -or @($report.errors).Count -ne 0) {
            throw ('Standalone assertion failed for ' + $report.role + '; inspect ' + $caseDirectory)
        }
    }
    if ($hostReport.layout -ne $clientReport.layout -or -not $hostReport.collidersMatch -or -not $clientReport.collidersMatch) {
        throw ('Server/client obstacle descriptions or actual colliders differ: ' + $caseDirectory)
    }
    $hostFall = Wait-Evidence 'host.fall.json' $smokeHostProcess $true
    $clientFall = Wait-Evidence 'client.fall.json' $smokeClientProcess $true
    foreach ($report in @($hostFall, $clientFall)) {
        if (-not $report.success -or $report.victimHealth -ne 0 -or $report.victimResult -ne 'Defeat' -or @($report.errors).Count -ne 0) {
            throw ('Fall replication/result HUD failed: ' + $caseDirectory)
        }
    }
    @($hostFall, $clientFall) | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $caseDirectory 'fall-summary.json') -Encoding utf8
    foreach ($worker in $workers) {
        if (-not $worker.WaitForExit(15000)) { throw 'Smoke worker did not exit gracefully.' }
        if ($worker.ExitCode -ne 0) { throw ('Smoke worker exit code: ' + $worker.ExitCode) }
    }
    @($hostReport, $clientReport) | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $caseDirectory 'summary.json') -Encoding utf8
    Write-Output ('PASS: two standalone players used MainMenu Host/Join, Ready and Start over KCP port ' + $selectedPort + '.')
    Write-Output ('PASS: identical obstacle descriptors and actual colliders; ' + $Faller + ' fell under server gravity; server HP=0; replicas show Defeat/Victory HUD; zero runtime errors.')
    Write-Output ('Evidence: ' + $caseDirectory)
}
finally {
    # Only process objects created above are eligible for cleanup; never enumerate or stop user Editors/players.
    foreach ($worker in $workers) {
        try { if (-not $worker.HasExited) { $worker.Kill(); $worker.WaitForExit(5000) | Out-Null } }
        finally { $worker.Dispose() }
    }
}
