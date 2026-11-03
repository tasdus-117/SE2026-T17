param(
    [string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor',
    [switch]$CompileProbes
)

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskRoot

function Assert-Scaffold([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$expected = [ordered]@{
    'SmallArena.Shared.Domain' = @()
    'SmallArena.Shared.Protocol' = @('SmallArena.Shared.Domain')
    'SmallArena.Networking' = @('Mirror', 'SmallArena.Shared.Domain', 'SmallArena.Shared.Protocol')
    'SmallArena.Server' = @('Mirror', 'SmallArena.Shared.Domain', 'SmallArena.Shared.Protocol', 'SmallArena.Networking')
    'SmallArena.Client' = @('Mirror', 'SmallArena.Shared.Domain', 'SmallArena.Shared.Protocol', 'SmallArena.Networking', 'Unity.InputSystem', 'Unity.TextMeshPro', 'UnityEngine.UI')
    'SmallArena.EditMode.Tests' = @('SmallArena.Shared.Domain')
    'SmallArena.PlayMode.Tests' = @('SmallArena.Shared.Domain', 'SmallArena.Shared.Protocol', 'SmallArena.Networking', 'SmallArena.Server', 'SmallArena.Client', 'Unity.InputSystem', 'Unity.InputSystem.TestFramework', 'Unity.TextMeshPro', 'UnityEngine.UI')
}
$definitions = @(Get-ChildItem -LiteralPath 'Assets/_Project' -Filter '*.asmdef' -Recurse)
Assert-Scaffold ($definitions.Count -eq $expected.Count) 'Expected exactly seven project asmdefs.'
$byName = @{}
foreach ($file in $definitions) {
    $definition = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    Assert-Scaffold ($expected.Contains($definition.name)) "Unexpected assembly: $($definition.name)"
    Assert-Scaffold (-not $byName.ContainsKey($definition.name)) 'Duplicate assembly name.'
    Assert-Scaffold ((@($definition.references) -join '|') -eq ($expected[$definition.name] -join '|')) "References differ: $($definition.name)"
    if ($definition.name -like '*.Tests') {
        Assert-Scaffold ($definition.overrideReferences -eq $true) 'Test overrideReferences must be enabled.'
        Assert-Scaffold ('TestAssemblies' -in $definition.optionalUnityReferences) 'Test assembly flag missing.'
        Assert-Scaffold (@($definition.references | Where-Object { $_ -in @('UnityEngine.TestRunner', 'UnityEditor.TestRunner') }).Count -eq 0) 'Test runner references must be implicit through TestAssemblies.'
        Assert-Scaffold ('nunit.framework.dll' -in $definition.precompiledReferences) 'NUnit reference missing.'
    }
    $byName[$definition.name] = @{ File = $file; Definition = $definition }
}
$domain = $byName['SmallArena.Shared.Domain'].Definition
function Visit-Assembly([string]$Name) {
    if ($visiting.ContainsKey($Name)) { throw "Circular assembly dependency at $Name" }
    if ($visited.ContainsKey($Name)) { return }
    $visiting[$Name] = $true
    foreach ($reference in $byName[$Name].Definition.references) {
        if ($byName.ContainsKey($reference)) { Visit-Assembly $reference }
    }
    $visiting.Remove($Name)
    $visited[$Name] = $true
}
$visiting = @{}
$visited = @{}
foreach ($name in $byName.Keys) { Visit-Assembly $name }
Write-Output 'PASS: Project assembly graph is acyclic; dependency directions match the architecture.'
Assert-Scaffold ($domain.noEngineReferences -eq $true) 'Domain must exclude Unity engine references.'
Assert-Scaffold ($domain.overrideReferences -eq $true -and @($domain.precompiledReferences).Count -eq 0) 'Domain must exclude automatic plugin references.'
$protocol = $byName['SmallArena.Shared.Protocol'].Definition
Assert-Scaffold ($protocol.noEngineReferences -eq $true -and $protocol.overrideReferences -eq $true) 'Protocol must remain engine/plugin independent.'
Assert-Scaffold ((@($byName['SmallArena.EditMode.Tests'].Definition.includePlatforms) -join '|') -eq 'Editor') 'EditMode must be Editor only.'
Assert-Scaffold (@($byName['SmallArena.PlayMode.Tests'].Definition.includePlatforms).Count -eq 0) 'PlayMode must not be Editor-only.'

$requiredFolders = @('Art/Materials', 'Input', 'Scenes', 'Settings', 'Tests/EditMode', 'Tests/PlayMode', 'Scripts/Shared/Domain', 'Scripts/Shared/Protocol', 'Scripts/Networking/Transport', 'Scripts/Networking/Replication', 'Scripts/Server/Simulation', 'Scripts/Server/Sessions', 'Scripts/Server/Validation', 'Scripts/Server/Combat', 'Scripts/Client/Networking', 'Scripts/Client/Input', 'Scripts/Client/Presentation', 'Scripts/Client/UI', 'Scripts/Editor/Build', 'Prefabs/Characters', 'Prefabs/Combat', 'Prefabs/Environment', 'Prefabs/UI')
foreach ($folder in $requiredFolders) {
    Assert-Scaffold (Test-Path -LiteralPath (Join-Path 'Assets/_Project' $folder) -PathType Container) "Missing folder: $folder"
}
$assets = @(Get-Item -LiteralPath 'Assets') + @(Get-ChildItem -LiteralPath 'Assets' -Recurse -Force | Where-Object { $_.Extension -ne '.meta' -and -not $_.Name.StartsWith('.') })
foreach ($asset in $assets) {
    Assert-Scaffold (Test-Path -LiteralPath ($asset.FullName + '.meta')) "Missing metadata: $($asset.FullName)"
}
$guids = @{}
foreach ($meta in @(Get-Item -LiteralPath 'Assets.meta') + @(Get-ChildItem -LiteralPath 'Assets' -Recurse -Filter '*.meta')) {
    $match = [regex]::Match([IO.File]::ReadAllText($meta.FullName), '(?mi)^guid: ([0-9a-f]{32})\s*$')
    Assert-Scaffold $match.Success "Invalid GUID: $($meta.FullName)"
    Assert-Scaffold (-not $guids.ContainsKey($match.Groups[1].Value)) "Duplicate GUID: $($meta.FullName)"
    $guids[$match.Groups[1].Value] = $meta.FullName
    Assert-Scaffold (Test-Path -LiteralPath ($meta.FullName.Substring(0, $meta.FullName.Length - 5))) "Orphan metadata: $($meta.FullName)"
}
Assert-Scaffold ([IO.File]::ReadAllText((Join-Path $taskRoot 'ProjectSettings/EditorSettings.asset')) -match 'm_SerializationMode: 2') 'Force Text is disabled.'
Assert-Scaffold ([IO.File]::ReadAllText((Join-Path $taskRoot 'ProjectSettings/VersionControlSettings.asset')) -match 'm_Mode: Visible Meta Files') 'Visible Meta Files is disabled.'
Assert-Scaffold ([IO.File]::ReadAllText((Join-Path $taskRoot 'ProjectSettings/ProjectVersion.txt')) -match 'm_EditorVersion: 6000.6.7f1') 'Unexpected Editor version.'
$graphics = [IO.File]::ReadAllText((Join-Path $taskRoot 'ProjectSettings/GraphicsSettings.asset'))
$pipelineMatch = [regex]::Match($graphics, 'm_CustomRenderPipeline: .*guid: ([0-9a-f]{32})')
Assert-Scaffold ($pipelineMatch.Success -and $guids.ContainsKey($pipelineMatch.Groups[1].Value)) 'Default URP asset is missing or unassigned.'
$quality = [IO.File]::ReadAllText((Join-Path $taskRoot 'ProjectSettings/QualitySettings.asset'))
foreach ($pipeline in [regex]::Matches($quality, 'customRenderPipeline: .*guid: ([0-9a-f]{32})')) {
    Assert-Scaffold ($guids.ContainsKey($pipeline.Groups[1].Value)) 'Quality profile refers to a missing URP asset.'
}
$buildSettings = [IO.File]::ReadAllText((Join-Path $taskRoot 'ProjectSettings/EditorBuildSettings.asset'))
Assert-Scaffold ($buildSettings -match 'path: Assets/_Project/Scenes/Arena_MVP.unity') 'Arena scene is not configured.'
Assert-Scaffold (Test-Path -LiteralPath 'Assets/_Project/Scenes/Arena_MVP.unity') 'Arena scene is missing.'
$scenes = [regex]::Matches($buildSettings, '(?m)^  - enabled: 1\r?\n    path: (.+)\r?\n    guid: ([0-9a-f]{32})')
Assert-Scaffold ($scenes.Count -eq 3 -and $scenes[0].Groups[1].Value.Trim() -eq 'Assets/_Project/Scenes/MainMenu.unity' -and $scenes[1].Groups[1].Value.Trim() -eq 'Assets/_Project/Scenes/Bootstrap.unity' -and $scenes[2].Groups[1].Value.Trim() -eq 'Assets/_Project/Scenes/Arena_MVP.unity') 'MainMenu, Bootstrap and Arena must be enabled at indices 0, 1 and 2.'
foreach ($scene in $scenes) {
    $scenePath = $scene.Groups[1].Value.Trim()
    Assert-Scaffold ((Get-Content -LiteralPath ($scenePath + '.meta') -Raw) -match ('guid: ' + $scene.Groups[2].Value)) "Build scene GUID mismatch: $scenePath"
}
$bootstrap = Get-Content -LiteralPath 'Assets/_Project/Scenes/Bootstrap.unity' -Raw
Assert-Scaffold ($bootstrap -match 'm_Name: _NetworkManager' -and $bootstrap -match 'guid: 8aab4c8111b7c411b9b92cf3dbc5bd4e' -and $bootstrap -match 'guid: 6b0fecffa3f624585964b0d0eb21b18e') 'Bootstrap must contain the standard Mirror manager and KCP transport.'
Assert-Scaffold ($bootstrap -match 'transport: \{fileID: 1003\}' -and $bootstrap -match 'onlineScene: Assets/_Project/Scenes/Arena_MVP.unity' -and $bootstrap -match 'offlineScene: Assets/_Project/Scenes/Bootstrap.unity') 'Bootstrap scene routing or transport reference is invalid.'
Write-Output 'PASS: MainMenu (index 0), Bootstrap (index 1), Arena_MVP (index 2), scene GUIDs, Mirror components and scene routing.'

$manifest = Get-Content -LiteralPath 'Packages/manifest.json' -Raw | ConvertFrom-Json
$lock = Get-Content -LiteralPath 'Packages/packages-lock.json' -Raw | ConvertFrom-Json
foreach ($dependency in $manifest.dependencies.PSObject.Properties) {
    $entry = $lock.dependencies.PSObject.Properties[$dependency.Name].Value
    Assert-Scaffold ($null -ne $entry -and $entry.version -eq $dependency.Value -and $entry.depth -eq 0) "Manifest/lock mismatch: $($dependency.Name)"
}
foreach ($entry in $lock.dependencies.PSObject.Properties) {
    Assert-Scaffold ($entry.Name -notmatch 'netcode|multiplayer|transport|services|collab|fusion|mirror') "Out-of-scope package: $($entry.Name)"
    foreach ($dependency in $entry.Value.dependencies.PSObject.Properties) {
        Assert-Scaffold ($null -ne $lock.dependencies.PSObject.Properties[$dependency.Name]) "Missing transitive dependency: $($dependency.Name)"
    }
}
foreach ($guide in @('PROJECT_CONTEXT.md', 'AGENTS.md', 'README.md', 'docs/architecture/structure.md', 'docs/architecture/authority.md', 'docs/protocol/protocol.md', 'docs/testing/test-plan.md', 'docs/operations/runbook.md')) {
    Assert-Scaffold (Test-Path -LiteralPath (Join-Path $taskRoot $guide)) "Missing team/context guide: $guide"
}
$tracked = @(git ls-files)
Assert-Scaffold ($LASTEXITCODE -eq 0) 'Cannot inspect Git index.'
Assert-Scaffold (@($tracked | Where-Object { $_ -match '^(Library|Temp|Obj|Build|Builds|Logs|UserSettings)/|\.(csproj|sln)$' }).Count -eq 0) 'Generated data is tracked.'
Write-Output "PASS: 7 project asmdefs, $($assets.Count) asset/folder metadata pairs, unique GUIDs, settings/URP/scene, package closure, team/context guides, Git hygiene."

if (-not $CompileProbes) { exit 0 }

# These are independent Roslyn checks, not a substitute for licensed Unity import.
# All generated sources, binaries and diagnostics stay in ignored Logs/.
$output = Join-Path $taskRoot 'Logs/ScaffoldVerification'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$dotnet = Join-Path $EditorRoot 'Data/DotNetSdk/dotnet.exe'
$csc = Join-Path $EditorRoot 'Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$referencePath = Join-Path $EditorRoot 'Data/DotNetSdk/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0'
$framework = @(Get-ChildItem -LiteralPath $referencePath -Filter '*.dll' | ForEach-Object { $_.FullName })
$engine = Join-Path $EditorRoot 'Data/Managed/UnityEngine/UnityEngine.CoreModule.dll'

function Invoke-Probe([string]$Name, [string[]]$Sources, [string[]]$References, [bool]$ExpectFailure = $false) {
    $binary = Join-Path $output ($Name + '.dll')
    $arguments = @($csc, '/nologo', '/noconfig', '/nostdlib+', '/langversion:9.0', '/target:library', '/warnaserror+', ('/out:' + $binary))
    $arguments += @($References | ForEach-Object { '/reference:' + $_ })
    $arguments += $Sources
    $diagnostics = @(& $dotnet @arguments 2>&1)
    $compilerExit = $LASTEXITCODE
    $diagnostics | Set-Content -LiteralPath (Join-Path $output ($Name + '.log'))
    if ($ExpectFailure) {
        Assert-Scaffold ($compilerExit -ne 0 -and ($diagnostics -join "`n") -match 'CS0246.*UnityEngine') 'Negative Domain engine probe did not fail as expected.'
    } else {
        Assert-Scaffold ($compilerExit -eq 0) ("Compilation failed: $Name`n" + ($diagnostics -join "`n"))
    }
    Write-Output "PASS: $Name"
}

& (Join-Path $PSScriptRoot 'Compile-UnityAssemblies.ps1') -EditorRoot $EditorRoot -OutputDirectory $output
$probeSource = Join-Path $output 'EngineBoundaryProbe.cs'
Set-Content -LiteralPath $probeSource -Value 'public sealed class EngineBoundaryProbe { public UnityEngine.Vector3 Position; }'
Invoke-Probe 'DomainEngineAccessRejected' @($probeSource) $framework $true
Invoke-Probe 'EngineAccessPositiveControl' @($probeSource) (@($framework) + @($engine))
Write-Output 'Independent C# checks passed. Unity Editor import/compilation still requires a valid Editor license.'
