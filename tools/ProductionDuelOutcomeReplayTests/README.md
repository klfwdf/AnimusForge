# ProductionDuelOutcomeReplayTests

This isolated runner inspects both freshly staged production implementations:

- `versions/1.3/AnimusForge.dll`
- `versions/1.4/AnimusForge.dll`

It uses CLR metadata and decoded IL, so the two assemblies with the same simple
name can be audited in one process without loading Bannerlord or deploying a
module. The adjacent schema-v2 build markers must match each DLL hash, API line,
flavor, reference version, and the timestamps of the relevant production source.

The replay locks:

- the internal typed Duel outcome types, enum values, factories, bounded owner,
  and owner transition ABI;
- all three public `PrepareDuel(..., float)` `void` overloads, both public
  `StartDuelViaAI(...)` `void` overloads, and
  `TryConsumeLastDuelResult(Hero, out bool)`;
- `_duelCooldowns` as `Dictionary<string,float>`, its exact SyncData key,
  SaveableTypeDefiner base ID `711070`, and class ID `1`;
- the process-local `_duelOutcomeOwner`; SyncData load may only mark the two
  active identities `UnknownAfterStart` and clear them, never begin/finalize/read
  or persist/replay the owner;
- the internal exact-ID readback plus a process-local per-subject latest-result
  index bounded to 256 subjects and 512 order records; neither enters SyncData;
- `TryBeginDuelOutcome`, `TryFinalizeDuelOutcome`,
  `MarkDuelOutcomeUnknown`, and `TryReadDuelOutcome` routing to the typed owner;
- all three terminal writers locking `TryRecordDuelOutcome` before effects,
  reaching the same `TryFinalizeDuelOutcome` seam after effect-state capture,
  and delaying `FinishDuel` until typed settlement;
- safe owner retention rollover only when `ActiveCount == 0`, under a dedicated
  lock, with the process-local subject index cleared before retry;
- the Fourberie start guard, wilderness-open window, callback patch seam, and
  startup registration;
- the exact `[ACTION:DUEL]` same-response gate before Duel stake parsing can arm
  the bounded pending-stake cache;
- stake, debt, and after-lines fingerprints/binding/consumption by the exact
  `DuelOutcomeId`, including stale-reply replacement and both debt normalizers'
  clear-before-cache order;
- equivalent audited surfaces in the 1.3 and 1.4 production DLLs.

## Fresh production run

The normal entry point first calls the repository's official unified Stage build
with `-Stage` only, then runs the audit. It never deploys to the game directory.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  .\tools\ProductionDuelOutcomeReplayTests\Invoke-ProductionDuelOutcomeReplay.ps1
```

All build paths are parameters. The defaults match the current G-drive
workspace; pass explicit values on another machine.

## Intentional red-stage inspection

To inspect an already staged build without rebuilding it:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  .\tools\ProductionDuelOutcomeReplayTests\Invoke-ProductionDuelOutcomeReplay.ps1 `
  -SkipStageBuild
```

`-SkipStageBuild` does not disable freshness checks. A stale pre-seam DLL is
expected to report `stage.freshness`, missing typed types/host fields, and missing
terminal-writer routes as explicit red signals.

This is a production-binary contract replay, not a live Campaign/Mission test.
It does not start Bannerlord, mutate a save, execute Duel effects, or prove
in-game Fourberie behavior.

## Captured local evidence

The latest complete local transcripts are kept separate from source under:

- `artifacts/debug-production-replay.log`
- `artifacts/release-production-replay.log`

They record the audited DLL SHA-256 and MVID values. Re-run the replay after any
production-source or Stage change; an older transcript never overrides the
freshness/hash checks performed against the current Stage.
