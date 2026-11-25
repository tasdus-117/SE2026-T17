# Called inside the same Windows Editor image used by unity-test-runner.
param([Parameter(Mandatory = $true)][ValidateSet('Scaffold', 'Compilation', 'ArenaConfiguration', 'ArenaLayout')][string]$Gate)
$ErrorActionPreference = 'Stop'
$editor = & (Join-Path $PSScriptRoot 'Resolve-UnityEditor.ps1')
switch ($Gate) {
    'Scaffold' { & (Join-Path $PSScriptRoot 'Verify-Scaffold.ps1') -EditorRoot $editor }
    'Compilation' { & (Join-Path $PSScriptRoot 'Compile-UnityAssemblies.ps1') -EditorRoot $editor }
    'ArenaConfiguration' { & (Join-Path $PSScriptRoot 'Verify-ArenaConfiguration.ps1') -EditorRoot $editor }
    'ArenaLayout' { & (Join-Path $PSScriptRoot 'Verify-ArenaLayout.ps1') -EditorRoot $editor -SkipCompilation }
}
exit 0
