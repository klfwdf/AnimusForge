# Bannerlord 1.3.x / 1.4.5 Compatibility Diff

This document is the maintenance map for keeping AnimusForge compatible with both Bannerlord 1.3.x and 1.4.5.

It is not a full decompiled-source diff. Use it as the first checklist before editing gameplay, mission, encounter, party, UI, campaign behavior, model, or packaging code.

## Source Of Truth

- Local vanilla 1.3 source reference: the generated 1.3.5 vanilla source folder in this repo.
- Local vanilla 1.4 source reference: the generated 1.4.5 vanilla source folder in this repo.
- Project build selector: `AnimusForge.csproj` property `BannerlordApi`.
- 1.4 compile symbol: `BANNERLORD_1_4_OR_GREATER`.
- Dual output rules: `docs/bannerlord_dual_module_output.md`.

When a future change touches TaleWorlds APIs, first check whether the member exists in both local vanilla source folders or in both referenced DLL sets. Do not assume a 1.4 member exists in 1.3.

## Build Matrix

Always keep both commands passing:

```bat
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.3
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.4
```

Packaging must also remain dual-client. Use the repository one-click package batch, or invoke `package_mod.ps1 -DualClientPackages` after the dual module output step.

The package step must produce two ZIP roots:

- `AnimusForge_1_3_x/...`
- `AnimusForge_1_4_5/...`

Both DLL files inside packages must still be named `AnimusForge.dll`.

## Build And Reference Differences

| Area | 1.3.x | 1.4.5 | AnimusForge Rule |
|---|---|---|---|
| MSBuild selector | `/p:BannerlordApi=1.3` | `/p:BannerlordApi=1.4` | Never test only the default build. |
| Compile symbol | No `BANNERLORD_1_4_OR_GREATER` | Defines `BANNERLORD_1_4_OR_GREATER` | Use this symbol only for real API/signature differences. |
| TaleWorlds references | Prefer `VersionedDepsDir` / `_deps_auto` when available | Prefer current game install binaries | Keep 1.3 references available; otherwise 1.3 build can silently compile against 1.4 APIs. |
| MCM dependency | May resolve `Bannerlord.MBOptionScreen.v1.3.6.dll` | May resolve `v1.4.0` / `v1.4.1` | Do not hardcode a single MCM DLL filename in C# or scripts. |
| Module output | `Modules/AnimusForge_1_3_x` | `Modules/AnimusForge_1_4_5` | Never mix both versions into `Modules/AnimusForge`. |

## Known API Differences Already Hit

| Area | 1.3.x | 1.4.5 | Current Fix Pattern |
|---|---|---|---|
| `MobilePartyAIModel` | No `FortificationPortPatrolDistanceAsDays`; no `GetSettlementNearbyThreatAndAllyCheckRadius(Settlement,bool)` override in the same shape | Adds port patrol / settlement threat radius members | `CourierMobilePartyAIModel.cs` wraps these members in `#if BANNERLORD_1_4_OR_GREATER`. |
| `PlayerEncounter.GetBattleRewards` Harmony prefix | Uses `float` reward outputs plus `goldChange`, `playerEarnedLootPercentage`, and `ExplainedNumber` refs | Uses `ExplainedNumber` reward outputs and `playerEarnedLootRate`; no old gold/ref shape | `MilitaryExerciseBattleRewardsZeroPatch` has separate prefix signatures per version. |
| `MapEvent.ApplyRenownAndInfluenceChanges` | Patchable in the old path | Missing or changed enough that the old patch is skipped | `MilitaryExerciseBehavior` patches it only in 1.3 and logs the 1.4 skip. |
| `Mission.SpawnTroop` | Call includes an extra boolean argument before nullable position/direction args | The extra boolean argument is not present | `TroopInspectionBehavior.cs` uses version-specific calls. |
| `IAgentOriginBase` implementation | Does not require `IsInSameArmyAsPlayer` in the same interface shape | Requires/uses `IsInSameArmyAsPlayer` | Custom prisoner origin exposes it only under `BANNERLORD_1_4_OR_GREATER`. |
| `MissionCameraFadeView` availability | Meeting startup delay can use the mission fade behavior | The old fade view path is unavailable | `MeetingBattleLockMissionBehavior` skips that startup loading delay on 1.4. |
| Gauntlet mouse release handling | Widget override `OnMouseReleased()` is used | Release is handled by polling `Input.IsKeyReleased(InputKey.LeftMouseButton)` in late update | `DevMultilineEditableTextWidget.cs` uses separate paths. |
| Companion party roles | Older party role API shape may differ; direct `GetHeroPartyRoles(Hero)` calls can MissingMethod on 1.3 runtimes | `MobileParty.GetHeroPartyRoles(Hero)` is available for multi-role detection | `ShoutBehavior.cs` uses shared `GetRoleHolder(PartyRole)` checks for prompt labels. |

## Known Runtime Behavior Differences

