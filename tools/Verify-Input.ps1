param([string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$output = Join-Path $taskRoot 'TestResults/Input'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$script:checks = 0
function Assert-Input([bool]$Condition, [string]$Diagnostic) {
    if (-not $Condition) { throw $Diagnostic }
    $script:checks++
}
$assetPath = Join-Path $taskRoot 'Assets/_Project/Input/SmallArena.inputactions'
$asset = Get-Content -LiteralPath $assetPath -Raw | ConvertFrom-Json
Assert-Input ($asset.name -eq 'SmallArena' -and @($asset.maps).Count -eq 1) 'Expected one SmallArena action map.'
$map = $asset.maps[0]
Assert-Input ($map.name -eq 'Gameplay' -and @($map.actions).Count -eq 4) 'Expected Gameplay with four actions.'
$expectedButtons = [ordered]@{ Jump = '<Keyboard>/space'; Punch = '<Keyboard>/j'; Interact = '<Keyboard>/e' }
$move = @($map.actions | Where-Object { $_.name -eq 'Move' })
Assert-Input ($move.Count -eq 1 -and $move[0].type -eq 'Value' -and $move[0].expectedControlType -eq 'Vector2' -and $move[0].initialStateCheck -eq $true) 'Move must be a Vector2 Value action with initial-state check.'
$composite = @($map.bindings | Where-Object { $_.isComposite })
Assert-Input ($composite.Count -eq 1 -and $composite[0].action -eq 'Move' -and $composite[0].path -eq '2DVector(mode=0)' -and -not $composite[0].isPartOfComposite) 'Move must use one normalized 2DVector composite.'
$parts = [ordered]@{ up = '<Keyboard>/w'; down = '<Keyboard>/s'; left = '<Keyboard>/a'; right = '<Keyboard>/d' }
foreach ($part in $parts.Keys) {
    $binding = @($map.bindings | Where-Object { $_.action -eq 'Move' -and $_.name -eq $part })
    Assert-Input ($binding.Count -eq 1 -and $binding[0].path -eq $parts[$part] -and $binding[0].isPartOfComposite -and -not $binding[0].isComposite) "Wrong composite part: $part"
}
foreach ($name in $expectedButtons.Keys) {
    $action = @($map.actions | Where-Object { $_.name -eq $name })
    Assert-Input ($action.Count -eq 1 -and $action[0].type -eq 'Button' -and $action[0].expectedControlType -eq 'Button') "Wrong Button action: $name"
    Assert-Input ($action[0].interactions -eq 'Press(behavior=0)' -and $action[0].initialStateCheck -eq $false) "Button must be press-only without initial-state replay: $name"
    $binding = @($map.bindings | Where-Object { $_.action -eq $name })
    Assert-Input ($binding.Count -eq 1 -and $binding[0].path -eq $expectedButtons[$name] -and -not $binding[0].isComposite -and -not $binding[0].isPartOfComposite) "Wrong keyboard binding: $name"
}
Assert-Input (@($map.bindings).Count -eq 8) 'Expected exactly eight bindings including composite parts.'
$ids = @($map.id) + @($map.actions | ForEach-Object { $_.id }) + @($map.bindings | ForEach-Object { $_.id })
foreach ($id in $ids) {
    $parsed = [guid]::Empty
    Assert-Input ([guid]::TryParse($id, [ref]$parsed)) "Invalid action/binding GUID: $id"
}
Assert-Input (@($ids | Select-Object -Unique).Count -eq $ids.Count) 'Action/binding GUIDs must be unique.'
Assert-Input (@($asset.controlSchemes).Count -eq 1 -and $asset.controlSchemes[0].name -eq 'Keyboard' -and $asset.controlSchemes[0].bindingGroup -eq 'Keyboard') 'Expected keyboard-only control scheme.'
Assert-Input (@($asset.controlSchemes[0].devices).Count -eq 1 -and $asset.controlSchemes[0].devices[0].devicePath -eq '<Keyboard>' -and -not $asset.controlSchemes[0].devices[0].isOptional -and -not $asset.controlSchemes[0].devices[0].isOR) 'Keyboard must be the single required device.'
Assert-Input (@($map.bindings | Where-Object { $_.groups -ne 'Keyboard' -or $_.interactions -ne '' -or $_.processors -ne '' }).Count -eq 0) 'Unexpected binding group/interaction/processor override.'
$meta = [IO.File]::ReadAllText($assetPath + '.meta')
Assert-Input ($meta -match 'ScriptedImporter:' -and $meta -match 'guid: 8404be70184654265930450def6a9037, type: 3' -and $meta -match 'm_GenerateWrapperCode: 0') 'Input action importer metadata or wrapper configuration is wrong.'
$project = [IO.File]::ReadAllText((Join-Path $taskRoot 'ProjectSettings/ProjectSettings.asset'))
Assert-Input ($project -match '(?m)^  activeInputHandler: 1\s*$') 'New Input System handling must remain active.'
$manifest = Get-Content -LiteralPath (Join-Path $taskRoot 'Packages/manifest.json') -Raw | ConvertFrom-Json
Assert-Input (@($manifest.testables).Count -eq 1 -and $manifest.testables[0] -eq 'com.unity.inputsystem') 'InputTestFixture requires the existing Input System package in testables.'
$runtimeSource = [IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/_Project/Scripts/Client/Input/PlayerInputHandler.cs'))
Assert-Input ($runtimeSource -notmatch 'CharacterController|Rigidbody|Physics\.|ApplyDamage|Health|FindObject|FindAnyObject|FindFirstObject') 'Adapter must not implement physics/gameplay/lookups.'
$testSource = [IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/_Project/Tests/PlayMode/PlayerInputHandlerTests.cs'))
$caseCount = [regex]::Matches($testSource, '(?m)^\s*\[(?:Test|TestCase)\b').Count
[pscustomobject]@{ SavedAssetAssertionsPassed = $script:checks; Maps = 1; Actions = 4; Bindings = 8; DeclaredPlayModeCases = $caseCount; NativeExecution = 'Not established by this verifier' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'results.json')
Write-Output "PASS: $script:checks saved-asset/adapter-scope assertions; $caseCount declared PlayMode cases."
& (Join-Path $PSScriptRoot 'Compile-UnityAssemblies.ps1') -EditorRoot $EditorRoot -OutputDirectory (Join-Path $output 'Assemblies')
