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
    @("docs\audits\gccz-town-runtime-inventory.md", "docs\gccz\audits\gccz-town-runtime-inventory.md"),
    @("docs\testing\gccz-town-player-test-sequence.md", "docs\testing\gccz-town-player-test-sequence.md")
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
$allInterventionRuntimeText = ((Get-ChildItem -LiteralPath $FusedRoot -File -Filter "SiegeAiInterventionBehavior*.cs") | ForEach-Object {
    [System.IO.File]::ReadAllText($_.FullName)
}) -join "`n"

$keywordTriggerPatterns = @(
    'playerText\s*\.\s*(Contains|IndexOf|StartsWith|EndsWith)\s*\(',
    '(Contains|IndexOf|StartsWith|EndsWith)\s*\(\s*playerText',
    'Regex\.(IsMatch|Match|Matches)\s*\(\s*playerText'
)
foreach ($pattern in $keywordTriggerPatterns) {
    Assert-Condition (-not [System.Text.RegularExpressions.Regex]::IsMatch($activeRuntimeText, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) "Active GCCZ runtime contains a dialogue keyword trigger matching: $pattern"
}

$settlementEffectAdapterPath = Join-Path $FusedRoot "SiegeAiInterventionBehavior.TownSettlementEffectAdapter.cs"
Assert-Condition (Test-Path -LiteralPath $settlementEffectAdapterPath -PathType Leaf) "Missing GCCZ settlement-effect adapter: $settlementEffectAdapterPath"
$settlementEffectAdapterText = [System.IO.File]::ReadAllText($settlementEffectAdapterPath)
$directSettlementMutationPatterns = @(
    'AdjustSettlementLocalPublicTrustForExternal\s*\(',
    'AdjustPersonalTrustWholeDeltaForExternal\s*\(',
    'ChangeRelationAction\.ApplyPlayerRelation\s*\(',
    '\.Prosperity\s*[+\-*/]?=',
    '\.Loyalty\s*[+\-*/]?=',
    '\.Security\s*[+\-*/]?=',
    '\.FoodStocks\s*[+\-*/]?='
)
foreach ($runtimeFile in Get-ChildItem -LiteralPath $FusedRoot -File -Filter "SiegeAiInterventionBehavior*.cs") {
    if ($runtimeFile.FullName -eq $settlementEffectAdapterPath) {
        continue
    }
    $runtimeFileText = [System.IO.File]::ReadAllText($runtimeFile.FullName)
    foreach ($pattern in $directSettlementMutationPatterns) {
        Assert-Condition (-not [System.Text.RegularExpressions.Regex]::IsMatch($runtimeFileText, $pattern)) "GCCZ settlement mutation escaped its adapter in $($runtimeFile.Name): $pattern"
    }
}
$requiredSettlementEffectEvidence = @(
    'TownSettlementEffectPlan.FromPlunderDelta',
    'TownSettlementEffectPlan.FromMassacreDelta',
    'TownSettlementEffectPlan.FromFinalOutcome',
    'ApplyTownSettlementEffectPlan(',
    'ApplyOwnerRelationDelta(',
    'ApplyExtraNativeDevastateProsperityPenalty(',
    'BeginRepopulationProsperityGrowthDebuff(',
    'BeginCivicPositiveBuff('
)
$settlementEffectRuntimeText = $activeRuntimeText + "`n" + $settlementEffectAdapterText
foreach ($snippet in $requiredSettlementEffectEvidence) {
    Assert-Condition ($settlementEffectRuntimeText.Contains($snippet)) "Missing GCCZ settlement-effect evidence: $snippet"
}

$economyEffectAdapterPath = Join-Path $FusedRoot "SiegeAiInterventionBehavior.TownEconomyEffectAdapter.cs"
Assert-Condition (Test-Path -LiteralPath $economyEffectAdapterPath -PathType Leaf) "Missing GCCZ economy-effect adapter: $economyEffectAdapterPath"
$economyEffectAdapterText = [System.IO.File]::ReadAllText($economyEffectAdapterPath)
$directEconomyMutationPatterns = @(
    'GiveGoldAction\.ApplyBetweenCharacters\s*\(',
    '\.ChangeHeroGold\s*\(',
    'itemRoster\.AddToCounts\s*\(',
    'sourceRoster\.AddToCounts\s*\(',
    '_pendingLootRoster\.AddToCounts\s*\('
)
foreach ($runtimeFile in Get-ChildItem -LiteralPath $FusedRoot -File -Filter "SiegeAiInterventionBehavior*.cs") {
    if ($runtimeFile.FullName -eq $economyEffectAdapterPath) {
        continue
    }
    $runtimeFileText = [System.IO.File]::ReadAllText($runtimeFile.FullName)
    foreach ($pattern in $directEconomyMutationPatterns) {
        Assert-Condition (-not [System.Text.RegularExpressions.Regex]::IsMatch($runtimeFileText, $pattern)) "GCCZ economy mutation escaped its adapter in $($runtimeFile.Name): $pattern"
    }
}
$requiredEconomyEffectEvidence = @(
    'AwardGoldToPlayer(',
    'TransferHeroGoldToPlayer(',
    'RestoreItemStackToPlayerParty(',
    'MoveItemStackToPendingLoot('
)
foreach ($snippet in $requiredEconomyEffectEvidence) {
    Assert-Condition ($allInterventionRuntimeText.Contains($snippet)) "Missing GCCZ economy-effect evidence: $snippet"
}

$completionEffectAdapterPath = Join-Path $FusedRoot "SiegeAiInterventionBehavior.TownCompletionEffectAdapter.cs"
Assert-Condition (Test-Path -LiteralPath $completionEffectAdapterPath -PathType Leaf) "Missing GCCZ completion-effect adapter: $completionEffectAdapterPath"
$completionEffectAdapterText = [System.IO.File]::ReadAllText($completionEffectAdapterPath)
$directCompletionMutationPatterns = @(
    'ChangeOwnerOfSettlementAction\.',
    'SiegeAftermathAction\.ApplyAftermath\s*\(',
    'KillCharacterAction\.',
    'HeroCreator\.CreateNotable\s*\(',
    'EnterSettlementAction\.ApplyForCharacterOnly\s*\(',
    '\.AddPower\s*\(',
    '\.Culture\s*=(?!=)',
    '\.IsOwnerUnassigned\s*=(?!=)'
)
foreach ($runtimeFile in Get-ChildItem -LiteralPath $FusedRoot -File -Filter "SiegeAiInterventionBehavior*.cs") {
    if ($runtimeFile.FullName -eq $completionEffectAdapterPath) {
        continue
    }
    $runtimeFileText = [System.IO.File]::ReadAllText($runtimeFile.FullName)
    foreach ($pattern in $directCompletionMutationPatterns) {
        Assert-Condition (-not [System.Text.RegularExpressions.Regex]::IsMatch($runtimeFileText, $pattern)) "GCCZ completion mutation escaped its adapter in $($runtimeFile.Name): $pattern"
    }
}
$requiredCompletionEffectEvidence = @(
    'ApplySettlementOwnershipBySiege(',
    'ApplySettlementOwnershipByDefault(',
    'ApplyNativeSettlementAftermath(',
    'ApplySettlementCulture(',
    'ApplyHeroCulture(',
    'ClearNotablePowerForReplacement(',
    'CreateReplacementNotable(',
    'PlaceReplacementNotable(',
    'KillInterventionNotableByBattle(',
    'KillInterventionNotableByMurder(',
    'RemoveInterventionNotable('
)
foreach ($snippet in $requiredCompletionEffectEvidence) {
    Assert-Condition ($allInterventionRuntimeText.Contains($snippet)) "Missing GCCZ completion-effect evidence: $snippet"
}

$directAftermathAdapterPath = Join-Path $FusedRoot "SiegeAiInterventionBehavior.DirectAftermathAdapter.cs"
Assert-Condition (Test-Path -LiteralPath $directAftermathAdapterPath -PathType Leaf) "Missing GCCZ direct-aftermath adapter: $directAftermathAdapterPath"
$directAftermathAdapterText = [System.IO.File]::ReadAllText($directAftermathAdapterPath)
$directAftermathStatePath = Join-Path $fusedCore "TownDirectAftermathFlowState.cs"
Assert-Condition (Test-Path -LiteralPath $directAftermathStatePath -PathType Leaf) "Missing GCCZ direct-aftermath state: $directAftermathStatePath"
$directAftermathRuntimeText = $activeRuntimeText + "`n" + $directAftermathAdapterText
$obsoleteDirectAftermathPatterns = @(
    '_directMassacreAftermathScriptPending',
    '_directMassacreLootScreenOpened',
    '_directMassacreWaitingForLootClose',
    '_directPlunderAftermathScriptPending',
    '_directPlunderLootScreenOpened',
    '_directPlunderWaitingForLootClose'
)
foreach ($pattern in $obsoleteDirectAftermathPatterns) {
    Assert-Condition (-not $directAftermathRuntimeText.Contains($pattern)) "Obsolete direct-aftermath field returned: $pattern"
}
$requiredDirectAftermathEvidence = @(
    'TownDirectAftermathFlowState DirectAftermathFlow',
    'TryRunDirectAftermathScript(',
    'TryHandleDirectAftermathMenuForExternal(',
    'TryOpenDirectAftermathLootScreenNow('
)
foreach ($snippet in $requiredDirectAftermathEvidence) {
    Assert-Condition ($directAftermathRuntimeText.Contains($snippet)) "Missing GCCZ direct-aftermath evidence: $snippet"
}

$completionStatePath = Join-Path $fusedCore "TownEncounterCompletionState.cs"
Assert-Condition (Test-Path -LiteralPath $completionStatePath -PathType Leaf) "Missing GCCZ encounter-completion state: $completionStatePath"
$obsoleteCompletionPatterns = @(
    '_pendingSummarySwitch',
    '_pendingSummaryAftermath',
    '_completedSummaryText',
    '_pendingSummaryMenuPresented',
    '_pendingSummaryContinueRequested',
    '_pendingEncounterFinish',
    '_nativeDevastateSummaryContinueHandled'
)
foreach ($pattern in $obsoleteCompletionPatterns) {
    Assert-Condition (-not $allInterventionRuntimeText.Contains($pattern)) "Obsolete encounter-completion field returned: $pattern"
}
$requiredCompletionEvidence = @(
    'TownEncounterCompletionState EncounterCompletion',
    'EncounterCompletion.BeginSummary(',
    'EncounterCompletion.QueueFinish(',
    'EncounterCompletion.HasSettledWithoutNativeMenu('
)
foreach ($snippet in $requiredCompletionEvidence) {
    Assert-Condition ($allInterventionRuntimeText.Contains($snippet)) "Missing GCCZ encounter-completion evidence: $snippet"
}

$sceneControlStatePath = Join-Path $fusedCore "TownSceneControlState.cs"
Assert-Condition (Test-Path -LiteralPath $sceneControlStatePath -PathType Leaf) "Missing GCCZ scene-control state: $sceneControlStatePath"
$obsoleteSceneControlPatterns = @(
    '_civilianSpeechRallyActive',
    '_civilianGatherPropagationActive',
    '_civilianFormationControlPending',
    '_civilianFormationControlComplete',
    '_civilianFormationControlMessageShown',
    '_soldierDefaultFollowOrderIssued',
    '_playerOrderControllerPrimed',
    '_civilianOrderControllerPrimed',
    '_civilianGatherStartedAt',
    '_nextCivilianGatherTickTime',
    '_civilianGatherMessengerSpeechCount',
    '_civilianFormationControlNotBeforeTime',
    '_nextCivilianFormationControlBatchTime',
    '_nextPlayerOrderControllerPrimeTime',
    '_civilianAssemblyPointReady'
)
foreach ($pattern in $obsoleteSceneControlPatterns) {
    Assert-Condition (-not $allInterventionRuntimeText.Contains($pattern)) "Obsolete scene-control field returned: $pattern"
}
$requiredSceneControlEvidence = @(
    'TownSceneControlState SceneControl',
    'SceneControl.TryStartCivilianGather(',
    'SceneControl.TryQueueCivilianFormationControl(',
    'SceneControl.TryScheduleCivilianFormationControlBatch(',
    'SceneControl.CanPrimePlayerOrderController('
)
foreach ($snippet in $requiredSceneControlEvidence) {
    Assert-Condition ($allInterventionRuntimeText.Contains($snippet)) "Missing GCCZ scene-control evidence: $snippet"
}

$requiredLifecycleEvidence = @(
    'IsActiveInCurrentMission()',
    'EndInterventionSceneScope("mission_ended")',
    'InterventionSceneMemory.EndScene()',
    'ClearInterventionSceneTransientState()',
    'ResetNpcResponseBudgetForExternal("town_scene_transient_clear")',
    'PendingAmbientReactionRequests.Clear()',
    'ActiveAmbientResponseEventIds.Clear()'
)
foreach ($snippet in $requiredLifecycleEvidence) {
    Assert-Condition ($activeRuntimeText.Contains($snippet)) "Missing GCCZ lifecycle evidence: $snippet"
}
$mainRuntimePath = Join-Path $FusedRoot "SiegeAiInterventionBehavior.cs"
$mainRuntimeText = [System.IO.File]::ReadAllText($mainRuntimePath)
$campaignListenerCount = [System.Text.RegularExpressions.Regex]::Matches($mainRuntimeText, 'CampaignEvents\.[A-Za-z0-9_]+\.AddNonSerializedListener\s*\(\s*this\s*,').Count
Assert-Condition ($campaignListenerCount -gt 0) "No campaign-lifetime GCCZ listeners were found in RegisterEvents."
$registerEventsMatch = [System.Text.RegularExpressions.Regex]::Match(
    $mainRuntimeText,
    'public\s+override\s+void\s+RegisterEvents\s*\(\s*\)\s*\{(?<body>.*?)\n\s*\}',
    [System.Text.RegularExpressions.RegexOptions]::Singleline
)
Assert-Condition ($registerEventsMatch.Success) "Could not locate the GCCZ RegisterEvents body."
$listenersOutsideRegisterEvents = $mainRuntimeText.Remove($registerEventsMatch.Index, $registerEventsMatch.Length)
Assert-Condition (-not [System.Text.RegularExpressions.Regex]::IsMatch($listenersOutsideRegisterEvents, 'AddNonSerializedListener\s*\(')) "A GCCZ campaign listener is registered outside RegisterEvents."
$dynamicEventSubscriptionPattern = '(?m)\b[A-Za-z_][A-Za-z0-9_.]*(?:Event|Events|Dispatcher)\s*[+\-]='
foreach ($runtimeFile in Get-ChildItem -LiteralPath $FusedRoot -File -Filter "SiegeAiInterventionBehavior*.cs") {
    $runtimeFileText = [System.IO.File]::ReadAllText($runtimeFile.FullName)
    Assert-Condition (-not [System.Text.RegularExpressions.Regex]::IsMatch($runtimeFileText, $dynamicEventSubscriptionPattern)) "Dynamic GCCZ event subscription requires symmetric cleanup in $($runtimeFile.Name)."
}
$missionEndFinallyPattern = 'private\s+void\s+OnMissionEnded\s*\([^)]*\)\s*\{.*?finally\s*\{.*?EndInterventionSceneScope\("mission_ended"\)'
Assert-Condition ([System.Text.RegularExpressions.Regex]::IsMatch($mainRuntimeText, $missionEndFinallyPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)) "OnMissionEnded must clear GCCZ scene scope from a finally block."

Write-Output "GCCZ town refactor boundary verification passed."
Write-Output "Core source files : $($standaloneCoreFiles.Count)"
Write-Output "Player resources  : $($resourceMappings.Count)"
Write-Output "Handoff documents : $($documentMappings.Count)"
Write-Output "Keyword triggers  : none in active GCCZ runtime"
Write-Output "Effect mutations  : confined to town settlement adapter"
Write-Output "Economy mutations : confined to town economy adapter"
Write-Output "Completion effects: confined to native completion adapter"
Write-Output "Direct aftermath  : one explicit flow state and adapter"
Write-Output "Encounter finish  : one explicit completion state"
Write-Output "Scene control     : one mission-scoped state"
Write-Output "Event lifecycle   : campaign-owned listeners and mission-finally cleanup"