| Area | Observed Risk | Compatibility Rule |
|---|---|---|
| Player encounter state | Public `PlayerEncounter` properties can be null, throw, or represent a different current event during redirects/end cleanup | Use `PlayerEncounterCompat` helpers for battle, encountered battle, map event, and campaign battle result access. |
| Meeting placement | 1.4 encounter scenes can place agents into invalid/void positions if the old fallback is used too early | Prefer frames built from actual battle lines; use `LordEncounterBehavior.BuildTargetHeroSpawnFrame()` / `BuildPlayerSpawnFrame()` only as fallback. |
| Party split / hero transfer | 1.4 is less forgiving when a hero appears in both source and target rosters after split/transfer | After moving heroes between parties, verify source/target counts and repair duplicate source roster entries. |
| Mission agent spawning | Spawn signature and origin interface differ | Keep all custom origin and spawn code version-gated and test actual mission entry in both versions. |
| Campaign battle result cleanup | Redirect/cleanup patches can run while the encounter is already ending | Guard cleanup paths with `PlayerEncounterCompat.HasCampaignBattleResult()` and safe map event lookup. |
| UI input focus | Gauntlet focus and mouse release flow differ | Use local widget compatibility paths; do not assume 1.3 overrides fire in 1.4. |

## Current Compatibility Hotspots

Review these files before changing related systems:

- `AnimusForge.csproj`: build selector, references, version symbols.
- `PlayerEncounterCompat.cs`: safe encounter and map event access.
- `Patch_PlayerEncounter_Start.cs`: encounter redirect guards.
- `Patch_GameMenu_ActivateGameMenu.cs`: menu redirect guards.
- `Patch_Conversation_Start_Intercept.cs`: conversation redirect guards.
- `Patch_ConversationManager_SetupAndStartMapConversation.cs`: map conversation guards.
- `Patch_ConversationManager_OpenMapConversation.cs`: map conversation guards.
- `MeetingBattleLockMissionBehavior.cs`: meeting mission placement, startup fade, encounter battle lookup.
- `AnimusForgeMeetingMissionViews.cs`: meeting mission view registration.
- `MilitaryExerciseBehavior.cs`: battle reward signature, party split/hero transfer repair, encounter cleanup.
- `TroopInspectionBehavior.cs`: mission spawn signatures, custom prisoner origin, hero transfer repair.
- `CourierMobilePartyAIModel.cs`: 1.4-only mobile party model overrides.
- `DevMultilineEditableTextWidget.cs`: Gauntlet mouse release/input differences.
- `ShoutBehavior.cs`: party role detection and scene agent navigation.
- `deploy_module.ps1` in the one-click script folder: dual module output.
- `package_mod.ps1` in the one-click script folder: dual client packaging.

## Compatibility Patterns

Prefer these patterns, in order:

1. Use an existing compatibility helper.

   Example: for encounter state, use `PlayerEncounterCompat.GetCurrentMapEventSafe()` instead of directly chaining `PlayerEncounter.Battle`, `EncounteredBattle`, and `MapEvent.PlayerMapEvent`.

2. Add a small compatibility helper near the affected domain.

   Example: if a new campaign behavior needs a property that moved between versions, hide the reflection or fallback lookup in one helper rather than scattering `try/catch` blocks.

3. Use `#if BANNERLORD_1_4_OR_GREATER` only when the code cannot compile in both versions.

   Good cases: method signatures, missing properties, interface members, constructor argument lists.

4. Use reflection only for private fields or when the public API differs but the underlying data is stable.

   Keep reflection helpers narrow and null-safe. Log once if an important field is missing.

5. Avoid version checks based on the installed game folder at runtime.

   The build already produces two DLLs. Runtime version guessing is less reliable than compile-time separation.

## New Feature Checklist

Before implementing:

- Identify whether the feature touches campaign, mission, encounter, party, settlement, model, UI, save data, or packaging.
- Search the local vanilla 1.3 and 1.4 source references for every unfamiliar TaleWorlds member.
- Search this project for existing helpers before adding a new version branch.
- Decide whether the difference is compile-time (`#if`) or runtime-safe helper logic.

During implementation:

- Keep common logic outside `#if` blocks.
- Put only the API-specific call or signature inside `#if`.
- If a Harmony patch targets a TaleWorlds method, verify the target method exists and the prefix/postfix signature matches both versions.
- If moving heroes or troops between rosters, validate both source and target counts after the operation.
- If spawning mission agents, verify both `Mission.SpawnTroop` signature and `IAgentOriginBase` implementation.
- If touching meeting or scene movement, prefer live `Agent` / `AgentIndex` / battle-line frames over raw coordinates.

After implementation:

- Build both `BannerlordApi=1.3` and `BannerlordApi=1.4`.
- Run one-click build/output if the change affects deployment.
- Run one-click packaging if the change affects release packaging.
- In-game test must enable only one AnimusForge module at a time:
  - Bannerlord 1.3.x: enable `AnimusForge_1_3_x`.
  - Bannerlord 1.4.5: enable `AnimusForge_1_4_5`.

## How To Update This Document

When a new version-specific issue is found:

1. Add the exact TaleWorlds member or behavior to `Known API Differences Already Hit` or `Known Runtime Behavior Differences`.
2. Add the project file that contains the compatibility fix to `Current Compatibility Hotspots`.
3. Add a short rule to `Compatibility Patterns` or `New Feature Checklist` if the issue can recur.
4. Re-run both builds before treating the documentation update as complete.

If Bannerlord 1.5 is added later, do not replace this document. Add a new section or a new document for `1.4.5 -> 1.5.x`, and keep the existing 1.3/1.4 rules intact.
