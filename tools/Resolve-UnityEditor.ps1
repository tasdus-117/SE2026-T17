param([string]$EditorRoot = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$version = [regex]::Match([IO.File]::ReadAllText((Join-Path $root 'ProjectSettings/ProjectVersion.txt')), '(?m)^m_EditorVersion:\s*(\S+)').Groups[1].Value
if (-not $version) { throw 'Missing m_EditorVersion in ProjectVersion.txt.' }
if (-not $EditorRoot) {
    # GameCI Windows images expose UNITY_PATH; local Hub installations do not.
    $EditorRoot = if ($env:UNITY_PATH) { Join-Path $env:UNITY_PATH 'Editor' }
        else { Join-Path "C:\Program Files\Unity\Hub\Editor\$version" 'Editor' }
}
$binary = Join-Path $EditorRoot 'Unity.exe'
if (-not (Test-Path -LiteralPath $binary -PathType Leaf)) { throw "Missing Unity Editor binary: $binary" }
$installed = (Get-Item -LiteralPath $binary).VersionInfo.ProductVersion.Split('_')[0]
if ($installed -ne $version) { throw "Project requires Unity $version; found $installed in $EditorRoot" }
return (Get-Item -LiteralPath $EditorRoot).FullName
