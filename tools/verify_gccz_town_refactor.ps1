[CmdletBinding()]
param(
    [string]$StandaloneRoot,
    [string]$FusedRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-NormalizedTextHash {
    param([string]$Path)

    $text = [System.IO.File]::ReadAllText($Path)
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($normalized)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha256.ComputeHash($bytes))).Replace("-", "")
    }
    finally {
        $sha256.Dispose()
    }
}

function Assert-NormalizedMirror {
    param(
        [string]$StandalonePath,
        [string]$FusedPath,
        [string]$Label
    )

    Assert-Condition (Test-Path -LiteralPath $StandalonePath -PathType Leaf) "Missing standalone $Label file: $StandalonePath"
    Assert-Condition (Test-Path -LiteralPath $FusedPath -PathType Leaf) "Missing fused $Label file: $FusedPath"
    $standaloneHash = Get-NormalizedTextHash $StandalonePath
    $fusedHash = Get-NormalizedTextHash $FusedPath
    Assert-Condition ($standaloneHash -eq $fusedHash) "Mirror mismatch for ${Label}: $StandalonePath <> $FusedPath"
}

$scriptRepositoryRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Split-Path -Parent $scriptRepositoryRoot
if ([string]::IsNullOrWhiteSpace($StandaloneRoot)) {
    $StandaloneRoot = Join-Path $workspaceRoot "GCCZ"
}
if ([string]::IsNullOrWhiteSpace($FusedRoot)) {
    $FusedRoot = Join-Path $workspaceRoot "NEW-10"
}

$StandaloneRoot = [System.IO.Path]::GetFullPath($StandaloneRoot)
$FusedRoot = [System.IO.Path]::GetFullPath($FusedRoot)
$standaloneCore = Join-Path $StandaloneRoot "src\AnimusForge.SiegeAftermathIntervention"
$fusedCore = Join-Path $FusedRoot "AnimusForge.SiegeAftermathIntervention"

Assert-Condition (Test-Path -LiteralPath $standaloneCore -PathType Container) "Standalone GCCZ core not found: $standaloneCore"
Assert-Condition (Test-Path -LiteralPath $fusedCore -PathType Container) "Fused GCCZ core not found: $fusedCore"

$standaloneCoreFiles = @(Get-ChildItem -LiteralPath $standaloneCore -File -Filter "*.cs" | Sort-Object Name)
$fusedCoreFiles = @(Get-ChildItem -LiteralPath $fusedCore -File -Filter "*.cs" | Sort-Object Name)
$standaloneNames = @($standaloneCoreFiles | ForEach-Object { $_.Name })
$fusedNames = @($fusedCoreFiles | ForEach-Object { $_.Name })
$missingFromFused = @($standaloneNames | Where-Object { $_ -notin $fusedNames })
$extraInFused = @($fusedNames | Where-Object { $_ -notin $standaloneNames })
Assert-Condition ($missingFromFused.Count -eq 0) ("Fused core is missing: " + ($missingFromFused -join ", "))
Assert-Condition ($extraInFused.Count -eq 0) ("Fused core has extra files: " + ($extraInFused -join ", "))

foreach ($file in $standaloneCoreFiles) {
    Assert-NormalizedMirror $file.FullName (Join-Path $fusedCore $file.Name) "core source $($file.Name)"
}

$resourceMappings = @(
    @("ModuleData\GcczTownActionPresentation.zh-CN.json", "AnimusForge\ModuleData\GcczTownActionPresentation.zh-CN.json"),
    @("ModuleData\GcczTownEntryPresentation.zh-CN.json", "AnimusForge\ModuleData\GcczTownEntryPresentation.zh-CN.json"),
    @("ModuleData\GcczTownHiddenResidents.zh-CN.json", "AnimusForge\ModuleData\GcczTownHiddenResidents.zh-CN.json"),
    @("ModuleData\GcczTownManual.zh-CN.json", "AnimusForge\ModuleData\GcczTownManual.zh-CN.json"),
    @("ModuleData\GcczTownPrompt.zh-CN.json", "AnimusForge\ModuleData\GcczTownPrompt.zh-CN.json"),
    @("ModuleData\Languages\CNs\gccz_town_manual_strings.xml", "AnimusForge\ModuleData\Languages\CNs\gccz_town_manual_strings.xml")
)
foreach ($mapping in $resourceMappings) {
    Assert-NormalizedMirror (Join-Path $StandaloneRoot $mapping[0]) (Join-Path $FusedRoot $mapping[1]) "player resource $($mapping[0])"
}

