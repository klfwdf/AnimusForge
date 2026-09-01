#requires -Version 5.1
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$sharedTargets = '..\ReplayDependencies\BannerlordReplayDependencies.targets'
$consumerNames = @(
    'PolicyGatewayReplayTests',
    'WorldDiplomacyGatewayReplayTests',
    'TtsGatewayReplayTests',
    'ProductionOptInEntryReplayTests',
    'ShoutNetworkSseReplayTests'
)

function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$passed = 0
foreach ($name in $consumerNames) {
    $projectPath = Join-Path $repositoryRoot "tools\$name\$name.csproj"
    Assert (Test-Path -LiteralPath $projectPath -PathType Leaf) "REPLAY_PROJECT_MISSING: $projectPath"

    $text = [IO.File]::ReadAllText($projectPath)
    [xml]$project = $text
    $frameworks = @($project.SelectNodes('/Project/PropertyGroup/TargetFramework') | ForEach-Object { $_.InnerText })
    Assert ($frameworks -contains 'net8.0') "REPLAY_PROJECT_FRAMEWORK: $name must remain net8.0"

    $imports = @($project.SelectNodes('/Project/Import') | Where-Object {
        [string]$_.GetAttribute('Project') -eq $sharedTargets
    })
    Assert ($imports.Count -eq 1) "REPLAY_PROJECT_SHARED_TARGETS: $name must import $sharedTargets exactly once"

    $localCopyTargets = @($project.SelectNodes('/Project/Target') | Where-Object {
        [string]$_.GetAttribute('Name') -eq 'CopyBannerlordRuntimeForReplay'
    })
    Assert ($localCopyTargets.Count -eq 0) "REPLAY_PROJECT_LOCAL_COPY_TARGET: $name must not define its own dependency copy target"
    Assert ($text -notmatch '(?im)[A-Z]:[\\/]') "REPLAY_PROJECT_ABSOLUTE_PATH: $name contains a machine-specific absolute path"
    Assert ($text -notmatch '(?im)(Modules|Workshop)[\\/][^\r\n<\"]*\*\*') "REPLAY_PROJECT_RECURSIVE_SCAN: $name scans a Modules/Workshop tree"
    Assert ($text -notmatch '(?im)AnimusForge\.dll') "REPLAY_PROJECT_IMPLEMENTATION_COPY: $name must not copy an AnimusForge implementation"

    $passed++
    Write-Output "PASS replayProjectBoundary consumer=$name"
}

Write-Output "PASS replayProjectBoundary consumers=$passed sharedTargets=$sharedTargets"
