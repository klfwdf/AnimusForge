# AF SETS 核心重做与影子接线 Progress Handoff

Date: 2026-08-28 (second session of the day)
Supersedes progress tracking in: `af-sets-noble-full-integration-handoff-20260828.md` (contract, architecture, and remaining-work sections there stay authoritative for the un-started items; this document records what actually landed since).
Related: `sets-urban-capture-refactor-handoff-20260825.md`, `sets-urban-capture-refactor-progress-20260826.md` (superseded core design — the 2026-08-26 `SetsUrbanCaptureContext`/`Policy`/`CompletionPlan` shapes described there were replaced in this session per the 2026-08-28 defect list).

## User Directive That Scoped This Session

"SETS 是完全不影响其他功能的，只有玩家在城内进攻的时候士兵会帮忙攻击。满足这个就行，然后你做好一定方便以后重构爆改。"

Translation of intent: do not build the full AF `AfMissionScope`/`AfSceneParticipantRegistry` cross-feature machine from the 2026-08-28 handoff's section 7 right now. SETS must stay a self-contained, zero-blast-radius feature (own the "followers help fight inside a captured town/castle" behavior only) while the internal capture core gets fixed and left easy to swap wholesale later. The noble-escort integration (handoff sections 9–12) was **not** started this session — out of scope per this directive until asked for.

## What Changed This Session (chronological)

### 1. Disk space failure recovered

Mid-session, C: dropped to 2.9 GB free (`No space left on device` on `git status`). Root cause: ~3.4 GB of stale temp data, mostly 10 leftover `codex-review-objects-*` directories (541 MB largest) and installer temp files unrelated to this repo. Deleted via `Remove-Item -Recurse -Force` on `C:\Users\28358\AppData\Local\Temp\codex-review-objects-*` plus a few named `.tmp`/random-named leftover dirs. C: now sits around 4.2 GB free — enough to build, but still tight; **flag to the user if it drops again**.

### 2. Backup tag before touching anything

`backup/pre-sets-noble-integration-20260828` created at GCCZ `d02f9cb` and NEW-10 `85cd4810` (the commits that carried the 2026-08-28 handoff itself). Earlier tag `backup/pre-sets-refactor-20260825` left untouched.

### 3. Protected pre-existing dirty work before rewriting the core

Both repos had uncommitted voice-session work (`TownOrdinarySpeakerVoiceSession.cs`, prompt catalog/composer edits, `AfGcczShoutBridge.cs`, `ShoutBehavior.cs`, `SiegeAiInterventionBehavior.cs` changes) unrelated to SETS. Committed as-is before any SETS edit, per the "no destructive shortcuts" rule:

- GCCZ `aa74301` — voice-session standalone additions
- NEW-10 `993d414d` — voice-session runtime wiring

### 4. SETS capture core rewritten to close S-01–S-06 and S-09

The 2026-08-26 core (`SetsUrbanCaptureContext`/`Policy`/`CompletionPlan`/`Session`, with an `IncidentTriggered`/owned branch baked into the same state machine) was replaced — not patched — because the 2026-08-28 handoff's defect table (section 5.1) named exactly the flaws in that shape: the transition table didn't consult context, the owned branch shared states with the hostile branch, ownership eligibility didn't require a committed victory, and the completion plan could return multiple simultaneous actions.

