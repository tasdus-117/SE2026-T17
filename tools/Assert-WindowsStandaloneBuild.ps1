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
$reader = New-Object IO.BinaryReader($exe)
try {
    if ($exe.Length -lt 64 -or $reader.ReadUInt16() -ne 0x5A4D) { throw 'SmallArena.exe has an invalid MZ signature or truncated header.' }
    $exe.Position = 0x3C
    $offset = [long]$reader.ReadUInt32()
    if ($offset -lt 64 -or $offset -gt $exe.Length - 26) { throw 'SmallArena.exe has an invalid PE header offset.' }
    $exe.Position = $offset
    if ($reader.ReadUInt32() -ne 0x00004550 -or $reader.ReadUInt16() -ne 0x8664) { throw 'SmallArena.exe requires a valid PE signature and AMD64 machine type.' }
    $exe.Position = $offset + 20
    $optionalSize = $reader.ReadUInt16()
    $exe.Position = $offset + 24
    if ($optionalSize -lt 112 -or $offset + 24 + $optionalSize -gt $exe.Length -or $reader.ReadUInt16() -ne 0x020B) { throw 'SmallArena.exe requires a complete PE32+ optional header.' }
} finally { $reader.Dispose() }
$data = Join-Path $output 'SmallArena_Data'
if (-not (Test-Path -LiteralPath $data -PathType Container) -or
    @(Get-ChildItem -LiteralPath $data -Recurse -File | Where-Object { $_.Length -gt 0 }).Count -eq 0) { throw 'Missing/empty SmallArena_Data directory.' }
$globals = Join-Path $data 'globalgamemanagers'
if (-not (Test-Path -LiteralPath $globals -PathType Leaf) -or (Get-Item -LiteralPath $globals).Length -eq 0) { throw 'Missing/empty globalgamemanagers build asset.' }
$sceneData = @('level0', 'data.unity3d') | ForEach-Object { Join-Path $data $_ } | Where-Object { (Test-Path -LiteralPath $_ -PathType Leaf) -and (Get-Item -LiteralPath $_).Length -gt 0 }
if (@($sceneData).Count -eq 0) { throw 'Missing built scene data (level0 or data.unity3d).' }
try { $manifest = Get-Content -LiteralPath (Join-Path $output 'build-report.json') -Raw | ConvertFrom-Json }
catch { throw "Malformed build-report JSON: $($_.Exception.Message)" }
$version = [regex]::Match([IO.File]::ReadAllText((Join-Path $root 'ProjectSettings/ProjectVersion.txt')), '(?m)^m_EditorVersion:\s*(\S+)').Groups[1].Value
if (-not $version -or $manifest.UnityVersion -ne $version -or $manifest.Target -ne 'StandaloneWindows64' -or $manifest.Backend -ne 'Mono2x' -or
    $manifest.Result -ne 'Succeeded' -or $null -eq $manifest.Errors -or $manifest.Errors -ne 0 -or
    $null -eq $manifest.Bytes -or $manifest.Bytes -le 0 -or ($manifest.Scenes -join '|') -cne ($expected -join '|')) {
    throw 'BuildReport is invalid: require matching Unity version/scenes, StandaloneWindows64/Mono2x, Succeeded, zero errors and positive size.'
}
$log = Join-Path $root 'Logs/WindowsStandalone/build.log'
if (-not (Test-Path -LiteralPath $log -PathType Leaf) -or (Get-Item -LiteralPath $log).Length -eq 0) { throw 'Missing/empty build log.' }
if ([IO.File]::ReadAllText($log) -match '(?im)Assets[\\/]_Project[^\r\n]*\b(?:warning|error) [A-Z]+\d+') { throw 'Project compiler warning/error in standalone build log.' }
Write-Output "PASS: Windows x64 PE32+/Mono2x player, runtime DLL, built scene data, BuildReport and log; errors=0, warnings=$($manifest.Warnings), bytes=$($manifest.Bytes)."