$documentMappings = @(
    @("docs\plans\gccz-town-refactor-feature-list-20260821.md", "docs\gccz\plans\gccz-town-refactor-feature-list-20260821.md"),
    @("docs\bridge\af-bridge-surface.md", "docs\gccz\bridge\af-bridge-surface.md"),
    @("docs\audits\gccz-town-runtime-inventory.md", "docs\gccz\audits\gccz-town-runtime-inventory.md")
)
foreach ($mapping in $documentMappings) {
    Assert-NormalizedMirror (Join-Path $StandaloneRoot $mapping[0]) (Join-Path $FusedRoot $mapping[1]) "handoff document $($mapping[0])"
}

$coreNameSet = @{}
foreach ($name in $standaloneNames) {
    $coreNameSet[$name] = $true
}
$duplicateTopLevelCoreFiles = @(Get-ChildItem -LiteralPath $FusedRoot -File -Filter "*.cs" | Where-Object { $coreNameSet.ContainsKey($_.Name) })
Assert-Condition ($duplicateTopLevelCoreFiles.Count -eq 0) ("Core source duplicated in AF root: " + (($duplicateTopLevelCoreFiles | ForEach-Object { $_.Name }) -join ", "))

$topLevelNamespaceLeaks = @()
foreach ($file in Get-ChildItem -LiteralPath $FusedRoot -File -Filter "*.cs") {
    if ([System.IO.File]::ReadAllText($file.FullName).Contains("namespace AnimusForge.SiegeAftermathIntervention")) {
        $topLevelNamespaceLeaks += $file.Name
    }
}
Assert-Condition ($topLevelNamespaceLeaks.Count -eq 0) ("Standalone namespace declared in AF root: " + ($topLevelNamespaceLeaks -join ", "))

$activeRuntimeFiles = @(
    (Join-Path $FusedRoot "AfGcczShoutBridge.cs"),
    (Join-Path $FusedRoot "SiegeAiInterventionBehavior.cs")
)
$activeRuntimeText = ($activeRuntimeFiles | ForEach-Object {
    Assert-Condition (Test-Path -LiteralPath $_ -PathType Leaf) "Missing active GCCZ runtime file: $_"
    [System.IO.File]::ReadAllText($_)
}) -join "`n"

$keywordTriggerPatterns = @(
    'playerText\s*\.\s*(Contains|IndexOf|StartsWith|EndsWith)\s*\(',
    '(Contains|IndexOf|StartsWith|EndsWith)\s*\(\s*playerText',
    'Regex\.(IsMatch|Match|Matches)\s*\(\s*playerText'
)
foreach ($pattern in $keywordTriggerPatterns) {
    Assert-Condition (-not [System.Text.RegularExpressions.Regex]::IsMatch($activeRuntimeText, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) "Active GCCZ runtime contains a dialogue keyword trigger matching: $pattern"
}

$requiredLifecycleEvidence = @(
    'IsActiveInCurrentMission()',
    'EndInterventionSceneScope("mission_ended")',
    'InterventionSceneMemory.EndScene()',
    'ClearInterventionSceneTransientState()',
    'ResetNpcResponseBudgetForExternal("town_scene_transient_clear")'
)
foreach ($snippet in $requiredLifecycleEvidence) {
    Assert-Condition ($activeRuntimeText.Contains($snippet)) "Missing GCCZ lifecycle evidence: $snippet"
}

Write-Output "GCCZ town refactor boundary verification passed."
Write-Output "Core source files : $($standaloneCoreFiles.Count)"
Write-Output "Player resources  : $($resourceMappings.Count)"
Write-Output "Handoff documents : $($documentMappings.Count)"
Write-Output "Keyword triggers  : none in active GCCZ runtime"
