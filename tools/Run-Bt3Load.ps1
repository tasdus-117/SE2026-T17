param([int]$Rounds = 20, [string]$Label = 'load', [string]$EditorRoot = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor')
$ErrorActionPreference = 'Stop'
$bt3Root = Split-Path -Parent $PSScriptRoot
$bt3Out = Join-Path $bt3Root ('docs/assignments/bt3-vt-066/evidence/' + $Label)
New-Item -ItemType Directory -Force -Path $bt3Out | Out-Null
$bt3Dotnet = Join-Path $EditorRoot 'Data/NetCoreRuntime/dotnet.exe'
$bt3Csc = Join-Path $EditorRoot 'Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$bt3Refs = Join-Path $EditorRoot 'Data/DotNetSdk/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0'
$bt3Binary = Join-Path $bt3Root 'TestResults/BT3/Bt3Load.dll'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $bt3Binary) | Out-Null
$bt3Sources = @((Join-Path $PSScriptRoot 'Bt3LoadHarness.cs'),(Join-Path $bt3Root 'Assets/_Project/Scripts/Server/Sessions/LobbyRoom.cs'),(Join-Path $bt3Root 'Assets/_Project/Scripts/Server/Sessions/LobbyStartOperation.cs'),(Join-Path $bt3Root 'Assets/_Project/Scripts/Networking/LobbyMessages.cs'))
$bt3Args = @($bt3Csc,'/nologo','/noconfig','/nostdlib+','/langversion:latest','/target:exe',('/out:' + $bt3Binary)) + @(Get-ChildItem -LiteralPath $bt3Refs -Filter '*.dll' | ForEach-Object { '/reference:' + $_.FullName }) + $bt3Sources
& $bt3Dotnet @bt3Args 2>&1 | Tee-Object -FilePath (Join-Path $bt3Out 'compile.log')
if ($LASTEXITCODE -ne 0) { throw 'Load harness compile failed.' }
[IO.File]::WriteAllText([IO.Path]::ChangeExtension($bt3Binary, '.runtimeconfig.json'), '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}')
& $bt3Dotnet $bt3Binary $bt3Out $Rounds 2>&1 | Tee-Object -FilePath (Join-Path $bt3Out 'run.log')
$bt3Exit = $LASTEXITCODE
[pscustomobject]@{sourceSha=(git -C $bt3Root rev-parse HEAD); sourceFiles=@($bt3Sources | ForEach-Object { [pscustomobject]@{path=$_; sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash} }); rounds=$Rounds; exitCode=$bt3Exit; command='powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Bt3Load.ps1 -Rounds '+$Rounds+' -Label '+$Label} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $bt3Out 'manifest.json') -Encoding UTF8
exit $bt3Exit
