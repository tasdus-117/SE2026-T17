param([string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$output = Join-Path $taskRoot 'TestResults/ArenaLayout'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$dotnet = Join-Path $EditorRoot 'Data/NetCoreRuntime/dotnet.exe'
$csc = Join-Path $EditorRoot 'Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$referencePath = Join-Path $EditorRoot 'Data/DotNetSdk/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0'
$framework = @(Get-ChildItem -LiteralPath $referencePath -Filter '*.dll' | ForEach-Object { $_.FullName })
$yaml = Join-Path $EditorRoot 'Data/MSBuild/msbuild.host/YamlDotNet.dll'
$binary = Join-Path $output 'ArenaLayoutVerifier.dll'
$arguments = @($csc, '/nologo', '/noconfig', '/nostdlib+', '/langversion:9.0', '/warnaserror+', '/nowarn:1701', '/target:exe', ('/out:' + $binary))
$arguments += @((@($framework) + @($yaml)) | ForEach-Object { '/reference:' + $_ })
$arguments += Join-Path $PSScriptRoot 'ArenaLayoutVerifier.cs'
$diagnostics = @(& $dotnet @arguments 2>&1)
if ($LASTEXITCODE -ne 0) { throw ("Layout verifier compilation failed.`n" + ($diagnostics -join "`n")) }
Copy-Item -LiteralPath $yaml -Destination (Join-Path $output 'YamlDotNet.dll') -Force
$runtime = '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}'
[IO.File]::WriteAllText((Join-Path $output 'ArenaLayoutVerifier.runtimeconfig.json'), $runtime, (New-Object Text.UTF8Encoding($false)))
$report = Join-Path $output 'results.json'
if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report -Force }
try {
    $ErrorActionPreference = 'Continue'
    $messages = @(& $dotnet $binary $taskRoot $EditorRoot $report 2>&1)
    $result = $LASTEXITCODE
} finally { $ErrorActionPreference = 'Stop' }
$messages | Set-Content -LiteralPath (Join-Path $output 'results.log')
$messages | Write-Output
if ($result -ne 0) { throw 'Saved arena layout verification failed.' }

$engine = Join-Path $EditorRoot 'Data/Managed/UnityEngine'
$testRunner = Join-Path $EditorRoot 'Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.urp-blank-17.2.6/ScriptAssemblies/UnityEngine.TestRunner.dll'
$nunit = Join-Path $EditorRoot 'Data/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll'
$probeReferences = @($framework) + @((Join-Path $engine 'UnityEngine.CoreModule.dll'), (Join-Path $engine 'UnityEngine.PhysicsModule.dll'), $testRunner, $nunit)
$arguments = @($csc, '/nologo', '/noconfig', '/nostdlib+', '/langversion:9.0', '/warnaserror+', '/target:library', ('/out:' + (Join-Path $output 'SmallArena.PlayMode.Tests.dll')))
$arguments += @($probeReferences | ForEach-Object { '/reference:' + $_ })
$arguments += @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Assets/_Project/Tests/PlayMode') -Filter '*.cs' | ForEach-Object { $_.FullName })
$diagnostics = @(& $dotnet @arguments 2>&1)
$probeExit = $LASTEXITCODE
$diagnostics | Set-Content -LiteralPath (Join-Path $output 'PlayMode.compile.log')
if ($probeExit -ne 0) { throw ("PlayMode compilation failed.`n" + ($diagnostics -join "`n")) }
Write-Output 'PASS: all PlayMode fixture sources compile against installed Unity/NUnit APIs (compile only).'
Write-Output "Report: $report"
