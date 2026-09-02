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
- equivalent audited surfaces in the 1.3 and 1.4 production DLLs;
- the M2 internal request-bound executor seam, deterministic exact DuelId,
  request/action fingerprint, pre-Economy Queue, explicit delayed holders,
  request-ID readback, Native/Scene routing, Courier rejection, duplicate
  suppression, four-state dispatch receipts and continued `legacy-unbound`
  isolation;
- `UnknownAfterDispatch` for a crossed side-effect boundary without an actual
  session identity, conservative Duel+Mood handling, and exact artifact cleanup;
- independent actual-target subject checks, immutable non-hero/action holders,
  Native/Scene provenance gates before Economy, and Courier session preflight;
- load cleanup of every queued/meeting/opening/runtime trigger, owner Start before
  Arena/Wilderness/in-place gameplay mutation, and bounded opening/setup timeouts;
- delayed consumption only from `Queued && HostAccepted`, direct actual start for
  Hero/non-hero targets already in the arena, and bounded Reject tombstones;
- successful typed Record as a prerequisite for settlement effects, load/runtime
  abort-state clearing, 30-second wilderness participant acquisition, and hard
  Arena/Wilderness no-settlement guards after abort.

## Fresh production run

The normal entry point first calls the repository's official unified Stage build
with `-Stage` only, then runs the audit. It never deploys to the game directory.
When rebuilding, pass the actual Bannerlord installation root explicitly (or set
`BANNERLORD_ROOT`); the wrapper has no machine-specific game or `NEW-10` default.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  .\tools\ProductionDuelOutcomeReplayTests\Invoke-ProductionDuelOutcomeReplay.ps1 `
  -BannerlordRoot "<Bannerlord root>"
```

All build paths are parameters. `-WorkshopContentDir` is optional (or can be supplied
through `WORKSHOP_CONTENT_DIR`). `-RuntimeDependencyDir` is optional: when omitted,
the official unified build script resolves and validates the private runtime DLL
set from the source or installed unified module. Pass it explicitly only when a
separate, complete dependency directory is intended.

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
