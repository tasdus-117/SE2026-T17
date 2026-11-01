param(
    [string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor',
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $taskRoot 'TestResults/Compilation' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $output.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Compiler output must remain in the workspace.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null
$resultPath = Join-Path $output 'results.json'
if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath -Force }
$utf8 = New-Object Text.UTF8Encoding($false)
$dotnet = Join-Path $EditorRoot 'Data/NetCoreRuntime/dotnet.exe'
$csc = Join-Path $EditorRoot 'Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$framework = @(Get-ChildItem -LiteralPath (Join-Path $EditorRoot 'Data/DotNetSdk/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0') -Filter '*.dll' | ForEach-Object { $_.FullName })
$engine = @(Get-ChildItem -LiteralPath (Join-Path $EditorRoot 'Data/Managed/UnityEngine') -Filter '*.dll' | ForEach-Object { $_.FullName })
$editorModules = Join-Path $EditorRoot 'Data/Managed/UnityEditor'
if (Test-Path -LiteralPath $editorModules) { $engine += @(Get-ChildItem -LiteralPath $editorModules -Filter '*.dll' | ForEach-Object { $_.FullName }) }
$editorFacade = Join-Path $EditorRoot 'Data/Managed/UnityEditor.dll'
if (Test-Path -LiteralPath $editorFacade) { $engine += $editorFacade }
$nunit = Join-Path $EditorRoot 'Data/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll'
$templateCache = Join-Path $EditorRoot 'Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.urp-blank-17.2.6/ScriptAssemblies'
$definitions = @{}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Assets') -Filter '*.asmdef' -Recurse) {
    $definition = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    $definitions[$definition.name] = @{ File = $file; Definition = $definition }
}
$order = @('SmallArena.Domain', 'SmallArena.Runtime', 'SmallArena.EditMode.Tests', 'SmallArena.PlayMode.Tests')
if ($definitions.Count -ne $order.Count) { throw 'Review compilation order after changing the four-assembly boundary.' }
$compiled = @{}
$report = @()
foreach ($name in $order) {
    $entry = $definitions[$name]
    if (-not $entry) { throw "Missing assembly: $name" }
    $definition = $entry.Definition
    $sources = @()
    foreach ($source in Get-ChildItem -LiteralPath $entry.File.DirectoryName -Filter '*.cs' -Recurse) {
        # Unity gives each source to its nearest parent asmdef, not every ancestor.
        $folder = $source.DirectoryName
        while ($folder) {
            $owners = @(Get-ChildItem -LiteralPath $folder -Filter '*.asmdef')
            if ($owners.Count -gt 0) {
                if ($owners.Count -ne 1) { throw "Ambiguous assembly folder: $folder" }
                if ($owners[0].FullName -eq $entry.File.FullName) { $sources += $source.FullName }
                break
            }
            $folder = Split-Path -Parent $folder
        }
    }
    if ($sources.Count -eq 0) { throw "No source files found for $name" }
    $references = @($framework)
    if (-not $definition.noEngineReferences) { $references += $engine }
    foreach ($reference in $definition.references) {
        if ($compiled.ContainsKey($reference)) { $references += $compiled[$reference]; continue }
        if ($definitions.ContainsKey($reference)) { throw "Dependency order violation: $name -> $reference" }
        $candidates = @((Join-Path $taskRoot "Library/ScriptAssemblies/$reference.dll"), (Join-Path $templateCache "$reference.dll"))
        $resolved = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if (-not $resolved) { throw "Missing installed package assembly: $reference" }
        $references += $resolved
    }
    foreach ($reference in $definition.precompiledReferences) {
        if ($reference -ne 'nunit.framework.dll') { throw "Review explicit plugin reference: $reference" }
        $references += $nunit
    }
    $references = @($references | Select-Object -Unique)
    $binary = Join-Path $output ($name + '.dll')
    $response = Join-Path $output ($name + '.rsp')
    $arguments = @('/nologo', '/nostdlib+', '/langversion:9.0', '/warnaserror+', '/target:library', '/define:UNITY_EDITOR;UNITY_INCLUDE_TESTS', ('/out:"' + $binary + '"'))
    $arguments += @($references | ForEach-Object { '/reference:"' + $_ + '"' })
    $arguments += @($sources | ForEach-Object { '"' + $_ + '"' })
    [IO.File]::WriteAllLines($response, $arguments, $utf8)
    $diagnostics = @(& $dotnet $csc '/noconfig' ('@' + $response) 2>&1)
    $compileExit = $LASTEXITCODE
    $log = if ($diagnostics.Count) { $diagnostics -join "`n" } else { 'Compilation succeeded with no diagnostics.' }
    [IO.File]::WriteAllText((Join-Path $output ($name + '.compile.log')), $log, $utf8)
    if ($compileExit -ne 0) { throw ("Compilation failed: $name`n" + ($diagnostics -join "`n")) }
    $compiled[$name] = $binary
    $report += [pscustomobject]@{ Assembly = $name; Sources = $sources; References = $references; NoEngineReferences = $definition.noEngineReferences; Outcome = 'Compiled; not Unity-imported or executed' }
    Write-Output "PASS: $name ($($sources.Count) source files, C# 9, warnings as errors)."
}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $resultPath
Write-Output 'Compiled actual sources from all four asmdefs. This is independent Roslyn compilation, not a Unity import/test execution.'
