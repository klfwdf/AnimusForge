# SETS Urban Capture Refactor Handoff

Date: 2026-08-25

## Objective

Refactor the SETS hostile settlement-entry capture flow without changing its current gameplay contract, settlement outcomes, troop costs, reward or penalty values, or GCCZ aftermath choices.

The first target is an enemy or otherwise non-owned town entered through the normal town-center location. Castle capture may reuse the same extracted policies where the current contract is identical, but village raids and owned or ruler-attached settlement incidents are separate flows and must not be folded into the hostile urban-capture state machine.

This handoff records the current runtime path and the required compatibility anchors. It does not authorize gameplay changes by itself.

## Repository Baseline

- Reusable policy source of truth: `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention`
- Active fused runtime: `G:\AFMOD\NEW-10`
- Observed GCCZ baseline: `501b890`
- Observed fused baseline: `5a0a9167`
- GitHub remote: `https://github.com/klfwdf/AnimusForge.git`

Before implementation, fetch the remote and re-audit the named methods because upstream AF may move the bridge points.

## Current Player-Facing Contract

The refactor must initially preserve all of the following:

1. SETS keeps two saved regular-troop profiles: up to 100 followers for owned settlements and up to 10 for other settlements. Heroes are excluded from these saved profiles.
2. Normal town entry is intercepted only at `TownEncounter.CreateAndOpenMissionController` for the `center` location. Vanilla entry continues after SETS prepares guarded mission context.
3. Selected followers are spawned in batches of 10 after a 0.75-second delay, with 0.15 seconds between batches. They remain commandable and use a mission-local agent-index allowlist.
4. A hostile capture conflict begins from an actual valid hit between the player side and an eligible settlement defender. Ordinary discussion alone does not silently capture a settlement.
5. The initial hostile objective includes existing guards, soldiers, and lords. Reserve sources include the garrison, militia, and resident lord parties.
6. Defender reserves use 30 troops per wave, three reserve phases, no more than four active waves, and a 30-second wave interval. Town reserves prefer workshop areas, with 10 defenders per workshop marker.
7. Player-side follower casualties and defender reserve casualties are removed from their originating campaign rosters once. Friendly-fire protection must not charge a follower casualty to the player roster.
8. TAB exit is blocked while a hostile conflict is active and undefeated defenders or reserve troops remain.
9. Victory is reached only after the tracked objective defenders are defeated and all reserve sources are exhausted. The current two-second forced mission-end fallback remains available if the normal mission exit stalls.
10. After returning to `MapState`, hostile town or castle victory transfers ownership to the player clan, prepares the native settlement-taken context, and opens `menu_settlement_taken`.
11. The native settlement-taken menu continues to offer its original aftermath choices and the existing GCCZ entry. SETS must not bypass GCCZ or duplicate GCCZ outcome settlement.
12. Owned or ruler-attached settlement incidents never transfer ownership. They use the existing dedicated incident menu and relation consequences instead of the hostile capture route.
13. Village victory remains a separate militia and force-supplies reward path.
14. Existing crime, same-kingdom vassal rebellion, notable-death, original-owner relation, native aftermath, and GCCZ reward and penalty values remain unchanged until explicitly redesigned and snapshot-tested.

## Live Runtime Path

### 1. Entry preparation

`G:\AFMOD\NEW-10\SettlementEntryTroopSelectionBehavior.cs`

- `InstallPatches(...)` patches town, castle, and village mission-controller entry points plus the relevant mission-exit and damage paths.
- `TownCreateAndOpenMissionControllerPrefix(...)` calls `TryPrepareSettlementEntryMission(...)` for the town-center location.
- `TryPrepareSettlementEntryMission(...)` resolves the owned or other profile, copies an available roster, classifies the scene, and stores `PendingMissionEntry`.
- `OnMissionStarted(...)` validates the live settlement and attaches one `SettlementEntryTroopSelectionMissionLogic`.

### 2. Mission setup and conflict

`SettlementEntryTroopSelectionMissionLogic` currently owns most of the runtime:

- staged follower spawning and command setup;
- mission-local allied and enemy agent-index sets;
- physical-hit routing in `OnAgentHit(...)` and `OnScoreHit(...)`;
- `StartConflict(...)`, team changes, crime delegation, rebellion queuing, and the initial defender wave;
- garrison, militia, and lord reserve extraction and spawning;
- native combat orders, navigation rescue, friendly-fire protection, and casualty settlement;
- victory tracking and exit blocking.

