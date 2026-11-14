param(
    [string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor',
    [string]$OutputDirectory = 'TestResults/LobbySearch',
    [switch]$Baseline,
    [switch]$Load
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $root
$output = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
if (-not $output.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be inside this worktree.' }
New-Item -ItemType Directory -Force -Path $output | Out-Null
$dotnet = Join-Path $EditorRoot 'Data/NetCoreRuntime/dotnet.exe'
$csc = Join-Path $EditorRoot 'Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$references = @(Get-ChildItem (Join-Path $EditorRoot 'Data/DotNetSdk/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0') -Filter '*.dll' | ForEach-Object { '/reference:' + $_.FullName })
$nunit = Join-Path $EditorRoot 'Data/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll'
$utf8 = New-Object Text.UTF8Encoding($false)
$defines = 'LOBBY_PORTABLE'
if ($Baseline) { $defines += ';LOBBY_BASELINE' }
function Compile([string]$Name, [string[]]$Sources, [string]$Target = 'library', [string[]]$Extra = @()) {
    & $dotnet $csc /nologo /noconfig /nostdlib+ /langversion:9.0 /warnaserror+ "/target:$Target" "/out:$output/$Name.dll" "/define:$defines" @references @Extra @Sources
    if ($LASTEXITCODE -ne 0) { throw "Compile failed: $Name" }
}
$sha = (& git rev-parse HEAD).Trim()
$patch = (& git diff HEAD -- Assets tools docs README.md) -join "`n"
$hash = [Security.Cryptography.SHA256]::Create().ComputeHash($utf8.GetBytes($patch))
$patchId = ([BitConverter]::ToString($hash)).Replace('-','').ToLowerInvariant()
$start = [DateTime]::UtcNow.ToString('o')
& git diff HEAD | Set-Content -LiteralPath (Join-Path $output 'tested.patch')
# Only a marker interface is substituted: no fake serializer or fake lobby implementation.
[IO.File]::WriteAllText((Join-Path $output 'MirrorMarker.cs'), 'namespace Mirror { public interface NetworkMessage {} }', $utf8)
Compile 'SmallArena.Shared.Domain' @((Join-Path $root 'Assets/_Project/Scripts/Shared/Domain/MatchState.cs'))
Compile 'SmallArena.EditMode.Tests' @(
    (Join-Path $output 'MirrorMarker.cs'),
    (Join-Path $root 'Assets/_Project/Scripts/Networking/LobbyMessages.cs'),
    (Join-Path $root 'Assets/_Project/Scripts/Server/Sessions/LobbyRoom.cs'),
    (Join-Path $root 'Assets/_Project/Tests/PlayMode/LobbyMemberSearchTests.cs')
) 'library' @('/reference:' + $nunit)
Copy-Item -LiteralPath $nunit -Destination (Join-Path $output 'nunit.framework.dll') -Force
Compile 'NUnitDomainRunner' @((Join-Path $root 'tools/NUnitDomainRunner.cs')) 'exe' @('/reference:' + $nunit)
$runtime = '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}'
[IO.File]::WriteAllText((Join-Path $output 'NUnitDomainRunner.runtimeconfig.json'), $runtime, $utf8)
$run = @(& $dotnet (Join-Path $output 'NUnitDomainRunner.dll') (Join-Path $output 'SmallArena.EditMode.Tests.dll') (Join-Path $output 'results.xml') 2>&1)
$code = $LASTEXITCODE
$run | Set-Content (Join-Path $output 'results.log')
$run | Write-Output
$command = "powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-LobbySearchTests.ps1 -EditorRoot '$EditorRoot' -OutputDirectory '$OutputDirectory'"
if ($Baseline) { $command += ' -Baseline' }
if ($Load) { $command += ' -Load' }
$inventory = @(Get-FileHash -Algorithm SHA256 -LiteralPath 'Assets/_Project/Scripts/Server/Sessions/LobbyRoom.cs','Assets/_Project/Scripts/Networking/LobbyMessages.cs','Assets/_Project/Tests/PlayMode/LobbyMemberSearchTests.cs','tools/Run-LobbySearchTests.ps1' | Select-Object @{Name='File';Expression={Split-Path -Leaf $_.Path}},Hash)
$info = [ordered]@{ utc = $start; sha = $sha; patchSha256 = $patchId; command = $command; files = $inventory; baseline = [bool]$Baseline; exitCode = $code; runtime = (& $dotnet --info | Out-String); boundary = 'Actual LobbyRoom + LobbyMessages with Mirror marker only; no serialization, transport, engine, or UI' }
$info | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'run.json')
if ($code -ne 0) { exit $code }
if ($Load) {
    $loadStart = [DateTime]::UtcNow.ToString('o')
    Compile 'LobbySearchLoad' @((Join-Path $root 'tools/LobbySearchLoad.cs')) 'exe' @('/reference:' + (Join-Path $output 'SmallArena.EditMode.Tests.dll'))
    [IO.File]::WriteAllText((Join-Path $output 'LobbySearchLoad.runtimeconfig.json'), $runtime, $utf8)
    & $dotnet (Join-Path $output 'LobbySearchLoad.dll') $output
    $loadCode = $LASTEXITCODE
    [ordered]@{ utc = $loadStart; sha = $sha; command = $command; exitCode = $loadCode; harnessSha256 = (Get-FileHash -LiteralPath 'tools/LobbySearchLoad.cs' -Algorithm SHA256).Hash; runtime = $info.runtime; processors = [Environment]::ProcessorCount; os = [Environment]::OSVersion.VersionString; summary = @(Get-Content (Join-Path $output 'load-summary.txt')) } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'load-run.json')
    exit $loadCode
}
exit 0
