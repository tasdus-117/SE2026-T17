param([string]$Label = 'mutation-final')
$ErrorActionPreference = 'Stop'
$bt3Root = Split-Path -Parent $PSScriptRoot
$bt3Target = 'Assets/_Project/Scripts/Networking/ArenaNetworkManager.cs'
$bt3Path = Join-Path $bt3Root $bt3Target
$bt3Out = Join-Path $bt3Root ('docs/assignments/bt3-vt-066/evidence/' + $Label)
New-Item -ItemType Directory -Force -Path $bt3Out | Out-Null
$bt3OriginalBranch = git -C $bt3Root branch --show-current
$bt3OriginalSha = git -C $bt3Root rev-parse HEAD
if ((git -C $bt3Root status --porcelain -- $bt3Target)) { throw 'Commit/review the target first. Mutation refuses a dirty target.' }
$bt3Original = [IO.File]::ReadAllText($bt3Path)
$bt3Anchor = 'Count(connection => connection.isAuthenticated) > LobbyLimits.Capacity'
if ([regex]::Matches($bt3Original, [regex]::Escape($bt3Anchor)).Count -ne 1) { throw 'Mutation anchor must be unique.' }
$bt3Filter = 'SmallArena.Tests.PlayMode.LobbyStartBoundaryTests.T13_AuthenticatedCount_RealConnectCallbackBoundary'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Run-Bt3Tests.ps1') -Label ($Label + '/clean') -Filter $bt3Filter
if ($LASTEXITCODE -ne 0) { throw 'Clean test must pass before mutation.' }
$bt3TempBranch = 'codex/bt3-vt-066-mutant-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
git -C $bt3Root switch -c $bt3TempBranch
if ($LASTEXITCODE -ne 0) { throw 'Cannot create mutation branch.' }
[IO.File]::WriteAllText($bt3Path, $bt3Original.Replace($bt3Anchor, $bt3Anchor.Replace(' > ', ' >= ')), (New-Object Text.UTF8Encoding($false)))
git -C $bt3Root add -- $bt3Target
git -C $bt3Root commit -m 'test(vt-066): temporary capacity boundary mutant > to >='
if ($LASTEXITCODE -ne 0) { throw 'Mutation commit failed; inspect temporary branch before recovery.' }
$bt3MutantSha = git -C $bt3Root rev-parse HEAD
git -C $bt3Root diff ($bt3MutantSha + '^') $bt3MutantSha -- $bt3Target | Set-Content -LiteralPath (Join-Path $bt3Out 'mutant.diff') -Encoding UTF8
try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Run-Bt3Tests.ps1') -Label ($Label + '/mutant') -Filter $bt3Filter
    $bt3MutantExit = $LASTEXITCODE
} finally {
    git -C $bt3Root switch $bt3OriginalBranch
    if ($LASTEXITCODE -ne 0) { throw 'Cannot restore submission branch; inspect before continuing.' }
}
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Run-Bt3Tests.ps1') -Label ($Label + '/restored') -Filter $bt3Filter
$bt3RestoredExit = $LASTEXITCODE
[xml]$bt3MutantXml = Get-Content -LiteralPath (Join-Path $bt3Out 'mutant/results.xml') -Raw
$bt3Failed = @($bt3MutantXml.SelectNodes('//test-case') | Where-Object { $_.result -eq 'Failed' })
$bt3KilledAtFour = @($bt3Failed | Where-Object { $_.fullname -like '*(4)' }).Count -gt 0
$bt3Restored = [IO.File]::ReadAllText($bt3Path) -eq $bt3Original
[pscustomobject]@{sourceSha=$bt3OriginalSha; mutantSha=$bt3MutantSha; temporaryBranch=$bt3TempBranch; submissionBranch=$bt3OriginalBranch; filter=$bt3Filter; mutantExit=$bt3MutantExit; restoredExit=$bt3RestoredExit; killedAtFour=$bt3KilledAtFour; targetRestored=$bt3Restored; failures=@($bt3Failed | ForEach-Object { [pscustomobject]@{id=$_.fullname; assertion=$_.failure.message.InnerText} })} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $bt3Out 'summary.json') -Encoding UTF8
if ($bt3MutantExit -eq 0 -or -not $bt3KilledAtFour -or -not $bt3Restored -or $bt3RestoredExit -ne 0) { throw 'Mutation experiment invalid, survived, or restoration failed.' }
Write-Output 'KILLED at authenticated count 4; submission target restored.'