The class is currently nested inside a 6,700-line campaign behavior and contains both live Bannerlord side effects and business state.

### 3. Victory and post-mission transition

- `ReachVictory(...)` marks victory, prepares exit state, and queues the post-mission flow.
- `QueueVictoryPostMissionFlow(...)` selects the town or castle native-menu route, the village reward route, or the owned-incident route.
- `QueueSettlementTakenMenuAfterVictory(...)` stores a static `PendingSettlementVictoryMenuEntry` containing the settlement id, surviving roster, source, ownership-transfer flag, incident flag, and notable-death flag.
- `PumpPendingPostMissionFlow(...)` is called from campaign tick, mission-ended, and game-menu-opened events.
- `TryPumpPendingSettlementTakenMenu(...)` waits for `MapState`, resolves the settlement, and calls `SiegeAiInterventionBehavior.TryOpenSettlementEntryVictoryMenu(...)`. A failed bridge call restores the pending entry and retries.

### 4. Ownership and native aftermath menu

`G:\AFMOD\NEW-10\SiegeAiInterventionBehavior.cs`

- `TryOpenSettlementEntryVictoryMenu(...)` resets stale GCCZ guards, records SETS capture context, stores the surviving intervention roster, and branches between hostile capture and owned incident.
- For hostile capture, `PrepareSetsCapturedTownRiotContext(...)` preserves the original owner for later relation handling.
- `ApplySetsSettlementEntryCaptureIfNeeded(...)` first uses the existing `ApplySettlementOwnershipBySiege(...)` effect adapter, then uses the default ownership action only if the first action did not leave the settlement with the player clan.
- `ConfirmSettlementOwnershipAssignment(...)` clears the unassigned-owner state after successful transfer.
- `PrepareNativeSettlementTakenMenuContextForSets(...)` writes the native `SiegeAftermathCampaignBehavior` fields required by `menu_settlement_taken` because SETS did not originate from a native siege `MapEvent`.
- `CaptureNativeSiegeContext(...)` substitutes the main party, preserved previous owner, and safe contribution map while the SETS capture context is active.
- Final aftermath is still applied by the existing GCCZ/native completion path. Do not introduce a second reward or consequence implementation in SETS.

### 5. Thin effect adapter already extracted

`G:\AFMOD\NEW-10\SiegeAiInterventionBehavior.TownCompletionEffectAdapter.cs`

This file already centralizes the direct Bannerlord calls for siege ownership, default ownership, and ownership assignment. Keep these as runtime effects; move decisions and idempotency out of the large behavior instead of moving Bannerlord types into the standalone core.

## Existing Reusable Core

The standalone project already contains:

- `SetsSettlementSceneKind.cs`
- `SetsSettlementEntryProfile.cs`
- `SetsOwnedSettlementIncidentProfile.cs`
- `SetsOwnedSettlementMassacreProfile.cs`
- `SetsVillageVictoryRewardProfile.cs`
- `SiegeAftermathMenuProfile.cs`
- `SiegeAgentWallRescueProfile.cs`

`SetsSettlementEntryProfile` currently owns basic scene routing and wording, but it does not model a hostile-capture session, wave ledger, victory transition, ownership commit, or post-mission retry state.

## Architecture Problems to Remove

These are refactor risks, not permission to change behavior:

1. `SettlementEntryTroopSelectionBehavior.cs` mixes saved configuration, Harmony entry patches, mission combat, spawning, casualties, civilian gathering, massacre handling, village aftermath, rebellion, and victory handoff.
2. `SettlementEntryTroopSelectionMissionLogic` uses many related booleans and collections without one explicit capture state or operation identity.
3. `PendingSettlementVictoryMenuEntry` is static runtime state and is not included in `SyncData`. A future persistence change must be versioned and idempotent rather than merely serializing live `TroopRoster` and `Agent` objects.
4. Hostile capture and owned-settlement incident flags share the same queue and bridge signature. This makes an accidental ownership transfer a high-impact regression.
5. Ownership transfer, native private-field preparation, menu activation, selected-roster handoff, and relation context are performed in one method with partial-failure retries.
6. Native aftermath compatibility relies on reflected private field names. This must remain isolated and validated separately for Bannerlord 1.3 and 1.4.
7. Capture state is split between `SettlementEntryTroopSelectionBehavior` and static fields in `SiegeAiInterventionBehavior`, so stale cleanup depends on several manual reset paths.
8. Player-visible Chinese text remains embedded in old C# paths. When touched, move it to a dedicated SETS localization or presentation resource without changing the displayed meaning.
9. Existing standalone tests cover profiles and constants but do not yet snapshot the complete hostile capture transition, ownership transfer eligibility, retry deduplication, or native-menu handoff plan.

