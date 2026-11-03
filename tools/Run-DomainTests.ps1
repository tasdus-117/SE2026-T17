param(
    [string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor',
    [switch]$MutationChecks
)

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskRoot
$domainFolder = Join-Path $taskRoot 'Assets/_Project/Scripts/Shared/Domain'
$testFolder = Join-Path $taskRoot 'Assets/_Project/Tests/EditMode'
$output = Join-Path $taskRoot 'TestResults/Domain'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$dotnet = Join-Path $EditorRoot 'Data/NetCoreRuntime/dotnet.exe'
$csc = Join-Path $EditorRoot 'Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$referencePath = Join-Path $EditorRoot 'Data/DotNetSdk/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0'
$framework = @(Get-ChildItem -LiteralPath $referencePath -Filter '*.dll' | ForEach-Object { $_.FullName })
$nunit = Join-Path $EditorRoot 'Data/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Compile-Source([string]$Binary, [string[]]$Sources, [string[]]$References, [string]$Target = 'library') {
    $arguments = @($csc, '/nologo', '/noconfig', '/nostdlib+', '/langversion:9.0', '/warnaserror+', ('/target:' + $Target), ('/out:' + $Binary))
    $arguments += @($References | ForEach-Object { '/reference:' + $_ })
    $arguments += $Sources
    $diagnostics = @(& $dotnet @arguments 2>&1)
    if ($LASTEXITCODE -ne 0) { throw ("Compiler failed for $Binary`n" + ($diagnostics -join "`n")) }
}

function Compile-Suite([string]$SourceFolder, [string]$Destination) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    $domainBinary = Join-Path $Destination 'SmallArena.Shared.Domain.dll'
    # No engine, plugin or NUnit reference is passed to the domain compiler.
    $domainSources = @(Get-ChildItem -LiteralPath $SourceFolder -Filter '*.cs' -Recurse | ForEach-Object { $_.FullName })
    Compile-Source $domainBinary $domainSources $framework
    $testSources = @((Join-Path $testFolder 'AssemblyInfo.cs'), (Join-Path $testFolder 'MatchStateTests.cs'))
    Compile-Source (Join-Path $Destination 'SmallArena.EditMode.Tests.dll') $testSources (@($framework) + @($domainBinary, $nunit))
}

Copy-Item -LiteralPath $nunit -Destination (Join-Path $output 'nunit.framework.dll') -Force
$hostBinary = Join-Path $output 'NUnitDomainRunner.dll'
Compile-Source $hostBinary @((Join-Path $PSScriptRoot 'NUnitDomainRunner.cs')) (@($framework) + @($nunit)) 'exe'
$runtime = '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}'
[IO.File]::WriteAllText((Join-Path $output 'NUnitDomainRunner.runtimeconfig.json'), $runtime, $utf8)

Compile-Suite $domainFolder $output
$baselineXml = Join-Path $output 'results.xml'
if (Test-Path -LiteralPath $baselineXml) { Remove-Item -LiteralPath $baselineXml -Force }
$baselineLog = @(& $dotnet $hostBinary (Join-Path $output 'SmallArena.EditMode.Tests.dll') $baselineXml 2>&1)
$baselineExit = $LASTEXITCODE
$baselineLog | Set-Content -LiteralPath (Join-Path $output 'results.log')
$baselineLog | Write-Output
if ($baselineExit -ne 0) { throw 'Baseline NUnit run failed; see TestResults/Domain/results.log.' }
[xml]$baselineResult = Get-Content -LiteralPath $baselineXml -Raw
$caseCount = @($baselineResult.SelectNodes('//test-case')).Count
if ($caseCount -eq 0) { throw 'NUnit discovered no cases.' }

# Verify the real compiled domain has no forbidden assembly references.
$domainAssembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $output 'SmallArena.Shared.Domain.dll'))
$domainReferences = @($domainAssembly.GetReferencedAssemblies() | ForEach-Object { $_.Name })
if (@($domainReferences | Where-Object { $_ -match 'Unity|NUnit|SmallArena\.(Client|Server|Networking)' }).Count -ne 0) { throw 'Compiled domain has a forbidden reference.' }
Write-Output ('Domain references: ' + ($domainReferences -join ', '))

if (-not $MutationChecks) { exit 0 }

