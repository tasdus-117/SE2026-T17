param([string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor')

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$output = Join-Path $taskRoot 'TestResults/ArenaConfiguration'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$dotnet = Join-Path $EditorRoot 'Data/NetCoreRuntime/dotnet.exe'
$csc = Join-Path $EditorRoot 'Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$referencePath = Join-Path $EditorRoot 'Data/DotNetSdk/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0'
$framework = @(Get-ChildItem -LiteralPath $referencePath -Filter '*.dll' | ForEach-Object { $_.FullName })
$yaml = Join-Path $EditorRoot 'Data/MSBuild/msbuild.host/YamlDotNet.dll'
$references = @($framework) + @($yaml)
$binary = Join-Path $output 'ArenaConfigurationVerifier.dll'
# The bundled YamlDotNet targets .NET 7; this .NET 8 verification host supplies
# the compatible newer System.Runtime. Suppress only that reference-unification warning.
$arguments = @($csc, '/nologo', '/noconfig', '/nostdlib+', '/langversion:9.0', '/warnaserror+', '/nowarn:1701', '/target:exe', ('/out:' + $binary))
$arguments += @($references | ForEach-Object { '/reference:' + $_ })
$arguments += Join-Path $PSScriptRoot 'ArenaConfigurationVerifier.cs'
$diagnostics = @(& $dotnet @arguments 2>&1)
if ($LASTEXITCODE -ne 0) { throw ("Configuration verifier compilation failed.`n" + ($diagnostics -join "`n")) }
Copy-Item -LiteralPath $yaml -Destination (Join-Path $output 'YamlDotNet.dll') -Force
$runtime = '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}'
[IO.File]::WriteAllText((Join-Path $output 'ArenaConfigurationVerifier.runtimeconfig.json'), $runtime, (New-Object Text.UTF8Encoding($false)))
$report = Join-Path $output 'results.json'
if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report -Force }
try {
    $ErrorActionPreference = 'Continue'
    $messages = @(& $dotnet $binary $taskRoot $EditorRoot $report 2>&1)
    $runExit = $LASTEXITCODE
} finally { $ErrorActionPreference = 'Stop' }
$messages | Set-Content -LiteralPath (Join-Path $output 'results.log')
$messages | Write-Output
if ($runExit -ne 0) { throw 'Saved arena configuration verification failed.' }

# Compile the actual PlayMode probe against the installed Unity APIs. Execution
# of these native-engine assertions still requires the Unity Test Runner.
$engine = Join-Path $EditorRoot 'Data/Managed/UnityEngine'
$nunit = Join-Path $EditorRoot 'Data/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll'
$probeReferences = @($framework) + @((Join-Path $engine 'UnityEngine.CoreModule.dll'), (Join-Path $engine 'UnityEngine.PhysicsModule.dll'), $nunit)
$probeBinary = Join-Path $output 'SmallArena.PlayMode.Tests.dll'
$probeArguments = @($csc, '/nologo', '/noconfig', '/nostdlib+', '/langversion:9.0', '/warnaserror+', '/target:library', ('/out:' + $probeBinary))
$probeArguments += @($probeReferences | ForEach-Object { '/reference:' + $_ })
$probeArguments += @((Join-Path $taskRoot 'Assets/_Project/Tests/PlayMode/AssemblyInfo.cs'), (Join-Path $taskRoot 'Assets/_Project/Tests/PlayMode/ArenaConfigurationTests.cs'))
$probeDiagnostics = @(& $dotnet @probeArguments 2>&1)
$probeExit = $LASTEXITCODE
$probeDiagnostics | Set-Content -LiteralPath (Join-Path $output 'PlayModeProbe.compile.log')
if ($probeExit -ne 0) { throw ("PlayMode probe compilation failed.`n" + ($probeDiagnostics -join "`n")) }
Write-Output 'PASS: PlayMode configuration probe compiles against installed Unity APIs (compile only).'
Write-Output "Report: $report"