## Target Design

Do not create a separate settlement-management game. Use one capture aggregate and a small number of meaningful adapters.

### Standalone core

Add dependency-free types under `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention` first:

- `SetsUrbanCaptureState`: explicit states such as `Inactive`, `EntryPrepared`, `MissionActive`, `ConflictActive`, `VictoryReached`, `AwaitingMap`, `OwnershipCommitted`, `MenuOpened`, and `Completed`.
- `SetsUrbanCaptureContext`: immutable settlement id, scene kind, ownership classification, previous-owner id, selected-follower snapshot, and operation id.
- `SetsUrbanCaptureLedger`: unique allied casualties, defender casualties, reserve withdrawals, victory commitment, ownership commitment, menu commitment, and completion commitment.
- `SetsUrbanCapturePolicy`: pure decisions for valid transition, ownership-transfer eligibility, exit blocking, reserve exhaustion, and victory readiness.
- `SetsUrbanCaptureCompletionPlan`: a pure result describing whether to preserve ownership, transfer ownership, open the native menu, open the owned-incident menu, grant the village reward, or do nothing.

Keep numerical compatibility anchors in one existing or new profile. Do not copy the same limits and wave constants into several classes.

### Fused runtime

Retain only these responsibilities in AF/Bannerlord code:

- resolve live settlement, clan, party, roster, mission, team, location, and agent objects;
- attach one active SETS mission adapter;
- spawn agents and issue engine orders from a plan;
- report unique runtime events to the core session;
- apply the core completion plan through the existing ownership and aftermath effect adapters;
- prepare the native menu reflection boundary;
- log state transitions and rejection reasons.

The likely useful extraction boundary is one mission adapter plus one post-mission transition coordinator. Avoid producing many single-method wrapper classes.

## Required State Separation

The hostile and owned paths must diverge before any ownership side effect:

```text
Hostile urban capture
EntryPrepared -> MissionActive -> ConflictActive -> VictoryReached
-> AwaitingMap -> OwnershipCommitted -> NativeMenuOpened -> Aftermath -> Completed

Owned or ruler-attached incident
EntryPrepared -> MissionActive -> IncidentTriggered
-> AwaitingMap -> OwnedIncidentMenuOpened -> Completed
```

There must be no transition from the owned path to `OwnershipCommitted`.

## Recommended Implementation Slices

### Slice A: lock the legacy contract

Before moving runtime logic, add snapshot tests for:

- 100/10 profile limits and hero exclusion;
- staged spawn timing and batch size;
- defender phase order, wave size, interval, and active-wave cap;
- valid town defenders and reserve sources;
- victory requires no live objective defenders and no remaining reserves;
- active hostile conflict blocks exit;
- hostile town and castle use the native siege victory menu;
- owned or attached incidents skip ownership transfer;
- villages never enter the urban ownership path;
- surviving-roster handoff and unique casualty accounting;
- complete native and GCCZ rewards, penalties, thresholds, and requirements.

### Slice B: extract capture state and policy

Replace the related boolean combination with `SetsUrbanCaptureState`, context, and ledger. Keep the old mission side effects in place temporarily, but make one core transition the source of truth. Delete the superseded boolean decision branches in the same slice.

### Slice C: isolate mission combat execution

Move the nested mission logic into a focused runtime file. Keep agent lookup, spawning, teams, combat orders, navigation rescue, and damage callbacks together because they share hot mission state. Cache live sets; do not scan all mission agents every frame.

### Slice D: make post-mission completion idempotent

Replace `PendingSettlementVictoryMenuEntry` with a versioned, operation-id-based transition record. Commit ownership and open the menu exactly once. A retry may continue from the last committed stage but must not repeat casualties, ownership events, relation changes, rewards, or penalties.

### Slice E: isolate the native aftermath boundary

Move reflected `SiegeAftermathCampaignBehavior` field writes behind one compatibility adapter with explicit 1.3 and 1.4 verification. Keep `SiegeAiInterventionBehavior` responsible only for active GCCZ runtime and calls into the adapter.

