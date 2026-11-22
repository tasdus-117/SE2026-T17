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
    @{ Name = 'MZ-only file is not PE'; Fault = 'Truncated'; Diagnostic = 'truncated header' },
    @{ Name = 'invalid PE offset'; Fault = 'Offset'; Diagnostic = 'PE header offset' },
    @{ Name = 'invalid PE signature'; Fault = 'PeSignature'; Diagnostic = 'PE signature' },
    @{ Name = 'x86 is not x64'; Fault = 'Machine'; Diagnostic = 'AMD64' },
    @{ Name = 'PE32 is not PE32+'; Fault = 'Optional'; Diagnostic = 'PE32+' },
    @{ Name = 'incomplete optional header'; Fault = 'OptionalSize'; Diagnostic = 'complete PE32+' },
    @{ Name = 'missing player DLL'; Fault = 'Dll'; Diagnostic = 'Missing/empty build output' },
    @{ Name = 'missing data'; Fault = 'Data'; Diagnostic = 'Missing/empty SmallArena_Data' },
    @{ Name = 'empty data'; Fault = 'EmptyData'; Diagnostic = 'Missing/empty SmallArena_Data' },
    @{ Name = 'missing global data'; Fault = 'Globals'; Diagnostic = 'globalgamemanagers' },
    @{ Name = 'missing built scene'; Fault = 'SceneData'; Diagnostic = 'built scene data' },
    @{ Name = 'missing report'; Fault = 'Report'; Diagnostic = 'Missing/empty build output' },
    @{ Name = 'malformed report'; Fault = 'CorruptReport'; Diagnostic = 'JSON' },
    @{ Name = 'failed BuildReport'; Fault = 'Failed'; Diagnostic = 'BuildReport is invalid' },
    @{ Name = 'nonzero errors'; Fault = 'Errors'; Diagnostic = 'BuildReport is invalid' },
    @{ Name = 'wrong report scenes'; Fault = 'ReportScenes'; Diagnostic = 'BuildReport is invalid' },
    @{ Name = 'wrong Unity version'; Fault = 'Version'; Diagnostic = 'BuildReport is invalid' },
    @{ Name = 'wrong scripting backend'; Fault = 'Backend'; Diagnostic = 'BuildReport is invalid' },
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
        # Structurally valid AMD64 PE32+ fixture, not an executable program.
        $header = New-Object byte[] 512
        $header[0] = 0x4D; $header[1] = 0x5A; $header[0x3C] = 0x80
        $header[0x80] = 0x50; $header[0x81] = 0x45
        $header[0x84] = 0x64; $header[0x85] = 0x86; $header[0x94] = 0xF0
        $header[0x98] = 0x0B; $header[0x99] = 0x02
        [IO.File]::WriteAllBytes($exe, $header)
        [IO.File]::WriteAllText((Join-Path $output 'UnityPlayer.dll'), 'synthetic fixture, not a runtime DLL')
        $data = Join-Path $output 'SmallArena_Data/level0'
        [IO.File]::WriteAllText($data, 'synthetic fixture, not a real player')
        $globals = Join-Path $output 'SmallArena_Data/globalgamemanagers'
        [IO.File]::WriteAllText($globals, 'synthetic global build data')
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
            'Truncated' { [IO.File]::WriteAllBytes($exe, [byte[]]@(0x4D, 0x5A, 1)) }
            'Offset' { $header[0x3F] = 0x7F; [IO.File]::WriteAllBytes($exe, $header) }
            'PeSignature' { $header[0x80] = 0; [IO.File]::WriteAllBytes($exe, $header) }
            'Machine' { $header[0x84] = 0x4C; $header[0x85] = 1; [IO.File]::WriteAllBytes($exe, $header) }
            'Optional' { $header[0x99] = 1; [IO.File]::WriteAllBytes($exe, $header) }
            'OptionalSize' { $header[0x94] = 2; [IO.File]::WriteAllBytes($exe, $header) }
            'Dll' { Remove-Item -LiteralPath (Join-Path $output 'UnityPlayer.dll') }
            'Data' { Remove-Item -LiteralPath $data, $globals; Remove-Item -LiteralPath (Split-Path -Parent $data) }
            'EmptyData' { Remove-Item -LiteralPath $data, $globals }
            'Globals' { Remove-Item -LiteralPath $globals }
            'SceneData' { Remove-Item -LiteralPath $data }
            'Failed' { $manifest.Result = 'Failed' }
            'Errors' { $manifest.Errors = 1 }
            'ReportScenes' { $manifest.Scenes = @('Wrong') }
            'Version' { $manifest.UnityVersion = '6000.6.7f1' }
            'Backend' { $manifest.Backend = 'IL2CPP' }
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
