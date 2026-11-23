param(
    [string]$Commit = '8fba1c127f650615faec4f14b34b16da7552c858',
    [string]$ValidationProject = 'TestResults/FourPlayerHarness/ValidationProject',
    [string]$EditorRoot = ''
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$resolvedCommit = (& git -C $root rev-parse ($Commit + '^{commit}')).Trim()
if ($LASTEXITCODE -ne 0 -or $resolvedCommit -notmatch '^[a-f0-9]{40}$') { throw 'Invalid baseline commit.' }
$project = [IO.Path]::GetFullPath((Join-Path $root $ValidationProject))
if (-not $project.StartsWith((Join-Path $root 'TestResults') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Validation project must remain inside workspace TestResults.' }
if (Test-Path -LiteralPath (Join-Path $project 'Temp/UnityLockfile')) { throw 'Validation Editor is open; close it first.' }
$EditorRoot = & (Join-Path $PSScriptRoot 'Resolve-UnityEditor.ps1') -EditorRoot $EditorRoot
$scratch = Join-Path $root ('TestResults/FourPlayerHarness/Source-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$zip = Join-Path $scratch 'baseline.zip'
& git -C $root archive --format=zip ('--output=' + $zip) $resolvedCommit Assets Packages ProjectSettings
if ($LASTEXITCODE -ne 0) { throw 'Cannot export baseline.' }
Expand-Archive -LiteralPath $zip -DestinationPath $scratch
New-Item -ItemType Directory -Path $project -Force | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    $destination = [IO.Path]::GetFullPath((Join-Path $project $folder))
    if (-not $destination.StartsWith($project + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe mirror destination.' }
    # Only the dedicated validation copy is mirrored; Library is retained, product assets untouched.
    & robocopy (Join-Path $scratch $folder) $destination /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -gt 7) { throw 'Cannot synchronize validation snapshot.' }
}
$toolFiles = @('FourPlayerAutomation.cs', 'FourPlayerHarnessBridge.cs', 'FourPlayerBuild.cs', 'Build-FourPlayerHarness.ps1')
$manifest = @($toolFiles | Sort-Object | ForEach-Object { @{ File = ('tools/' + $_); SHA256 = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $_) -Algorithm SHA256).Hash.ToLowerInvariant() } })
$canonical = ($manifest | ForEach-Object { $_.File + ':' + $_.SHA256 }) -join "`n"
$sha = [Security.Cryptography.SHA256]::Create()
try { $toolingHash = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($canonical)))).Replace('-', '').ToLowerInvariant() } finally { $sha.Dispose() }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'FourPlayerAutomation.cs') -Destination (Join-Path $project 'Assets/_Project/Scripts/Client/FourPlayerAutomation.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'FourPlayerHarnessBridge.cs') -Destination (Join-Path $project 'Assets/_Project/Scripts/Networking/FourPlayerHarnessBridge.cs')
$editorFolder = Join-Path $project 'Assets/_Project/Scripts/Editor/Build'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'FourPlayerBuild.cs') -Destination (Join-Path $editorFolder 'FourPlayerBuild.cs')
$identity = '#if SMALL_ARENA_FOUR_PLAYER_HARNESS' + "`n" + 'namespace SmallArena.Networking.Verification { public static class FourPlayerBuildIdentity { public const string Commit = "' + $resolvedCommit + '"; public const string Tooling = "' + $toolingHash + '"; } }' + "`n#endif`n"
[IO.File]::WriteAllText((Join-Path $project 'Assets/_Project/Scripts/Networking/FourPlayerBuildIdentity.cs'), $identity)
$identityReport = @{ Commit = $resolvedCommit; ToolingSHA256 = $toolingHash; ToolingFiles = $manifest; Instrumented = $true; Oracle = 'PENDING_TV2_ORACLE (#24)' }
$env:SMALL_ARENA_FOUR_PLAYER_BUILD = '1'
$log = Join-Path $project 'Logs/FourPlayerBuild.log'
New-Item -ItemType Directory -Path (Split-Path -Parent $log) -Force | Out-Null
try {
    $unityArguments = '-quit -batchmode -nographics -silent-crashes -buildTarget StandaloneWindows64 -projectPath "' + $project + '" -executeMethod SmallArena.Tools.FourPlayerBuild.Build -logFile "' + $log + '"'
    $worker = Start-Process -FilePath (Join-Path $EditorRoot 'Unity.exe') -ArgumentList $unityArguments -WindowStyle Hidden -PassThru
    $null = $worker.Handle
    if (-not $worker.WaitForExit(1200000)) { Stop-Process -Id $worker.Id -Force; throw 'Infrastructure: instrumented build exceeded 20 minutes.' }
    if ($worker.ExitCode -ne 0) { throw "Instrumented Unity build exit=$($worker.ExitCode); see $log" }
    & (Join-Path $PSScriptRoot 'Assert-WindowsStandaloneBuild.ps1') -ProjectPath $project
    $exe = Join-Path $project 'Builds/WindowsStandalone/SmallArena.exe'
    $identityReport.BinarySHA256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    . (Join-Path $PSScriptRoot 'FourPlayerHarnessChecks.ps1')
    $identityReport.ContentSHA256 = Get-FourPlayerContentHash (Split-Path -Parent $exe)
    $identityReport.Player = $exe
    $identityReport | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path (Split-Path -Parent $exe) 'four-player-build.json') -Encoding UTF8
    Write-Output ('PASS: instrumented baseline=' + $resolvedCommit + '; tooling=' + $toolingHash + '; binary=' + $identityReport.BinarySHA256)
} finally { Remove-Item Env:SMALL_ARENA_FOUR_PLAYER_BUILD -ErrorAction SilentlyContinue }