### Slice F: resources and cleanup

Move touched player-visible SETS wording into resource files, remove dead fields and compatibility branches replaced by the new path, update the handoff, run cleanup searches, then run both Bannerlord builds.

## Compatibility and Save Rules

- Preserve `_setsOwnSettlementEntryProfile_v1`, `_setsOtherSettlementEntryProfile_v1`, and the existing rebellion save keys.
- New persisted records require new versioned keys. Missing fields must mean no pending capture, not an inferred victory.
- Never serialize `Agent`, `Mission`, `Team`, or other scene objects. Persist stable ids, counts, committed flags, and roster snapshots in a Bannerlord-supported form.
- A corrupt or stale pending record must fail closed without transferring ownership.
- A loaded record may resume only if its settlement, previous owner, player clan, operation id, and expected transition still agree with live state.
- Do not write migration code for hypothetical formats that never shipped.

## Required Tests

### Standalone

- every legal and illegal state transition;
- hostile versus owned ownership eligibility;
- empty, partial, and exhausted defender reserves;
- duplicate allied and defender casualty events;
- duplicate victory, ownership, menu, and completion commands;
- retry after ownership succeeds but before menu activation;
- stale settlement id, changed owner, missing clan, corrupt record, and old save without new fields;
- town, castle, village, and unsupported scene routing;
- exact legacy numerical snapshots.

### Fused static and build checks

- verify the town `center` entry patch and one mission behavior registration;
- verify `SceneTauntBehavior` yields to active SETS capture damage handling but remains normal elsewhere;
- verify owned incidents cannot request ownership transfer;
- verify only one ownership action is applied for hostile capture;
- verify native private-field names for both supported Bannerlord versions;
- run GCCZ standalone tests and verifier;
- build `BannerlordApi=1.3`, `BannerlordApi=1.4`, and Bootstrap with zero errors;
- run conflict-marker, dead-code, duplicate-path, and new-CJK-in-C# searches;
- verify the reusable core mirrors semantically between GCCZ and NEW-10.

### In-game sequence

1. Enter an enemy town with zero configured followers and confirm vanilla entry plus guarded SETS behavior.
2. Enter with 1 and 10 configured followers; verify staged spawn, commandability, and no unrelated NPC classification.
3. Start the conflict by hitting a valid guard, then verify current defenders, garrison, militia, and lord reserves.
4. Attempt TAB before victory and confirm exit is blocked.
5. Kill or knock out all objective defenders, verify one victory, return to the map, verify one ownership transfer, and verify one native settlement-taken menu.
6. Enter GCCZ from that menu and verify the preserved previous owner, surviving follower roster, and unchanged aftermath values.
7. Repeat through original mercy, pillage, and devastation choices using separate saves and verify no duplicate reward or penalty.
8. Test a player-owned town and a ruler-attached vassal town; verify neither transfers ownership.
9. Exit a normal town without starting conflict and verify no pending menu, capture, crime, or stale SETS state.
10. Repeat the capture with save/load only at supported map-state checkpoints and inspect `SETS.log` plus GCCZ diagnostics.

## Diagnostics

Keep `SETS.log` concise by default. Transition logs should include:

- operation id;
- settlement id and scene kind;
- previous-owner and ownership-classification ids;
- old state, requested event, and new state;
- allied, objective-defender, and remaining-reserve counts;
- ownership and menu commit status;
- retry count and rejection reason.

Do not log full prompts, private player text, or per-frame agent dumps unless verbose SETS diagnostics are explicitly enabled.

## Out of Scope Unless Reconfirmed

- redesigning the 100/10 profile limits;
- rebalancing reserve waves or casualty costs;
- changing native mercy, pillage, devastation, GCCZ rewards, or relation penalties;
- semantic dialogue keywords that capture a town without the validated SETS action path;
- village reward redesign;
- owned-settlement massacre redesign;
- a new town-management interface;
- deployment to the game directory.

## First Action for the Next Task

Start with Slice A. Prove the live enemy-town path from `TownCreateAndOpenMissionControllerPrefix(...)` to `TryOpenSettlementEntryVictoryMenu(...)`, then snapshot the current numerical and completion contract before extracting any state. Do not begin by moving all 6,700 mission-behavior lines or all 16,000 GCCZ runtime lines.
