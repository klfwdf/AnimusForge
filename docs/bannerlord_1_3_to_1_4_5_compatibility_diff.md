# Bannerlord 1.3.x / 1.4.x Compatibility Diff

This document is the maintenance map for keeping AnimusForge compatible with Bannerlord 1.3.x and the 1.4.x API line. The checked-in decompiled 1.4 source baseline is 1.4.5; an actual build may use a newer verified 1.4.x game root, and its exact version is recorded in build metadata.

It is not a full decompiled-source diff. Use it as the first checklist before editing gameplay, mission, encounter, party, UI, campaign behavior, model, or packaging code.

## Source Of Truth

- Local vanilla 1.3 source reference: the generated 1.3.5 vanilla source folder in this repo.
- Local vanilla 1.4 source reference: the generated 1.4.5 vanilla source folder in this repo.
- Project build selector: `AnimusForge.csproj` property `BannerlordApi`.
- 1.4 compile symbol: `BANNERLORD_1_4_OR_GREATER`.
- Unified module / dual implementation output rules: `docs/bannerlord_dual_module_output.md`.

When a future change touches TaleWorlds APIs, first check whether the member exists in both local vanilla source folders or in both referenced DLL sets. Do not assume a 1.4 member exists in 1.3.

## Build Matrix

Always keep the verified unified build passing for both implementations plus Bootstrap:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\一键编译覆盖推送\build_single_module.ps1 `
  -ProjectRoot . `
  -BannerlordRoot "<Bannerlord root>" `
  -Configuration Debug `
  -Stage
```

Direct `BannerlordApi=1.3` project builds intentionally fail unless the unified script has first verified the pinned 1.3 reference line. A compile symbol must never be treated as reference provenance.

Packaging must produce one client ZIP with one `AnimusForge/...` root. The unified module contains a minimal `AnimusForge.Bootstrap.dll` plus both implementation DLLs:

- `AnimusForge/bin/Win64_Shipping_Client/AnimusForge.Bootstrap.dll`
- `AnimusForge/bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll`
- `AnimusForge/bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll`

`SubModule.xml` declares only the Bootstrap. The Bootstrap selects and loads exactly one implementation at runtime. Both implementation files remain named `AnimusForge.dll` and use the assembly simple name `AnimusForge` to preserve code and save-type identity.

## Build And Reference Differences

| Area | 1.3.x | 1.4.x | AnimusForge Rule |
|---|---|---|---|
| MSBuild selector | `/p:BannerlordApi=1.3` | `/p:BannerlordApi=1.4` | Never test only the default build. |
| Compile symbol | No `BANNERLORD_1_4_OR_GREATER` | Defines `BANNERLORD_1_4_OR_GREATER` | Use this symbol only for real API/signature differences. |
| TaleWorlds references | Unified build requires a verified 1.3 `_deps_auto` / `Bannerlord13ReferenceDir` overlay and routes every DLL present there through `VersionedDepsDir` | Unified build verifies the selected game root is a 1.4.x line | Never mark an implementation until its reference line has been verified; build metadata records the exact reference version. |
| MCM dependency | May resolve `Bannerlord.MBOptionScreen.v1.3.6.dll` | May resolve `v1.4.0` / `v1.4.1` | Do not hardcode a single MCM DLL filename in C# or scripts. |
| Implementation output | `Modules/AnimusForge/bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll` | `Modules/AnimusForge/bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll` | Build both, publish one module, and let the Bootstrap load exactly one implementation. |

## Known API Differences Already Hit

| Area | 1.3.x | 1.4.5 source baseline | Current Fix Pattern |
|---|---|---|---|
| `MobilePartyAIModel` | No `FortificationPortPatrolDistanceAsDays`; no `GetSettlementNearbyThreatAndAllyCheckRadius(Settlement,bool)` override in the same shape | Adds port patrol / settlement threat radius members | `CourierMobilePartyAIModel.cs` wraps these members in `#if BANNERLORD_1_4_OR_GREATER`. |
| `PlayerEncounter.GetBattleRewards` Harmony prefix | Uses `float` reward outputs plus `goldChange`, `playerEarnedLootPercentage`, and `ExplainedNumber` refs | Uses `ExplainedNumber` reward outputs and `playerEarnedLootRate`; no old gold/ref shape | `MilitaryExerciseBattleRewardsZeroPatch` has separate prefix signatures per version. |
| `MapEvent.ApplyRenownAndInfluenceChanges` | Patchable in the old path | Missing or changed enough that the old patch is skipped | `MilitaryExerciseBehavior` patches it only in 1.3 and logs the 1.4 skip. |
| `PlayerEncounter.RestartPlayerEncounter` | Three parameters: defender party, attacker party, `forcePlayerOutFromSettlement` | Adds fourth optional `isPlayerEncounterRestartedForRaid` parameter | `PlayerEncounterCompat.RestartPlayerEncounter(...)` resolves and invokes the runtime overload; mod code should not call `PlayerEncounter.RestartPlayerEncounter` directly. |
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

- `AnimusForge.csproj`: implementation build selector, references, version symbols, and isolated version artifact capture.
- Bootstrap project/source: minimal shared-API entry point, runtime version selection, implementation loading, and lifecycle forwarding.
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
- `AnimusForgeModulePaths.cs`: active module root must resolve only to `Modules/AnimusForge`; legacy versioned folders are read-only migration inputs at most.
- `deploy_module.ps1` in the one-click script folder: unified module assembly and deployment.
- `package_mod.ps1` in the one-click script folder: single unified-client ZIP packaging.

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

5. Keep runtime version selection inside the Bootstrap only.

   Implementation code must continue to rely on compile-time separation. The Bootstrap should inspect loaded TaleWorlds assembly/API identity, use an explicit supported-version mapping, and fail closed when the runtime is unsupported or ambiguous. Do not infer the version from the install folder name or module path.

The current pinned 1.3 overlay contains 18 high-use 1.3 assemblies and the project explicitly routes all of them into the 1.3 build. Some directly referenced assemblies are not yet present in that overlay (notably SandBox core/Gauntlet, Core ViewModel, Gauntlet Data/Prefab, SaveSystem, and TwoDimension), so new code touching those APIs still requires comparison against the local 1.3 source and ideally a future complete 1.3 reference snapshot. Do not treat a successful compile against an unpinned fallback assembly as proof of API compatibility.

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
- Run one-click unified build/output if the change affects deployment.
- Run one-click packaging if the change affects release packaging; it must produce one ZIP containing both implementation variants.
- In-game, enable the single `AnimusForge` launcher module. The Bootstrap must report that it selected `bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll` on 1.3.x or `bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll` on 1.4.x.
- Do not enable retired `AnimusForge_1_3_x` or `AnimusForge_1_4_5` modules alongside the unified module.

## How To Update This Document

When a new version-specific issue is found:

1. Add the exact TaleWorlds member or behavior to `Known API Differences Already Hit` or `Known Runtime Behavior Differences`.
2. Add the project file that contains the compatibility fix to `Current Compatibility Hotspots`.
3. Add a short rule to `Compatibility Patterns` or `New Feature Checklist` if the issue can recur.
4. Re-run both builds before treating the documentation update as complete.

If Bannerlord 1.5 is added later, do not replace this document. Add a new section or a new document for `1.4.5 -> 1.5.x`, and keep the existing 1.3/1.4 rules intact.
