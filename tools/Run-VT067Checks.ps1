param(
    [ValidateSet('Green', 'Red', 'Mutation')][string]$Mode = 'Green',
    [string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $taskRoot 'Assets/_Project/Scripts/Shared/Domain'
$output = Join-Path $taskRoot ('TestResults/VT067/Reproduce-' + $Mode)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$runner = Join-Path $PSScriptRoot 'Run-DomainTests.ps1'
if ($Mode -eq 'Green') {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $runner -EditorRoot $EditorRoot -OutputDirectory $output
    if ($LASTEXITCODE -ne 0) { throw 'Green Domain regression failed.' }
    exit 0
}
$copy = Join-Path $output 'Sources'
$resolved = [IO.Path]::GetFullPath($copy)
if (-not $resolved.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Experiment must stay inside the workspace.' }
New-Item -ItemType Directory -Path $copy -Force | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $source -Filter '*.cs' -Recurse) {
    $destination = Join-Path $copy $file.FullName.Substring($source.Length + 1)
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
}
$originalPath = Join-Path $source 'AutoSaveDraftService.cs'
$experimentPath = Join-Path $copy 'AutoSaveDraftService.cs'
$original = [IO.File]::ReadAllBytes($originalPath)
$before = (Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
$text = [Text.Encoding]::UTF8.GetString($original).Replace("`r`n", "`n")
$guard = "                if (transaction.Current != null && snapshot.Revision <= transaction.Current.Revision)`n                    return DraftSaveResult.RejectedStale;`n"
if ([regex]::Matches($text, [regex]::Escape($guard)).Count -ne 1) { throw 'Expected exactly one stale guard.' }
$text = $text.Replace($guard, '')
if ($Mode -eq 'Red') { $text = $text.Replace('snapshot == null || !snapshot.IsValid()', 'snapshot == null') }
[IO.File]::WriteAllText($experimentPath, $text, (New-Object Text.UTF8Encoding($false)))
try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $runner -EditorRoot $EditorRoot -OutputDirectory $output -SourceDirectory $copy -TestPrefix SmallArena.Tests.EditMode.VT067
    $code = $LASTEXITCODE
    [xml]$result = Get-Content -LiteralPath (Join-Path $output 'results.xml') -Raw
    $failures = @($result.SelectNodes("//test-case[@result='Failed']") | ForEach-Object { $_.GetAttribute('name') })
    if ($code -eq 0 -or $failures.Count -eq 0) { throw 'Expected requirement failures were not detected.' }
    foreach ($required in @('TC03_', 'TC04_', 'TC06_')) {
        if (-not @($failures | Where-Object { $_.StartsWith($required) }).Count) { throw "Missing deterministic detector $required" }
    }
    $failures | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'detected-by.json')
    Write-Output "EXPECTED $Mode failure: $($failures.Count) cases detected the missing guards."
} finally {
    [IO.File]::WriteAllBytes($experimentPath, $original)
    if ((Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash -ne $before -or
        (Get-FileHash -LiteralPath $experimentPath -Algorithm SHA256).Hash -ne $before) { throw 'Source restoration failed.' }
    [IO.File]::WriteAllText((Join-Path $output 'restored.txt'), "Production untouched; experiment restored. SHA256=$before")
}
