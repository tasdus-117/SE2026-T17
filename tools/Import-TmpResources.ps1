param(
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$destinationRoot = [IO.Path]::GetFullPath($ProjectPath)
if (-not ($destinationRoot -eq $taskRoot -or $destinationRoot.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase))) {
    throw 'Resource destination must remain in the workspace.'
}
$package = Join-Path $EditorRoot 'Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage'
$extractRoot = Join-Path $taskRoot 'TestResults/TmpEssentialResources'
New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
$entries = @(& tar -tf $package)
if ($LASTEXITCODE -ne 0 -or @($entries | Where-Object { $_ -match '(^/|^[A-Za-z]:|(^|/)\.\.(/|$))' }).Count) { throw 'Invalid resource archive paths.' }
& tar -xf $package -C $extractRoot
if ($LASTEXITCODE -ne 0) { throw 'TMP resource extraction failed.' }
$count = 0
foreach ($folder in Get-ChildItem -LiteralPath $extractRoot -Directory) {
    $pathname = Join-Path $folder.FullName 'pathname'
    if (-not (Test-Path -LiteralPath $pathname)) { continue }
    $relative = [IO.File]::ReadAllText($pathname).Trim()
    $destination = [IO.Path]::GetFullPath((Join-Path $destinationRoot $relative))
    if (-not $destination.StartsWith((Join-Path $destinationRoot 'Assets') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Invalid asset path: $relative" }
    $metadata = Join-Path $folder.FullName 'asset.meta'
    if ((Get-Content -LiteralPath $metadata -Raw) -match 'folderAsset: yes') {
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
    } else {
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $folder.FullName 'asset') -Destination $destination -Force
    }
    Copy-Item -LiteralPath $metadata -Destination ($destination + '.meta') -Force
    $count++
}
Write-Output "Imported $count bundled TMP assets with their original metadata and licenses."
