param(
    [Parameter(Mandatory=$true)][string]$Label,
    [string]$Filter = 'SmallArena.Tests.PlayMode.LobbyStartOperationTests;SmallArena.Tests.PlayMode.LobbyStartProgressTests',
    [string]$Editor = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$bt3Root = Split-Path -Parent $PSScriptRoot
$bt3Out = Join-Path $bt3Root ('docs/assignments/bt3-vt-066/evidence/' + $Label)
New-Item -ItemType Directory -Force -Path $bt3Out | Out-Null
$bt3XmlPath = Join-Path $bt3Out 'results.xml'
if (Test-Path -LiteralPath $bt3XmlPath) { throw "Preserve existing output: $bt3XmlPath" }
$bt3Args = @('-batchmode','-nographics','-projectPath',('"'+$bt3Root+'"'),'-runTests','-testPlatform','PlayMode','-testFilter',$Filter,'-testResults',('"'+$bt3XmlPath+'"'),'-logFile',('"'+(Join-Path $bt3Out 'unity.log')+'"'))
$bt3Start = [DateTime]::UtcNow.ToString('o')
$bt3SourceHashes = @(Get-ChildItem -LiteralPath (Join-Path $bt3Root 'Assets/_Project/Scripts'),(Join-Path $bt3Root 'Assets/_Project/Tests') -Filter '*.cs' -Recurse | Sort-Object FullName | ForEach-Object { [pscustomobject]@{path=$_.FullName; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })
$bt3SourceHashes | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $bt3Out 'source-hashes.json') -Encoding UTF8
git -C $bt3Root diff HEAD -- Assets/_Project/Scripts Assets/_Project/Tests | Set-Content -LiteralPath (Join-Path $bt3Out 'source.diff') -Encoding UTF8
$bt3Process = Start-Process -FilePath $Editor -ArgumentList $bt3Args -WindowStyle Hidden -Wait -PassThru
$bt3Result = 'NoXml'; $bt3Cases = @()
if (Test-Path -LiteralPath $bt3XmlPath) {
    [xml]$bt3Xml = Get-Content -LiteralPath $bt3XmlPath -Raw
    $bt3Result = $bt3Xml.'test-run'.result
    $bt3Cases = @($bt3Xml.SelectNodes('//test-case'))
}
[pscustomobject]@{ editor=$Editor; arguments=$bt3Args; startedUtc=$bt3Start; endedUtc=[DateTime]::UtcNow.ToString('o'); sourceSha=(git -C $bt3Root rev-parse HEAD); exitCode=$bt3Process.ExitCode; result=$bt3Result; caseCount=$bt3Cases.Count; cases=@($bt3Cases | ForEach-Object { [pscustomobject]@{id=$_.fullname; result=$_.result; assertion=$_.failure.message.InnerText} }) } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $bt3Out 'manifest.json') -Encoding UTF8
Write-Output "$Label : exit=$($bt3Process.ExitCode), result=$bt3Result, cases=$($bt3Cases.Count)"
if ($bt3Cases.Count -eq 0) { exit 2 }
exit $bt3Process.ExitCode
