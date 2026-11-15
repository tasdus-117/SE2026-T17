param(
    [ValidateSet('EditMode', 'PlayMode', 'Build')][string]$Phase = 'PlayMode',
    [string]$Editor = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe',
    [string]$TestFilter = ''
)
$ErrorActionPreference = 'Stop'
$validationRoot = Split-Path -Parent $PSScriptRoot
$results = Join-Path $validationRoot 'TestResults/LobbyArenaLoading'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$label = $Phase.ToLowerInvariant()
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $validationRoot + '"'),
    '-logFile', ('"' + (Join-Path $results ($label + '.log')) + '"'))
$temporarySources = @()
try {
    if ($Phase -eq 'Build') {
        $buildSources = @(
            (Join-Path $validationRoot 'Assets/_Project/Scripts/Editor/Build/Build-LobbyStandalone.cs'),
            (Join-Path $validationRoot 'Assets/_Project/Scripts/Client/LobbyLoadingStandaloneSmoke.cs'))
        foreach ($path in $buildSources) {
            if ((Test-Path -LiteralPath $path) -or (Test-Path -LiteralPath ($path + '.meta'))) { throw ('Temporary source already exists: ' + $path) }
        }
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Build-LobbyStandalone.cs') -Destination $buildSources[0]
        $temporarySources += $buildSources[0]
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LobbyLoadingStandaloneSmoke.cs') -Destination $buildSources[1]
        $temporarySources += $buildSources[1]
        $env:SMALL_ARENA_M2_BUILD = Join-Path $results 'Standalone/SmallArenaTV31.exe'
        $arguments += @('-executeMethod', 'SmallArena.Tools.LobbyStandaloneBuilder.Build', '-quit')
    } else {
        $arguments += @('-runTests', '-testPlatform', $Phase, '-testResults', ('"' + (Join-Path $results ($label + '.xml')) + '"'))
        if ($TestFilter) { $arguments += @('-testFilter', $TestFilter) }
    }
    $worker = Start-Process -FilePath $Editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $worker.Id | Set-Content -LiteralPath (Join-Path $results ($label + '.pid'))
    $worker.WaitForExit()
    if ($worker.ExitCode -ne 0) { throw ('Unity ' + $Phase + ' failed: exit ' + $worker.ExitCode + '; see ' + $results) }
    if ($Phase -ne 'Build') {
        [xml]$report = Get-Content -LiteralPath (Join-Path $results ($label + '.xml')) -Raw
        $report.'test-run' | Select-Object result, total, passed, failed, skipped | Format-List
        if ([int]$report.'test-run'.total -eq 0 -or [int]$report.'test-run'.failed -ne 0) { throw 'Invalid or failing Unity test report.' }
    }
} finally {
    foreach ($path in $temporarySources) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
        if (Test-Path -LiteralPath ($path + '.meta')) { Remove-Item -LiteralPath ($path + '.meta') }
    }
}
