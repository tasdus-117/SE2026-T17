$ErrorActionPreference = 'Stop'
$validator = Join-Path $PSScriptRoot 'Assert-WindowsStandaloneBuild.ps1'
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('SmallArena-BuildGate-' + [guid]::NewGuid().ToString('N'))
$scratch = [IO.Path]::GetFullPath($scratch)
if ((Split-Path -Parent $scratch) -ne [IO.Path]::GetTempPath().TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Unsafe fixture path.' }
$source = Split-Path -Parent $PSScriptRoot
$cases = @(
    @{ Name = 'valid scenes'; ScenesOnly = $true },
    @{ Name = 'valid player layout' },
    @{ Name = 'wrong scene order'; Fault = 'Order'; Diagnostic = 'Enabled scenes' },
    @{ Name = 'required scene disabled'; Fault = 'Disabled'; Diagnostic = 'Enabled scenes' },
    @{ Name = 'missing scene'; Fault = 'Scene'; Diagnostic = 'Missing/empty build scene' },
    @{ Name = 'wrong scene GUID'; Fault = 'Guid'; Diagnostic = 'GUID mismatch' },
    @{ Name = 'missing exe'; Fault = 'Exe'; Diagnostic = 'Missing/empty build output' },
    @{ Name = 'empty exe'; Fault = 'EmptyExe'; Diagnostic = 'Missing/empty build output' },
    @{ Name = 'invalid exe signature'; Fault = 'Signature'; Diagnostic = 'MZ signature' },
    @{ Name = 'missing player DLL'; Fault = 'Dll'; Diagnostic = 'Missing/empty build output' },
    @{ Name = 'missing data'; Fault = 'Data'; Diagnostic = 'Missing/empty SmallArena_Data' },
    @{ Name = 'empty data'; Fault = 'EmptyData'; Diagnostic = 'Missing/empty SmallArena_Data' },
    @{ Name = 'missing report'; Fault = 'Report'; Diagnostic = 'Missing/empty build output' },
    @{ Name = 'malformed report'; Fault = 'CorruptReport'; Diagnostic = 'JSON' },
    @{ Name = 'failed BuildReport'; Fault = 'Failed'; Diagnostic = 'BuildReport is invalid' },
    @{ Name = 'nonzero errors'; Fault = 'Errors'; Diagnostic = 'BuildReport is invalid' },
    @{ Name = 'wrong report scenes'; Fault = 'ReportScenes'; Diagnostic = 'BuildReport is invalid' },
    @{ Name = 'wrong Unity version'; Fault = 'Version'; Diagnostic = 'BuildReport is invalid' },
    @{ Name = 'missing build log'; Fault = 'Log'; Diagnostic = 'Missing/empty build log' },
    @{ Name = 'project compiler warning'; Fault = 'Warning'; Diagnostic = 'Project compiler warning/error' },
    @{ Name = 'project Unity analyzer warning'; Fault = 'Analyzer'; Diagnostic = 'Project compiler warning/error' }
)
$failed = 0
try {
    for ($index = 0; $index -lt $cases.Count; $index++) {
        $case = $cases[$index]
        $root = Join-Path $scratch $index
        foreach ($folder in @('ProjectSettings', 'Assets/_Project/Scenes', 'Builds/WindowsStandalone/SmallArena_Data', 'Logs/WindowsStandalone')) {
            New-Item -ItemType Directory -Path (Join-Path $root $folder) -Force | Out-Null
        }
        $settings = Join-Path $root 'ProjectSettings/EditorBuildSettings.asset'
        Copy-Item -LiteralPath (Join-Path $source 'ProjectSettings/EditorBuildSettings.asset') -Destination $settings
        Copy-Item -LiteralPath (Join-Path $source 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $root 'ProjectSettings/ProjectVersion.txt')
        foreach ($name in @('MainMenu', 'Bootstrap', 'Arena_MVP')) {
            [IO.File]::WriteAllText((Join-Path $root "Assets/_Project/Scenes/$name.unity"), '%YAML 1.1')
            Copy-Item -LiteralPath (Join-Path $source "Assets/_Project/Scenes/$name.unity.meta") -Destination (Join-Path $root "Assets/_Project/Scenes/$name.unity.meta")
        }
        $output = Join-Path $root 'Builds/WindowsStandalone'
        $exe = Join-Path $output 'SmallArena.exe'
        [IO.File]::WriteAllBytes($exe, [byte[]]@(0x4D, 0x5A, 1))
        [IO.File]::WriteAllText((Join-Path $output 'UnityPlayer.dll'), 'synthetic fixture, not a runtime DLL')
        $data = Join-Path $output 'SmallArena_Data/data.bin'
        [IO.File]::WriteAllText($data, 'synthetic fixture, not a real player')
        $report = Join-Path $output 'build-report.json'
        $manifest = @{ UnityVersion = '6000.6.8f1'; Target = 'StandaloneWindows64'; Result = 'Succeeded'; Backend = 'Mono2x'; Errors = 0; Warnings = 0; Bytes = 3; Scenes = @('Assets/_Project/Scenes/MainMenu.unity', 'Assets/_Project/Scenes/Bootstrap.unity', 'Assets/_Project/Scenes/Arena_MVP.unity') }
        $log = Join-Path $root 'Logs/WindowsStandalone/build.log'
        [IO.File]::WriteAllText($log, 'Synthetic successful build')
        switch ($case.Fault) {
            'Order' { [IO.File]::WriteAllText($settings, [IO.File]::ReadAllText($settings).Replace('MainMenu.unity', 'Wrong.unity')) }
            'Disabled' { [IO.File]::WriteAllText($settings, [IO.File]::ReadAllText($settings).Replace('enabled: 1', 'enabled: 0')) }
            'Scene' { Remove-Item -LiteralPath (Join-Path $root 'Assets/_Project/Scenes/MainMenu.unity') }
            'Guid' { [IO.File]::WriteAllText((Join-Path $root 'Assets/_Project/Scenes/MainMenu.unity.meta'), 'guid: 00000000000000000000000000000000') }
            'Exe' { Remove-Item -LiteralPath $exe }
            'EmptyExe' { [IO.File]::WriteAllBytes($exe, [byte[]]@()) }
            'Signature' { [IO.File]::WriteAllText($exe, 'not an executable') }
            'Dll' { Remove-Item -LiteralPath (Join-Path $output 'UnityPlayer.dll') }
            'Data' { Remove-Item -LiteralPath $data; Remove-Item -LiteralPath (Split-Path -Parent $data) }
            'EmptyData' { Remove-Item -LiteralPath $data }
            'Failed' { $manifest.Result = 'Failed' }
            'Errors' { $manifest.Errors = 1 }
            'ReportScenes' { $manifest.Scenes = @('Wrong') }
            'Version' { $manifest.UnityVersion = '6000.6.7f1' }
            'Log' { Remove-Item -LiteralPath $log }
            'Warning' { [IO.File]::WriteAllText($log, 'Assets/_Project/Scripts/Example.cs(1,1): warning CS0168: unused variable') }
            'Analyzer' { [IO.File]::WriteAllText($log, 'Warning: Assets/_Project/Scripts/Example.cs(1,1): warning UAC1009: unsupported serialized type') }
        }
        $manifest | ConvertTo-Json | Set-Content -LiteralPath $report
        if ($case.Fault -eq 'Report') { Remove-Item -LiteralPath $report }
        if ($case.Fault -eq 'CorruptReport') { [IO.File]::WriteAllText($report, '{broken') }
        $caught = $null
        try { & $validator -ProjectPath $root -ScenesOnly:([bool]$case.ScenesOnly) | Out-Null }
        catch { $caught = $_.ToString() }
        if (($case.Fault -and ($null -eq $caught -or $caught -notlike ('*' + $case.Diagnostic + '*'))) -or
            (-not $case.Fault -and $null -ne $caught)) {
            $failed++; Write-Output "FAIL: $($case.Name): $caught"
        } else { Write-Output "PASS: $($case.Name)" }
    }
} finally { Remove-Item -LiteralPath $scratch -Recurse -Force }
Write-Output "Windows build validator: total=$($cases.Count), passed=$($cases.Count - $failed), failed=$failed; $($PSVersionTable.PSVersion)"
if ($failed -gt 0) { exit 1 }
exit 0