New shape (all in `GCCZ\src\AnimusForge.SiegeAftermathIntervention\`, mirrored byte-for-byte to `NEW-10\AnimusForge.SiegeAftermathIntervention\`):

| File | What it is now |
|---|---|
| `SetsUrbanCaptureState.cs` | 9 states, hostile-only: `Inactive → EntryPrepared → MissionActive → ConflictActive → VictoryReached → AwaitingMap → OwnershipCommitted → NativeMenuOpened → Completed`, plus terminal `Suspended`. No `IncidentTriggered`/owned-menu states — removed entirely (S-02). |
| `SetsUrbanCaptureContext.cs` | Sealed, built only via `TryCreateHostile(...)`, which returns **null** for anything not a hostile town/castle (S-01): villages, unknown scenes, empty ids, or a target the player already owns. Carries `PlayerClanId`. `ResolveRecovery(...)` implements the exact 5-row table from handoff §8.2 (S-05): missing settlement → Abandon; unchanged owner → Continue; player owns it + victory committed → ContinueOwnershipAlreadyApplied; player-owned without committed victory, third-party owner, or missing/changed player clan → Suspend. |
| `SetsUrbanCapturePolicy.cs` | `IsLegalTransition(context, from, event, ledger)` now takes context+ledger, not state alone (S-03/S-04): `CommitOwnership` requires `AwaitingMap` **and** `ledger.VictoryCommitted`; `OpenNativeMenu` requires `OwnershipCommitted` **and** `ledger.OwnershipCommitted`; `Complete` requires `ledger.MenuCommitted`; `EndMission` reaches `AwaitingMap` only with a committed victory ledger (a quiet visit with no conflict returns to `Inactive` instead). `Abort` is legal only pre-victory-commit. New `IsRestoredCombinationValid(state, ledger)` rejects every impossible pairing (S-06) — e.g. `OwnershipCommitted` state without `ledger.OwnershipCommitted`. |
| `SetsUrbanCaptureCompletionPlan.cs` | Renamed concept: no longer a multi-flag "plan" object. `SetsUrbanCaptureCompletionPlanner.ResolveNextAction(...)` returns exactly **one** `SetsUrbanCaptureNextAction` (`CommitOwnership → PrepareNativeAftermathContext → OpenNativeMenu → Complete`, or `Suspend` on an illegal restored combination) — S-04. New `SetsUrbanCaptureActionOutcome` enum (`Succeeded / AlreadyApplied / Retryable / Failed`) and `ResolveEventForOutcome(...)` map a structured runtime result to the next event; `Retryable` advances nothing, `Failed` maps to `Suspend`. `MaxRetriesPerAction = 5` with `ShouldSuspendAfterRetry(...)` bounds retries (S-09). |
| `SetsUrbanCaptureSession.cs` | Aggregate unchanged in spirit, updated to the new API: `TryApply` now delegates every check to `Policy.IsLegalTransition` (context+ledger aware); `ResolveNextAction`, `MarkNativeContextPrepared`, `RecordRetryableFailure`, `RestoreFromRecord` (forces `Suspended` on an illegal restored combination — S-06) are new. `IsSuspended` property added. |
| `SetsUrbanCaptureLedger.cs` | Unchanged from 2026-08-26 (still correct): per-agent once-only casualty gates, per-phase reserve-withdrawal gate, five commit flags (`victory/ownership/menu/villageReward/completion`), `RestoreCommittedStages` never resurrects mission-scoped agent indexes. |
| `SetsUrbanCaptureContractProfile.cs` | Unchanged (Slice A numerical anchors: 100/10 limits, wave/timing constants, save keys, phase order). |

`SetsUrbanCaptureCompletionPlan.DoNothing` / `.Resolve(...)` from 2026-08-26 no longer exist — replaced by the planner's `ResolveNextAction`. Anything referencing the old shape (there was nothing outside tests) needed updating.

Tests: the three SETS suites in `Program.cs` (`TestSetsUrbanCaptureStateMachine`, `TestSetsUrbanCaptureLedger`, `TestSetsUrbanCaptureCompletionPlan`) were rewritten around the S-01…S-09 scenarios by name (search the test file for the `S-0x:` comments). ~95 assertions, all passing.

Commits:
- GCCZ `cce9899` — core rewrite + rewritten tests
- NEW-10 `224b53ab` — byte-identical mirror (SHA256-verified per file at commit time)

### 5. Shadow-mode wiring in the fused runtime (handoff §20 actions 7–9, narrowed)

Per the user's scoping directive, this did **not** build `AfMissionScope`/`AfSceneParticipantRegistry`. Instead, `SettlementEntryTroopSelectionMissionLogic` (`NEW-10\SettlementEntryTroopSelectionBehavior.cs`) now carries one `_shadowCaptureSession` field:

- `CreateShadowCaptureSession(entry)` (called from the constructor) returns **null** unless `_defenderConflictEnabled` and the scene is Town or Castle — owned settlements, villages, and unsupported scenes are structurally untouched. On success it calls `SetsUrbanCaptureContext.TryCreateHostile(...)` (operation id = `settlementId@hoursSinceCampaignStart-TickCount`) then applies `PrepareEntry` + `StartMission`.
- `ShadowApply(event, legacyAllowed, site)` — a small helper that applies an event to the shadow session and logs `SETS shadow DIVERGENCE at {site}: legacy={x}, shadow={y}, {session.DescribeForLog()}` whenever the shadow's `TryApply` result disagrees with the legacy boolean decision at that call site. Wired at:
  - `StartConflict` (right after `_conflictActive = true`)
  - `ReachVictory` (right after `_victoryReached = true`; also commits `Ledger.TryCommitVictory()`)
  - `OnEndMission` (first line)
- `CompareShadowExitBlock(legacyBlocked)` — wired into `OnEndMissionRequest`; computes `SetsUrbanCapturePolicy.ShouldBlockExit(session.State, liveEnemies, reserveExhausted)` and logs a divergence line if it disagrees with the legacy TAB-block boolean.

**Every shadow call is wrapped in try/catch and never returns a value the legacy path consults.** If the shadow session throws, is null, or disagrees, the legacy boolean flow is what the player experiences — unchanged. This is the actual mechanism behind "SETS 完全不影响其他功能": the shadow code is additive-only, and the four wiring points are the only places it was touched.

Build-time bug found and fixed: `Campaign.Current.CampaignStartTime` does not exist in this Bannerlord API surface (`CS1061`). Replaced with `CampaignTime.Now.ToHours` for the operation-id timestamp component — cosmetic, only affects the log-friendly id string, not any decision.

Commit: NEW-10 `5cbb440f`.

### 6. Verifier kept in sync

The TAB-exit-block assertion in `verify_gccz_town_refactor.ps1` (added in the original Slice A commit) pinned the literal legacy `if (...)` condition text. Wiring the shadow compare required hoisting that condition into a local (`bool legacyBlocked = ...`) so it could be reused in both the legacy branch and the shadow comparison call. Updated the verifier's string match to the hoisted form — same boolean expression, same semantics, just no longer inline in an `if`.

Commit: GCCZ `7415c9f`.

## Commits This Session, In Order

| # | Repo | Commit | Content |
|---|---|---|---|
| 1 | GCCZ | `aa74301` | Protect pre-existing voice-session standalone work |
| 2 | GCCZ | `cce9899` | SETS core rewrite closing S-01–S-06, S-09 + rewritten tests |
| 3 | NEW-10 | `993d414d` | Protect pre-existing voice-session runtime work |
| 4 | NEW-10 | `224b53ab` | Mirror of commit 2 (SHA256-verified) |
| 5 | NEW-10 | `5cbb440f` | Shadow-mode wiring (additive only) |
| 6 | GCCZ | `7415c9f` | Verifier follows the exit-block condition's shadow-compare hoist |

Two unrelated commits from other sessions landed interleaved in NEW-10's history during this work (`9da59e03` add + `b6faeba0`/`f1acdd83`/`8b104416` revert-and-scaffold-and-revert-again of an "expedition parade" experiment) and one in GCCZ (`f355939`, native settlement population constants). None touch SETS files; verified by re-checking SHA256 of all 7 core files across repos after the fact — still byte-identical.

## Verified State As Of This Handoff

- Both repos' working trees are clean (`git status --short` empty in both).
- GCCZ standalone tests: `dotnet run --project G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\...csproj` → all pass.
- Verifier: `powershell -File G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1` → pass, including the SETS contract section.
- NEW-10 unified build (Bootstrap + 1.3 + 1.4, stage-only, game directory untouched): pass. Recipe below.
- 7 SETS core files: SHA256-identical between `GCCZ\src\AnimusForge.SiegeAftermathIntervention\` and `NEW-10\AnimusForge.SiegeAftermathIntervention\`.
- **Not yet done**: an actual in-game run. The shadow log has never been observed against a live mission. This is the very next required step before any legacy-boolean deletion.

## Build Recipe (unchanged from 2026-08-26, repeated for convenience)

Game is at `E:\Steam\steamapps\common\Mount & Blade II Bannerlord` (v1.4.7), workshop content at `E:\Steam\steamapps\workshop\content\261550`. Network stays restricted (github.com/nuget.org unreachable).

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

Quick 1.4-only compile check (faster than the full unified script when iterating):
```
dotnet msbuild AnimusForge.csproj -p:Configuration=Debug -p:BannerlordApi=1.4 -p:"BannerlordRoot=E:\Steam\steamapps\common\Mount & Blade II Bannerlord" -p:"WorkshopContentDir=E:\Steam\steamapps\workshop\content\261550" -p:BaseIntermediateOutputPath=obj/codex_gccz_14/
```

Standalone tests: `G:\AFMOD\.dotnet-sdk\dotnet.exe run --project G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\...csproj` with `DOTNET_CLI_HOME=G:\AFMOD\.dotnet-home`.

Verifier: `powershell -NoProfile -ExecutionPolicy Bypass -File G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1`.

In Git Bash: always write `-p:X`, never `/p:X` (path-mangled). Use `git -C <repo>` rather than `cd repo && git ...` compounds.

## Next Steps, In Order

1. **In-game shadow run.** Enter a hostile town or castle with a handful of followers configured, start the conflict, defeat the objective, exit to the map, enter GCCZ, and check `SETS.log` for any `SETS shadow DIVERGENCE` line. Do this at least twice (town + castle) and once with zero followers configured (should stay in `MissionActive` shadow-side, matching the legacy no-conflict quiet exit).
2. **If clean**, switch the four wired decisions to read the shadow session's result instead of just comparing against it, and delete the now-redundant legacy booleans (`_conflictActive`'s decision-making role, not necessarily the field itself if other code still reads it — check call sites first). Re-run tests, verifier, unified build after every deletion, not at the end.
3. **If divergent**, the log line names the site, both booleans, and the full session state — fix the state machine or the wiring point, not the legacy code (the legacy code is the known-correct baseline being replaced).
4. **Ownership commit and native menu bridging** (handoff §8.5–8.7, S-07/S-08) still needs the actual runtime wiring: today `TryOpenSettlementEntryVictoryMenu` and `ApplySetsSettlementEntryCaptureIfNeeded` in `SiegeAiInterventionBehavior.cs` are untouched by this session's work. The completion planner exists and is tested standalone, but nothing calls `ResolveNextAction`/`RecordRetryableFailure` from the pump chain yet (`TryPumpPendingSettlementTakenMenu`, L1794). That wiring is the next real behavior change, gated on step 1's clean shadow run.
5. **Noble escort integration (handoff §9) is still fully unstarted.** Per the user's directive this session, do not begin it unless explicitly asked — SETS's own core correctness was the requested scope.
6. Slices C (mission-logic extraction), E (reflection boundary adapter), F (resource/cleanup) from the 2026-08-25/26 handoffs remain queued behind the shadow-run gate above.

## Explicitly Out of Scope This Session (per user directive)

- `AfMissionScope`, `AfSceneParticipantRegistry`, `AfSpawnLease`, `AfParticipantActionLease` (handoff §7) — not built.
- Noble captive escort work (handoff §9) — not touched.
- Harmony dispatcher unification (handoff §11), shared dialogue/command UI (handoff §12) — not touched.
- Any actual behavior switch-over — the session is shadow-only; legacy code still drives 100% of live behavior.
