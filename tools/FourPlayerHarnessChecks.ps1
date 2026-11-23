$ErrorActionPreference = 'Stop'
function Write-FourPlayerJson([string]$Path, [object]$Value) {
    $temp = $Path + '.tmp'
    [IO.File]::WriteAllText($temp, ($Value | ConvertTo-Json -Compress -Depth 8))
    [IO.File]::Move($temp, $Path) # No overwrite: each control sequence has a unique immutable filename.
}
function Read-FourPlayerJson([string]$Path) {
    try { $stream = [IO.File]::Open($Path, 'Open', 'Read', ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)) }
    catch [IO.FileNotFoundException] { return $null }
    catch [IO.DirectoryNotFoundException] { return $null }
    catch [IO.IOException] {
        # Mono/Windows replacement can briefly hold the destination exclusively.
        if (($_.Exception.HResult -band 65535) -in @(32, 33)) { return $null }
        throw
    }
    $reader = New-Object IO.StreamReader($stream)
    try { return ($reader.ReadToEnd() | ConvertFrom-Json) } finally { $reader.Dispose() }
}
function Assert-FourPlayerDeadline([double]$Elapsed, [double]$Limit, [bool]$Expired = $false) {
    if ([double]::IsNaN($Elapsed) -or [double]::IsInfinity($Elapsed) -or $Elapsed -lt 0 -or $Limit -le 0 -or $Expired -or $Elapsed -ge $Limit) {
        throw 'Infrastructure: four-player watchdog deadline exceeded/invalid.'
    }
}
function Assert-FourPlayerStorage([long]$AvailableBytes) {
    if ($AvailableBytes -lt 512MB) { throw 'Infrastructure: at least 512 MiB free artifact storage required.' }
}
function Get-FourPlayerFailureCategory([string]$Text) {
    if ($Text -match '\[FOUR_PLAYER_FAILURE\] Infrastructure:') { return 'Infrastructure' }
    if ($Text -match '\[FOUR_PLAYER_FAILURE\] Product:|\b[\w.]*Exception\s*:|Unhandled exception|Fatal error|Crash!!!') { return 'Product' }
    return 'Infrastructure'
}
function Read-FourPlayerStream([string]$Path, [hashtable]$Cursor) {
    try { $stream = [IO.File]::Open($Path, 'Open', 'Read', ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)) }
    catch [IO.FileNotFoundException] { return $null }
    if (-not $Cursor.ContainsKey('Offset')) { $Cursor.Offset = 0L }
    $null = $stream.Seek($Cursor.Offset, [IO.SeekOrigin]::Begin)
    $reader = New-Object IO.StreamReader($stream)
    try { $text = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $end = $text.LastIndexOf("`n")
    if ($end -lt 0) { return $null } # The writer has not completed its last JSON record yet.
    $complete = $text.Substring(0, $end + 1)
    $frame = $null
    foreach ($record in @($complete -split '\r?\n' | Where-Object { $_.Trim() })) { $frame = $record | ConvertFrom-Json }
    $Cursor.Offset += [Text.Encoding]::UTF8.GetByteCount($complete)
    return $frame
}
function Resolve-FourPlayerFrame([hashtable]$Cache, [object]$Incoming, [double]$Now) {
    if ($null -ne $Incoming -and (-not $Cache.ContainsKey('Frame') -or $Incoming.sequence -gt $Cache.Frame.sequence)) {
        $Cache.Frame = $Incoming; $Cache.SampledAt = $Now
    }
    if (-not $Cache.ContainsKey('Frame')) { return $null }
    if ($Now - $Cache.SampledAt -gt 10) { throw 'Infrastructure: worker stopped publishing snapshots for 10 seconds.' }
    return $Cache.Frame
}
function Read-FourPlayerSegments([string]$Prefix, [hashtable]$Cursor) {
    if (-not $Cursor.ContainsKey('Segment')) { $Cursor.Segment = 0 }
    $latest = $null
    for ($count = 0; $count -lt 64; $count++) {
        $sample = Read-FourPlayerStream ($Prefix + $Cursor.Segment + '.jsonl') $Cursor
        if ($null -ne $sample) { $latest = $sample }
        if (-not (Test-Path -LiteralPath ($Prefix + ($Cursor.Segment + 1) + '.jsonl'))) { return $latest }
        $Cursor.Segment++; $Cursor.Offset = 0L
    }
    throw 'Infrastructure: excessive snapshot segment backlog.'
}
function Assert-FourPlayerLog([string]$Text) {
    if ($Text -match '(?im)\b[\w.]*Exception\s*:|Unhandled exception|Fatal error|Crash!!!') { throw ('Product: runtime error: ' + $Matches[0]) }
}
function Assert-FourPlayerEvents([string]$Text, [string[]]$Required) {
    $events = @($Text -split '\r?\n' | Where-Object { $_.Trim() } | ForEach-Object { ($_ | ConvertFrom-Json).event })
    foreach ($event in $Required) { if ($events -cnotcontains $event) { throw "Product: missing worker event $event" } }
}
function Get-FourPlayerContentHash([string]$Directory) {
    $manifest = @(Get-ChildItem -LiteralPath $Directory -File -Recurse | Where-Object { $_.Name -ne 'four-player-build.json' } |
        Sort-Object FullName | ForEach-Object {
            $_.FullName.Substring($Directory.TrimEnd('\', '/').Length + 1).Replace('\', '/') + ':' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        })
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($manifest -join "`n"))))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
function Assert-FourPlayerConvergence([object[]]$Frames, [string]$Phase = 'InProgress', [double]$ClockTolerance = .75) {
    if ($Frames.Count -ne 4) { throw 'Not converged: four fresh observer snapshots required.' }
    if (@($Frames | Where-Object { $_.phase -ne $Phase -or $_.avatars -ne 4 -or @($_.scores).Count -ne 4 }).Count) { throw 'Not converged: phase/avatar/score count.' }
    if (@($Frames.session | Select-Object -Unique).Count -ne 1 -or -not $Frames[0].session) { throw 'Not converged: session IDs.' }
    if (@($Frames.localSlot | Select-Object -Unique).Count -ne 4) { throw 'Not converged: local ownership slots.' }
    foreach ($frame in $Frames) {
        if ([double]::IsNaN($frame.clock) -or [double]::IsInfinity($frame.clock) -or $frame.clock -lt 0) { throw 'Product: invalid match clock.' }
        if (@($frame.actors | Where-Object { $_.hp -lt 0 -or $_.hp -gt 10 -or $_.damage -notin @(1, 2) }).Count) { throw 'Product: invalid HP/damage metrics.' }
        if ($Phase -eq 'Countdown' -and @($frame.actors | Where-Object canAct).Count) { throw 'Product: input enabled during Countdown.' }
    }
    $clock = $Frames | Measure-Object -Property clock -Minimum -Maximum
    if ($clock.Maximum - $clock.Minimum -gt $ClockTolerance) { throw 'Not converged: observer clocks.' }
    $keys = @($Frames | ForEach-Object {
        $scoreKey = (@($_.scores | Sort-Object slot | ForEach-Object { '{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}' -f $_.slot,$_.participantId,$_.rank,$_.kills,$_.deaths,$_.damage,$_.life,$_.lifeState,$_.connected }) -join '|')
        $actorKey = (@($_.actors | Sort-Object slot | ForEach-Object { '{0}:{1}:{2}:{3}:{4}' -f $_.slot,$_.hp,$_.damage,$_.life,$_.lifeState }) -join '|')
        $scoreKey + '/' + $actorKey + '/' + $_.winner
    } | Select-Object -Unique)
    if ($keys.Count -ne 1) { throw 'Not converged: health/life/buff/score/winner replicas.' }
}
function Assert-FourPlayerFinalScores([object]$Frame, [int]$VictimSlot) {
    $attacker = @($Frame.scores | Where-Object slot -eq 0)[0]
    $victim = @($Frame.scores | Where-Object slot -eq $VictimSlot)[0]
    if ($attacker.kills -ne 5 -or $attacker.deaths -ne 0 -or $attacker.damage -ne 50 -or $victim.deaths -ne 5 -or $victim.kills -ne 0 -or $Frame.winner -ne $attacker.participantId) {
        throw 'Product: expected host=5 kills/50 actual damage, victim=5 deaths, host winner.'
    }
    foreach ($row in $Frame.scores | Where-Object { $_.slot -ne 0 -and $_.slot -ne $VictimSlot }) {
        if ($row.kills -ne 0 -or $row.deaths -ne 0 -or $row.damage -ne 0) { throw 'Product: idle participant combat attribution changed.' }
    }
    # Independent oracle: do not call the production ranking function to calculate expected values.
    $ranked = @($Frame.scores | Sort-Object @{Expression='kills';Descending=$true},deaths,@{Expression='damage';Descending=$true},slot)
    for ($i = 0; $i -lt 4; $i++) { if ($ranked[$i].rank -ne $i + 1) { throw 'Product: deterministic ranking mismatch.' } }
    foreach ($actor in $Frame.actors) {
        $expected = if ($actor.slot -eq 0) { 'Victory' } else { 'Defeat' }
        if ($actor.result -ne $expected -or $actor.canAct) { throw 'Product: result/input freeze mismatch.' }
    }
}
