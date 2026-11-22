$ErrorActionPreference = 'Stop'
function Read-SmokeLog([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $reader = New-Object IO.StreamReader($stream)
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
function Assert-SmokeNoErrors([string]$Text) {
    if ($Text -match '(?im)\b[\w.]*Exception\s*:|Unhandled exception|Fatal error|Crash!!!|^\[SMOKE_EVENT\] FAILURE\b') {
        throw ('Smoke runtime error: ' + $Matches[0])
    }
}
function Test-SmokeMarker([string]$Text, [string]$Name) {
    return [regex]::IsMatch($Text, ('(?m)^\[SMOKE_EVENT\] ' + [regex]::Escape($Name) + '(?: |\r?$)'))
}
function Assert-SmokeMarkers([string]$Text, [string[]]$Required) {
    Assert-SmokeNoErrors $Text
    foreach ($name in $Required) { if (-not (Test-SmokeMarker $Text $name)) { throw "Missing smoke marker: $name" } }
}
function Assert-SmokeOrder([string]$Text, [string[]]$Names) {
    $previous = -1
    foreach ($name in $Names) {
        $match = [regex]::Match($Text, ('(?m)^\[SMOKE_EVENT\] ' + [regex]::Escape($name) + '(?: |\r?$)'))
        if (-not $match.Success -or $match.Index -le $previous) { throw "Invalid smoke transition order at $name" }
        $previous = $match.Index
    }
}
function Assert-SmokeExit([int]$Code, [bool]$ForcedClient = $false) {
    if (-not $ForcedClient -and $Code -ne 0) { throw "Smoke worker failed: exit=$Code" }
    if ($ForcedClient -and $Code -eq 0) { throw 'Abrupt disconnect client unexpectedly exited normally.' }
}
function Assert-SmokeDeadline([double]$Elapsed, [int]$TimeoutSeconds, [bool]$WatchdogExpired = $false) {
    if ($WatchdogExpired -or $Elapsed -ge $TimeoutSeconds) { throw "Smoke watchdog timeout after $TimeoutSeconds seconds." }
}
