param(
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$EditorRoot = '',
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$root = (Get-Item -LiteralPath $ProjectPath).FullName
& (Join-Path $PSScriptRoot 'Assert-WindowsStandaloneBuild.ps1') -ProjectPath $root -ScenesOnly
$EditorRoot = & (Join-Path $PSScriptRoot 'Resolve-UnityEditor.ps1') -EditorRoot $EditorRoot
$version = [regex]::Match([IO.File]::ReadAllText((Join-Path $root 'ProjectSettings/ProjectVersion.txt')), '(?m)^m_EditorVersion:\s*(\S+)').Groups[1].Value
$installed = (Get-Item -LiteralPath (Join-Path $EditorRoot 'Unity.exe')).VersionInfo.ProductVersion.Split('_')[0]
if ($version -ne $installed) { throw "Project requires $version; Editor is $installed." }
if (-not (Test-Path -LiteralPath (Join-Path $EditorRoot 'Data/PlaybackEngines/windowsstandalonesupport') -PathType Container)) { throw 'Install Windows Build Support for this Editor.' }
$entry = Join-Path $root 'Assets/_Project/Scripts/Editor/Build/SmallArenaBuildPipeline.cs'
if (-not (Test-Path -LiteralPath $entry -PathType Leaf)) { throw 'Missing SmallArenaBuildPipeline Editor entry point.' }
if ($ValidateOnly) { Write-Output "PASS: build preflight for Unity $version; no Editor execution or player build was performed."; return }
if (Test-Path -LiteralPath (Join-Path $root 'Temp/UnityLockfile')) { throw 'Close the Editor for this project or use a separate validation copy.' }
$logs = Join-Path $root 'Logs/WindowsStandalone'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
# A failed launch must never validate a previous successful report/log.
foreach ($relative in @('Builds/WindowsStandalone/build-report.json', 'Logs/WindowsStandalone/build.log', 'Logs/WindowsStandalone/editor.log')) {
    $path = Join-Path $root $relative
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
}
$arguments = @('-quit', '-batchmode', '-nographics', '-silent-crashes', '-buildTarget', 'StandaloneWindows64',
    '-projectPath', ('"' + $root + '"'), '-executeMethod', 'SmallArena.Tools.SmallArenaBuildPipeline.BuildWindowsStandalone',
    '-logFile', ('"' + (Join-Path $logs 'editor.log') + '"'))
$process = Start-Process -FilePath (Join-Path $EditorRoot 'Unity.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
$null = $process.Handle
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity Windows build failed: exit $($process.ExitCode). See $logs/editor.log" }
& (Join-Path $PSScriptRoot 'Assert-WindowsStandaloneBuild.ps1') -ProjectPath $root
