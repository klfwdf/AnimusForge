# SETS Urban Capture Refactor Progress Handoff

Date: 2026-08-26
Supersedes progress tracking in: `sets-urban-capture-refactor-handoff-20260825.md` (that document's contract, runtime path, and compatibility rules remain authoritative; this document records execution state).

## User Directives Received During Execution

1. Structural improvements are authorized at the implementer's discretion ("有什么想法都可以改改").
2. The goal is better game runtime stability ("只要让游戏更好的运行就可以了"). Gameplay values stay frozen per the original handoff.
3. Do not ask for confirmation; follow the plan, keep backups, write a handoff at the end.

## Approved Plan

`C:\Users\28358\.claude\plans\toasty-scribbling-bee.md` — Slice A–F plus a "runtime quality fixes" table (bounded bridge retries, explicit ownership-commit ledger, single-object state reset, validated reflection boundary). Read it before continuing.

## Backups

- GCCZ tag `backup/pre-sets-refactor-20260825` at `d048516` (branch `codex/gccz-village`)
- NEW-10 tag `backup/pre-sets-refactor-20260825` at `0ee4774f` (branch `main`, still 1 commit ahead of origin at that point; remote unreachable in this environment)

## Completed

### Slice A — legacy contract frozen (DONE)

- New `SetsUrbanCaptureContractProfile.cs` (GCCZ core + NEW-10 mirror): 100/10 follower limits, 30-troop waves, 3 reserve phases, 4 active waves, 30s wave interval, 10 per workshop marker, 0.75s/0.15s spawn timing, 2s forced end fallback, both `_sets*SettlementEntryProfile_v1` save keys, garrison→militia→lord_party phase order, `owner_hero` folded into lord_party.
- `tests/.../Program.cs`: `TestSetsUrbanCaptureContractProfile` (30 assertions) — includes scene routing (only town/castle reach the native victory menu), owned-incident menu id freeze, and the native -15/-30 relation baseline offsets.
- `tools/verify_gccz_town_refactor.ps1`: new SETS contract section asserting, against the fused sources: frozen constant declarations, exact SyncData save-key lines, town-center entry patch registration, `ReachVictory("all_defenders_defeated")`, the exact TAB-block condition, `skipOwnershipTransfer: _isOwnSettlement || _ownedSettlementIncidentTriggered`, the exact bridge call signature, a ban on `ChangeOwnerOfSettlementAction` inside the SETS behavior, presence of the three hostile-capture bridge methods, and all 7 reflected native field names.

### Slice B — standalone capture core (DONE; runtime wiring NOT started)

New dependency-free types in `GCCZ\src\AnimusForge.SiegeAftermathIntervention\`, mirrored to `NEW-10\AnimusForge.SiegeAftermathIntervention\`:

| File | Purpose |
|---|---|
| `SetsUrbanCaptureState.cs` | `SetsUrbanCaptureState` (11 states incl. `IncidentTriggered`/`OwnedIncidentMenuOpened` branch), `SetsUrbanCaptureEvent` (12 events incl. `Abort`), `SetsUrbanCaptureOwnershipClassification` (Hostile / PlayerOwned / RulerAttached) |
| `SetsUrbanCaptureContext.cs` | Immutable identity (operationId, settlementId, sceneKind, classification, previousOwnerClanId, follower count); `MatchesLiveState(...)` fails closed on settlement/owner drift |
| `SetsUrbanCaptureLedger.cs` | Once-only gates: per-agent allied/defender casualties, per-phase reserve withdrawal, victory/ownership/menu/villageReward/completion commits; `RestoreCommittedStages` for load recovery (never restores mission-scoped agent indexes) |
| `SetsUrbanCapturePolicy.cs` | Legal-transition table, `ResolveNextState`, `IsOwnershipTransferEligible` (hostile+town/castle+AwaitingMap only), `ShouldBlockExit`, `IsVictoryReady` |
| `SetsUrbanCaptureCompletionPlan.cs` | Pure completion plan (transfer / native menu / owned menu / village reward / nothing) with named rejection reasons; retry after committed ownership plans menu-only |
| `SetsUrbanCaptureSession.cs` | Aggregate (context+state+ledger); single-object replacement for scattered static state; `DescribeForLog()` |

Key invariants proven by tests: owned path can never reach `OwnershipCommitted` (state table has no such edge AND `TryApply` double-checks classification); `EndMission` from `MissionActive` without conflict returns to `Inactive` (quiet visit leaves nothing pending); invalid context rejects every event.

Tests: `TestSetsUrbanCaptureStateMachine`, `TestSetsUrbanCaptureLedger`, `TestSetsUrbanCaptureCompletionPlan` (~70 assertions). Full suite passes.

## Commits

| Repo | Commit | Content |
|---|---|---|
| GCCZ (`codex/gccz-village`) | `7052601` | Slice A contract + verifier + Slice B core + tests |
| NEW-10 (`main`) | `3a4c66d6` | Mirror of the 7 new core files (no runtime wiring) |

Neither repo pushed (remote unreachable). NEW-10 is now 2 commits ahead of origin/main.

## Build Environment (solved this session; reuse verbatim)

Network is restricted (github.com and nuget.org unreachable; proxy 127.0.0.1 refuses). Game is NOT at the csproj default `F:\SteamLibrary\...`; the real install is **`E:\Steam\steamapps\common\Mount & Blade II Bannerlord` (v1.4.7)** with workshop content at **`E:\Steam\steamapps\workshop\content\261550`** (Harmony 2859188632, UIExtenderEx 2859222409, MBOptionScreen 2859238197).

Working unified build (1.3 + 1.4 + Bootstrap, stage-only, no game dir modification):

```powershell
cd G:\AFMOD\NEW-10
powershell -NoProfile -ExecutionPolicy Bypass -Command "
  $env:PATH = 'G:\AFMOD\.dotnet-sdk;' + $env:PATH;
  $env:DOTNET_CLI_HOME = 'G:\AFMOD\.dotnet-home';
  $env:NUGET_PACKAGES = 'C:\Users\28358\.nuget\packages';
  & './一键编译覆盖推送/build_single_module.ps1' -ProjectRoot . `
    -BannerlordRoot 'E:\Steam\steamapps\common\Mount & Blade II Bannerlord' `
    -WorkshopContentDir 'E:\Steam\steamapps\workshop\content\261550' `
    -Configuration Debug -Stage"
```

Notes:
- `NUGET_PACKAGES` must point at `C:\Users\28358\.nuget\packages` (has `microsoft.netframework.referenceassemblies*` 1.0.3); `G:\AFMOD\.dotnet-home\.nuget\packages` lacks the net472 pack and restore then fails offline.
- Direct `-p:BannerlordApi=1.3` builds are fail-closed by design; always use the script.
- A quick 1.4-only compile check: `dotnet msbuild AnimusForge.csproj -p:Configuration=Debug -p:BannerlordApi=1.4 -p:"BannerlordRoot=E:\..." -p:"WorkshopContentDir=E:\..." -p:BaseIntermediateOutputPath=obj/codex_gccz_14/` (reuses the historical per-variant obj dir whose assets point at the correct cache).
- Standalone tests: `G:\AFMOD\.dotnet-sdk\dotnet.exe run --project G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\...csproj` (set `DOTNET_CLI_HOME=G:\AFMOD\.dotnet-home`).
- Verifier: `powershell -NoProfile -ExecutionPolicy Bypass -File G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1`.
- In Git Bash, `/p:X` gets mangled — always write `-p:X`. Avoid `cd X && git commit` compounds; use `git -C`.

## Verification State (all green as of the commits above)

1. Standalone tests: pass (`All GCCZ standalone core tests passed.`)
2. Verifier: pass (new summary line `SETS contract : frozen limits, entry patch, exit block, ownership route, reflection fields`)
3. Unified build: Bootstrap + 1.3 + 1.4 all built and staged to `bin\Debug\single_module_stage\AnimusForge` (no game dir touched)
4. Core mirror: GCCZ vs NEW-10 directory listings identical (169→176 files, verifier hashes all files)

## Remaining Work

### Slice B runtime wiring (NEXT ACTION)

In `NEW-10\SettlementEntryTroopSelectionBehavior.cs`:
1. Give `SettlementEntryTroopSelectionMissionLogic` one `SetsUrbanCaptureSession` field, created in `TryPrepareSettlementEntryMission` (classification: `Hostile` when not own/attached; `PlayerOwned`/`RulerAttached` from the existing `_isOwnSettlement` / ruler-attach checks; operationId e.g. settlementId+CampaignTime tick).
2. Route through `session.TryApply(...)` at: `OnMissionStarted` (StartMission), `StartConflict` (StartConflict — L3201), `StartOwnedSettlementIncident` (TriggerOwnedIncident), `ReachVictory` (ReachVictory — L6305, gate on `Ledger.TryCommitVictory()`), `OnEndMission` (EndMission), and use `SetsUrbanCapturePolicy.ShouldBlockExit` inside `OnEndMissionRequest` (L6288) and `IsVictoryReady` inside the tick check (L2903–2912).
3. Wire `SettleAlliedCasualty` (L6244) and `SettleDefenderReserveDefeat` (L6260) through `Ledger.TryRecordAlliedCasualty/TryRecordDefenderCasualty` and delete `_settledCasualtyAgentIndexes`/`_settledDefenderReserveAgentIndexes`.
4. Keep behavior identical: the boolean fields may remain as mirrors this slice, but decisions must read the session. Delete superseded boolean decision branches in the same slice.
5. Re-run: tests, verifier, unified build. Commit both repos.

### Slice C — move mission logic out (task #3)

Extract the nested class to `NEW-10\SettlementEntryTroopSelectionMissionLogic.cs` (namespace `AnimusForge`). Nested-class references to outer `private static` members (`_pendingVictoryMenuEntry`, `SetsSelectedFollowerAgentIndexes`, `_setsActiveUsableProtectionMission`, etc.) need `internal static` promotion or accessor methods. Add verifier assertions: file exists; behavior file no longer contains combat callbacks.

### Slice D — idempotent completion (task #4)

Replace static `PendingSettlementVictoryMenuEntry` (behavior L2417–2425, slot L72) with a session-backed transition record; pump (`TryPumpPendingSettlementTakenMenu` L1794) consumes `ResolveCompletionPlan()` — ownership exactly once, menu exactly once, bounded retries (add a retry cap; currently infinite), fail closed with a player-visible message on cap. Bridge side: `TryOpenSettlementEntryVictoryMenu` (SiegeAiInterventionBehavior L1046–1101) should accept/consult the ledger so a retry after committed ownership skips `ApplySetsSettlementEntryCaptureIfNeeded` (L1190) instead of relying on the "already owned" early-out. Collapse the 4 manual reset paths (L15519 `ResetAftermathRuntimeGuards`, L15564 `ClearActiveState`, L1128 `ClearSetsOwnedSettlementIncidentContext`, local flips L1270/1282) onto dropping/replacing the single session object. Persistence (if implemented): new versioned keys only; missing = no pending capture; never serialize scene objects.

### Slice E — reflection boundary (task #5)

Move all reads/writes of the 7 native fields into one compat adapter file; call sites: `PrepareNativeSettlementTakenMenuContextForSets` L1158–1188, `CaptureNativeSiegeContext` L14917–14951, `TrySetNativePlayerEncounterAftermathForSummary` L14965–14982, `TownColonizationLoadRecovery.cs` L129–158. Add a startup field-existence probe that logs missing fields once. Tighten verifier: reflected names allowed only in the adapter.

### Slice F — resources and cleanup (task #6)

Move touched SETS player-visible Chinese strings to `ModuleData` resources (follow `Gccz*.zh-CN.json` + `Load*Catalog` test pattern); delete dead fields/branches; run conflict-marker / dead-code / duplicate-path / new-CJK searches; update this handoff; final: tests + verifier + unified build; mirror check.

## Key Reconnaissance (line numbers at commit `3a4c66d6`; re-verify after edits)

Behavior (`SettlementEntryTroopSelectionBehavior.cs`): constants L35–66; SyncData L135–144; pump chain L1678–1694; settlement-taken pump L1794–1837; profile limit L2090; hit routing L2915–2965; casualty callback L3018–3045; StartConflict L3201–3238; tick victory check L2903–2912; wave spawn L4941–5027; phase kinds L5076–5103; allied casualty L6244; defender casualty L6260; TAB block L6288–6298; ReachVictory L6305; forced end L6321; queue flow L6352–6397; reserve build L6554–6638; hero filter L6675.

Bridge (`SiegeAiInterventionBehavior.cs`): entry L1046–1101; SETS context prep L1103–1117; owned-incident clear L1128–1147; riot context L1149–1156; native menu context L1158–1188; capture-if-needed L1190–1241; native penalty baseline (-15/-30) L1318–1329; owned penalty L1331–1370; native context capture L14917–14951; reflection helper L14953–14963; guards reset L15519–15562; clear state L15564–15599. Effect adapter: `SiegeAiInterventionBehavior.TownCompletionEffectAdapter.cs` (ownership actions live ONLY here).

## Unchanged Rules From the 2026-08-25 Handoff

Player-facing contract items 1–14, compatibility and save rules, diagnostics format, out-of-scope list, and the in-game test sequence all remain in force. Deployment to the game directory stays out of scope.
