param(
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [switch]$ScenesOnly
)
$ErrorActionPreference = 'Stop'
$root = (Get-Item -LiteralPath $ProjectPath).FullName
$expected = @('Assets/_Project/Scenes/MainMenu.unity', 'Assets/_Project/Scenes/Bootstrap.unity', 'Assets/_Project/Scenes/Arena_MVP.unity')
$settings = [IO.File]::ReadAllText((Join-Path $root 'ProjectSettings/EditorBuildSettings.asset'))
$entries = [regex]::Matches($settings, '(?m)^  - enabled: ([01])\r?\n    path: ([^\r\n]+)\r?\n    guid: ([0-9a-fA-F]{32})\s*$')
$enabled = @($entries | Where-Object { $_.Groups[1].Value -eq '1' })
$paths = @($enabled | ForEach-Object { $_.Groups[2].Value.Trim() })
if (($paths -join '|') -cne ($expected -join '|')) { throw 'Enabled scenes must be MainMenu (0), Bootstrap (1), Arena_MVP (2), in that order.' }
foreach ($scene in $enabled) {
    $path = Join-Path $root $scene.Groups[2].Value.Trim()
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) { throw "Missing/empty build scene: $path" }
    if (-not (Test-Path -LiteralPath ($path + '.meta') -PathType Leaf) -or
        [IO.File]::ReadAllText($path + '.meta') -notmatch ('(?mi)^guid: ' + $scene.Groups[3].Value + '\s*$')) { throw "Build scene GUID mismatch: $path" }
}
Write-Output 'PASS: enabled scene order, scene files and GUIDs: MainMenu (0), Bootstrap (1), Arena_MVP (2).'
if ($ScenesOnly) { return }
$output = Join-Path $root 'Builds/WindowsStandalone'
foreach ($name in @('SmallArena.exe', 'UnityPlayer.dll', 'build-report.json')) {
    $path = Join-Path $output $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) { throw "Missing/empty build output: $path" }
}
$exe = [IO.File]::OpenRead((Join-Path $output 'SmallArena.exe'))
try { if ($exe.ReadByte() -ne 0x4D -or $exe.ReadByte() -ne 0x5A) { throw 'SmallArena.exe is not a Windows executable (missing MZ signature).' } }
finally { $exe.Dispose() }
$data = Join-Path $output 'SmallArena_Data'
if (-not (Test-Path -LiteralPath $data -PathType Container) -or
    @(Get-ChildItem -LiteralPath $data -Recurse -File | Where-Object { $_.Length -gt 0 }).Count -eq 0) { throw 'Missing/empty SmallArena_Data directory.' }
try { $manifest = Get-Content -LiteralPath (Join-Path $output 'build-report.json') -Raw | ConvertFrom-Json }
catch { throw "Malformed build-report JSON: $($_.Exception.Message)" }
$version = [regex]::Match([IO.File]::ReadAllText((Join-Path $root 'ProjectSettings/ProjectVersion.txt')), '(?m)^m_EditorVersion:\s*(\S+)').Groups[1].Value
if (-not $version -or $manifest.UnityVersion -ne $version -or $manifest.Target -ne 'StandaloneWindows64' -or
    $manifest.Result -ne 'Succeeded' -or $null -eq $manifest.Errors -or $manifest.Errors -ne 0 -or
    $null -eq $manifest.Bytes -or $manifest.Bytes -le 0 -or ($manifest.Scenes -join '|') -cne ($expected -join '|')) {
    throw 'BuildReport is invalid: require matching Unity version/scenes, StandaloneWindows64, Succeeded, zero errors and positive size.'
}
$log = Join-Path $root 'Logs/WindowsStandalone/build.log'
if (-not (Test-Path -LiteralPath $log -PathType Leaf) -or (Get-Item -LiteralPath $log).Length -eq 0) { throw 'Missing/empty build log.' }
if ([IO.File]::ReadAllText($log) -match '(?im)Assets[\\/]_Project[^\r\n]*\b(?:warning|error) [A-Z]+\d+') { throw 'Project compiler warning/error in standalone build log.' }
Write-Output "PASS: Windows player, runtime DLL, non-empty data, BuildReport and log; errors=0, warnings=$($manifest.Warnings), bytes=$($manifest.Bytes)."
