param(
    [Parameter(Mandatory = $true)][string]$OutputDir,
    [string]$BaselineRevision = ""
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$runRoot = [IO.Path]::GetFullPath($OutputDir)
if (-not $runRoot.StartsWith($repoRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDir must be a new directory inside the repository.'
}
if (Test-Path -LiteralPath $runRoot) { throw 'OutputDir already exists; use a new isolated output directory.' }
$null = New-Item -ItemType Directory -Path $runRoot
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$utf8 = New-Object Text.UTF8Encoding($false)
$votePath = 'src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.cs'
$noticePath = 'src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.MapNotification.cs'

function Get-Method([string]$source, [string]$signature) {
    $start = $source.IndexOf($signature, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing production method: $signature" }
    $brace = $source.IndexOf('{', $start)
    $depth = 1
    $end = $brace + 1
    while ($depth -gt 0 -and $end -lt $source.Length) {
        if ($source[$end] -eq '{') { $depth++ }
        elseif ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    if ($depth -ne 0) { throw "Unclosed production method: $signature" }
    return $source.Substring($start, $end - $start)
}

$variants = @('current')
if ($BaselineRevision) { $variants += 'baseline' }
foreach ($variant in $variants) {
    $folder = Join-Path $runRoot $variant
    $null = New-Item -ItemType Directory -Path $folder
    if ($variant -eq 'baseline') {
        $vote = (& git -C $repoRoot show "${BaselineRevision}:$votePath") -join "`n"
        if ($LASTEXITCODE -ne 0) { throw 'Cannot read requested baseline VoteDeal source.' }
        $notice = (& git -C $repoRoot show "${BaselineRevision}:$noticePath") -join "`n"
        if ($LASTEXITCODE -ne 0) { throw 'Cannot read requested baseline map notification source.' }
    } else {
        $vote = [IO.File]::ReadAllText((Join-Path $repoRoot $votePath))
        $notice = [IO.File]::ReadAllText((Join-Path $repoRoot $noticePath))
    }
    $methods = @(
        'private static bool QueueBilateralCounterpartDecision(',
        'private static void SetDecisionTriggerTime(',
        'internal static bool IsDecisionPendingInKingdom(',
        'private static void RegisterAgendaDelaySnapshot(',
        'private static bool IsPlayerInvolvedInDecision(',
        'private static bool ShouldAutoStartAgendaVote(',
        'internal static bool RequiresPlayerRulerResolution('
    )
    if ($vote.Contains('private static float GetAgendaDelayDays(')) { $methods += 'private static float GetAgendaDelayDays(' }
    $chunks = @($methods | ForEach-Object { Get-Method $vote $_ })
    $chunks += Get-Method $notice 'private void OnAgendaKingdomDecisionAdded('
    $chunks += Get-Method $notice 'internal static bool IsAgendaMapNoticeDecisionValidForExternal('
    $vm = Get-Method $vote 'public void RefreshAgendaItems()'
    $meeting = Get-Method $vote 'private void StartVoteMeeting('
    $production = "using System; using System.Collections.Generic; using System.Linq;`nnamespace AnimusForge;`npublic partial class VoteDealBehavior {`n" + ($chunks -join "`n") + "`npublic partial class KingdomAgendaVM {`n" + $vm + "`n}`npublic partial class KingdomAgendaVMMixin {`n" + $meeting + "`n}}`n"
    [IO.File]::WriteAllText((Join-Path $folder 'Production.cs'), $production, $utf8)
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs') -Destination (Join-Path $folder 'Program.cs')
    [IO.File]::WriteAllText((Join-Path $folder 'Replay.csproj'), '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><EnableDefaultCompileItems>true</EnableDefaultCompileItems><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources></PropertyGroup></Project>', $utf8)
    [IO.File]::WriteAllText((Join-Path $folder 'NuGet.Config'), '<configuration><packageSources><clear /></packageSources></configuration>', $utf8)
    [IO.File]::WriteAllText((Join-Path $folder 'sources.json'), (@{
        variant = $variant; baselineRevision = $BaselineRevision
        voteDealPath = $votePath; noticePath = $noticePath
        productionSha256 = (Get-FileHash -LiteralPath (Join-Path $folder 'Production.cs') -Algorithm SHA256).Hash
        extractedMethods = $methods + @('OnAgendaKingdomDecisionAdded', 'IsAgendaMapNoticeDecisionValidForExternal', 'KingdomAgendaVM.RefreshAgendaItems', 'KingdomAgendaVMMixin.StartVoteMeeting')
        boundary = 'Source-derived production methods; fake campaign entities, time, reflection wrapper, event dispatcher and UI leaves. No game, Harmony, actual renderer, saved game or end-to-end peace execution.'
    } | ConvertTo-Json -Depth 5), $utf8)
    $savedEnv = @{}
    foreach ($name in @('DOTNET_CLI_HOME', 'NUGET_PACKAGES', 'APPDATA', 'LOCALAPPDATA', 'TEMP', 'TMP', 'DOTNET_GENERATE_ASPNET_CERTIFICATE', 'DOTNET_NOLOGO', 'DirectoryBuildPropsPath', 'DirectoryBuildTargetsPath', 'MSBuildSDKsPath', 'FrameworkPathOverride', 'DOTNET_SKIP_FIRST_TIME_EXPERIENCE', 'DOTNET_CLI_TELEMETRY_OPTOUT')) {
        $savedEnv[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $null, 'Process')
    }
    try {
        $env:DOTNET_CLI_HOME = Join-Path $folder '.dotnet-home'
        $env:NUGET_PACKAGES = Join-Path $folder '.nuget'
        $env:APPDATA = Join-Path $folder '.appdata'
        $env:LOCALAPPDATA = Join-Path $folder '.appdata'
        $env:TEMP = Join-Path $folder '.temp'
        $env:TMP = $env:TEMP
        $null = New-Item -ItemType Directory -Path $env:APPDATA, $env:TEMP
        $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
        $env:DOTNET_NOLOGO = '1'
        $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        $build = & $dotnet build (Join-Path $folder 'Replay.csproj') -v:q "-p:RestoreConfigFile=$(Join-Path $folder 'NuGet.Config')" -p:NuGetAudit=false 2>&1
        $buildExit = $LASTEXITCODE
        [IO.File]::WriteAllText((Join-Path $folder 'build.log'), ($build -join "`n"), $utf8)
        if ($buildExit -ne 0) { throw "Harness compilation failed for ${variant}:`n$($build -join "`n")" }
        $run = & $dotnet (Join-Path $folder 'bin/Debug/net8.0/Replay.dll') 2>&1
        $runExit = $LASTEXITCODE
        $runText = $run -join "`n"
        [IO.File]::WriteAllText((Join-Path $folder 'run.log'), $runText, $utf8)
        if ($variant -eq 'current') {
            if ($runExit -ne 0) { throw "Current replay failed:`n$runText" }
            Write-Output $runText
        } else {
            if ($runExit -eq 0 -or -not $runText.Contains('FAIL vassal counterpart keeps the player kingdom 21-day window')) {
                throw "Baseline did not fail at the expected behavioral regression:`n$runText"
            }
            Write-Output 'PASS baseline source rejected: vassal counterpart still had only 3 days.'
        }
    } finally {
        foreach ($name in $savedEnv.Keys) { [Environment]::SetEnvironmentVariable($name, $savedEnv[$name], 'Process') }
    }
}
Write-Output "OUTPUT $runRoot"