# Each mutation is compiled and tested in an isolated copy. Production sources are never edited.
$mutations = @(
    @{ Id = 'M01'; File = 'MatchStateMachine.cs'; Find = '|| !readiness.IsReady'; Replace = '|| readiness.IsReady'; Description = 'Invert readiness guard' },
    @{ Id = 'M02'; File = 'MatchStateMachine.cs'; Find = 'CurrentState != MatchState.Initializing'; Replace = 'CurrentState == MatchState.Initializing'; Description = 'Invert start-state guard' },
    @{ Id = 'M03'; File = 'MatchStateMachine.cs'; Find = 'CurrentState != MatchState.Playing'; Replace = 'CurrentState == MatchState.Playing'; Description = 'Invert victory-state guard' },
    @{ Id = 'M04'; File = 'MatchStateMachine.cs'; Find = 'TransitionTo(MatchState.Victory);'; Replace = 'TransitionTo(MatchState.Playing);'; Description = 'Victory remains Playing' },
    @{ Id = 'M05'; File = 'MatchStateMachine.cs'; Find = 'OnStateChanged?.Invoke(nextState);'; Replace = '_ = OnStateChanged;'; Description = 'Suppress state-change notification' },
    @{ Id = 'M06'; File = 'MatchStateMachine.cs'; Find = 'if (isNotifying || CurrentState != MatchState.Playing)'; Replace = 'if (CurrentState != MatchState.Playing)'; Description = 'Allow victory during Playing notification' },
    @{ Id = 'M07'; File = 'MatchStateMachine.cs'; Find = "CurrentState = nextState;`n            isNotifying = true;"; Replace = 'isNotifying = true;'; FindAfter = 'OnStateChanged?.Invoke(nextState);'; ReplaceAfter = "OnStateChanged?.Invoke(nextState);`n                CurrentState = nextState;"; Description = 'Notify before committing state' },
    @{ Id = 'M08'; File = 'MatchStateMachine.cs'; Find = 'isNotifying = false;'; Replace = 'isNotifying = true;'; Description = 'Never release notification guard' },
    @{ Id = 'M09'; File = 'MatchStateMachine.cs'; Find = 'OnStateChanged?.Invoke(nextState);'; Replace = "OnStateChanged?.Invoke(nextState);`n                OnStateChanged?.Invoke(nextState);"; Description = 'Duplicate transition notification' },
    @{ Id = 'M10'; File = 'MatchStateMachine.cs'; Find = 'CurrentState == MatchState.Playing'; Replace = 'CurrentState != MatchState.Playing'; Description = 'Invert IsPlaying query' },
    @{ Id = 'M11'; File = 'MatchStateMachine.cs'; Find = 'get; private set;'; Replace = 'get; set;'; Description = 'Expose public state setter' },
    @{ Id = 'M12'; File = 'MatchReadinessResult.cs'; Find = 'diagnosticMessage ?? string.Empty'; Replace = 'diagnosticMessage'; Description = 'Remove default/null diagnostic normalization' },
    @{ Id = 'M13'; File = 'MatchStateMachine.cs'; Find = "finally`n            {`n                // Subscriber exceptions propagate; the committed state is retained.`n                isNotifying = false;`n            }"; Replace = "catch`n            {`n                throw;`n            }`n            isNotifying = false;"; Description = 'Release notification guard only on success' }
)
$summary = @()
foreach ($mutation in $mutations) {
    $mutationFolder = Join-Path $output ('Mutations/' + $mutation.Id)
    $sourceFolder = Join-Path $mutationFolder 'Sources'
    New-Item -ItemType Directory -Path $sourceFolder -Force | Out-Null
    Get-ChildItem -LiteralPath $domainFolder -Filter '*.cs' | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $sourceFolder -Force }
    $sourcePath = Join-Path $sourceFolder $mutation.File
    $source = [IO.File]::ReadAllText($sourcePath).Replace("`r`n", "`n")
    if ([regex]::Matches($source, [regex]::Escape($mutation.Find)).Count -ne 1) { throw "Mutation anchor is not unique: $($mutation.Id)" }
    $source = $source.Replace($mutation.Find, $mutation.Replace)
    if ($mutation.ContainsKey('FindAfter')) {
        if ([regex]::Matches($source, [regex]::Escape($mutation.FindAfter)).Count -ne 1) { throw "Second mutation anchor is not unique: $($mutation.Id)" }
        $source = $source.Replace($mutation.FindAfter, $mutation.ReplaceAfter)
    }
    [IO.File]::WriteAllText($sourcePath, $source, $utf8)
    Compile-Suite $sourceFolder $mutationFolder
    foreach ($hostFile in @('NUnitDomainRunner.dll', 'NUnitDomainRunner.runtimeconfig.json', 'nunit.framework.dll')) {
        Copy-Item -LiteralPath (Join-Path $output $hostFile) -Destination $mutationFolder -Force
    }
    $xmlPath = Join-Path $mutationFolder 'results.xml'
    if (Test-Path -LiteralPath $xmlPath) { Remove-Item -LiteralPath $xmlPath -Force }
    $mutationHost = Join-Path $mutationFolder 'NUnitDomainRunner.dll'
    $runLog = @(& $dotnet $mutationHost (Join-Path $mutationFolder 'SmallArena.EditMode.Tests.dll') $xmlPath 2>&1)
    $runExit = $LASTEXITCODE
    $runLog | Set-Content -LiteralPath (Join-Path $mutationFolder 'results.log')
    [xml]$result = Get-Content -LiteralPath $xmlPath -Raw
    $failed = @($result.SelectNodes("//test-case[@result='Failed']"))
    if ($runExit -eq 0 -or $failed.Count -eq 0 -or @($result.SelectNodes('//test-case')).Count -ne $caseCount) { throw "Mutation survived or run was invalid: $($mutation.Id)" }
    $summary += [pscustomobject]@{ Id = $mutation.Id; Description = $mutation.Description; FailedTests = $failed.Count; DetectedBy = @($failed | ForEach-Object { $_.GetAttribute('name') }) }
    Write-Output "KILLED $($mutation.Id): $($mutation.Description) ($($failed.Count) failing tests)"
}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'mutation-results.json')
Write-Output "PASS: $caseCount NUnit cases; $($summary.Count)/$($mutations.Count) targeted mutations killed. No production source was mutated."
