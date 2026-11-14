param([string]$Editor = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$bt3Root = Split-Path -Parent $PSScriptRoot
$bt3Out = Join-Path $bt3Root 'docs/assignments/bt3-vt-066/evidence/baseline'
New-Item -ItemType Directory -Force -Path $bt3Out | Out-Null
$bt3Suites = @('SmallArena.Tests.PlayMode.LobbyRoomManagementTests','SmallArena.Tests.PlayMode.LobbyGovernanceAndScreenTests')
$bt3Runs = @()
function Invoke-Bt3Suite([string]$Label, [string]$Filter) {
    $bt3XmlPath = Join-Path $bt3Out ($Label + '.xml')
    $bt3LogPath = Join-Path $bt3Out ($Label + '.log')
    if (Test-Path -LiteralPath $bt3XmlPath) { throw "Output already exists: $bt3XmlPath" }
    $bt3Args = @('-batchmode','-nographics','-projectPath',('"' + $bt3Root + '"'),'-runTests','-testPlatform','PlayMode','-testFilter',$Filter,'-testResults',('"' + $bt3XmlPath + '"'),'-logFile',('"' + $bt3LogPath + '"'))
    $bt3Start = [DateTime]::UtcNow
    $bt3Process = Start-Process -FilePath $Editor -ArgumentList $bt3Args -WindowStyle Hidden -PassThru -Wait
    $bt3Discovered = @()
    $bt3Cases = @()
    $bt3Result = 'NoXml'
    if (Test-Path -LiteralPath $bt3XmlPath) {
        [xml]$bt3Xml = Get-Content -LiteralPath $bt3XmlPath -Raw
        $bt3Cases = @($bt3Xml.SelectNodes('//test-case'))
        $bt3Discovered = @($bt3Xml.SelectNodes('//test-suite') | ForEach-Object { $_.GetAttribute('fullname') } | Where-Object { $bt3Suites -contains $_ } | Sort-Object -Unique)
        $bt3Result = $bt3Xml.'test-run'.result
    }
    $bt3Run = [pscustomobject]@{ label=$Label; editor=$Editor; arguments=$bt3Args; startedUtc=$bt3Start.ToString('o'); endedUtc=[DateTime]::UtcNow.ToString('o'); sourceSha=(git -C $bt3Root rev-parse HEAD); exitCode=$bt3Process.ExitCode; xml=$bt3XmlPath; log=$bt3LogPath; result=$bt3Result; cases=$bt3Cases.Count; suites=$bt3Discovered; failures=@($bt3Cases | Where-Object { $_.result -eq 'Failed' } | ForEach-Object { [pscustomobject]@{id=$_.fullname; assertion=$_.failure.message.InnerText} }) }
    $bt3Run | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $bt3Out ($Label + '-manifest.json')) -Encoding UTF8
    return $bt3Run
}
$bt3Combined = Invoke-Bt3Suite 'combined' ($bt3Suites -join ';')
$bt3Runs += $bt3Combined
if ($bt3Combined.suites.Count -ne 2 -or $bt3Combined.cases -eq 0) {
    foreach ($bt3Suite in $bt3Suites) { $bt3Runs += Invoke-Bt3Suite ($bt3Suite.Split('.')[-1]) $bt3Suite }
}
$bt3Runs | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $bt3Out 'runs.json') -Encoding UTF8
$bt3Runs | Select-Object label,exitCode,result,cases,@{N='suites';E={$_.suites -join ','}} | Format-Table -AutoSize
if (@($bt3Runs | Where-Object { $_.cases -gt 0 }).Count -eq 0) { exit 2 }
if (@($bt3Runs | Where-Object { $_.exitCode -ne 0 -or $_.result -ne 'Passed' }).Count -gt 0) { exit 1 }
