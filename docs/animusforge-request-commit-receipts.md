# Request-level commit receipts

Owner: Conversation/Memory lifecycle. This extends the existing detached commit
framework; it is not another pipeline or a durable Economy transaction manager.

## Replaced path

Previously `InteractionResultCommitter` used a content-based memory-cache key to
infer that actions had succeeded. This ignored trace/runtime/save generation,
could suppress two legitimate identical turns, and forgot failed memory writes
after an action had already happened. Recreating a committer could replay that
action. Executor facts also produced a second, different memory key.

## Current contract

- Request identity: runtime generation, save generation, trace ID, channel,
  session ID, subject ID, and the existing Courier direction snapshot field.
  Native/Scene captures have unique sessions; Courier retains a stable letter
  session and inbound/reply direction. `LOCAL-7-H` adds an internal per-process
  nonce to trace ID so identical restart-local sequence numbers cannot collide.
- Payload fingerprint: append-player flag, player/visible text, ordered action
  tags/targets/parameters/raw plan, and supplied facts. Parameters use ordinal
  key order. Length-prefixed encoding is hashed; cached keys retain no raw text.
- Reserve the request before calling an action or memory owner. Reentry is
  rejected; terminal failure/exception is retained as well as success. A changed
  payload for the same identity is rejected and does not rerun either owner.
- Duplicate results retain the original status and known history/action flags,
  and set `InteractionCommitResult.IsDuplicate`. `DetachedInteractionHost` skips
  `afterCommit` for a duplicate so a recreated Host cannot repeat notifications
  or Courier completion callbacks.
- Memory append keys derive from the same request ID. `MemoryCommitReceiptCache`
  remains only a post-success compatibility diagnostic; it is never consulted
  before the persistent owner validates opaque ID + payload hash + quarantine.
- Rejected-action history is reported as written only when the memory port
  reports an applied/duplicate result. The old Native opt-in public runner also
  treats callback throw/null/failure as terminal, never as permission to replay
  through legacy. Its public signature is retained for existing external users.

The request cache stores at most 512 request receipts. Only completed entries may be
evicted; all-pending capacity rejects new reservations. No game access or owner
callback occurs under its lock. Hashing is once per commit attempt, linear in
payload size. The separate memory recovery tick described below does not execute
through this cache.

## Validation

Run `tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj`
for the existing 40 cases, 69 Host callback cases, 4 old Native callback-failure
cases, and 39 request-receipt cases. The receipt cases include fresh traces and
generations, changed payload/append/facts, memory and executor failures,
non-batch memory, Courier directions, reentry, recreated Hosts and capacity.

After a Debug unified Stage build, run
`tools/ProductionConfiguredHostReplayTests/ProductionConfiguredHostReplayTests.csproj`
for the production 1.4 Host matrix plus 6 recreated-committer cases. The tests
use controlled owner ports and do not mutate Bannerlord objects or saves.

## Memory owner acceptance (LOCAL-7-D)

`MyBehaviorMemoryFacade.Commit` now uses `MyBehavior.CommitExternalDialogueHistory`
instead of inferring success from the public void append APIs. The strict entry
checks the game thread before live lookup, resolves the current Campaign behavior
(never the static Instance fallback), validates identity/eligibility and returns
the existing `MemoryCommitResult`. Only confirmed runtime acceptance enters the
memory receipt cache. The legacy non-batch `Append` contract is unchanged.

The single existing implementation retains player → AFEF → assistant daily writes,
recent-history normalization and the 260-line window. Each daily write confirms
the normalized owner/day and the new draft/line references in the raw owner store
after sanitization; recent history confirms the actual published list. Prompt
reads are deliberately not used for readback because they hide active sessions
and filter history. The four public void APIs remain for active compatibility
callers and share this implementation; no parallel memory pipeline was added.

These checks run only at append/commit boundaries. Readback scans the selected
owner's draft/line lists, already traversed by sanitization, not the world's NPCs;
normal Hero resolution retains direct ID lookup before the existing fallback.
No tick work, queue, new save key/type or persistent receipt was introduced by
`LOCAL-7-D`; `LOCAL-7-H` below is the explicit additive persistence extension.

## Courier Economy reservation (LOCAL-7-E)

The generic Economy-aware executor now accepts an optional channel-owner gate.
Planning, raw/typed plan equality and capability checks remain pure and run
first; when an Economy replay is present, the gate must return `Executed`
before the main-thread Economy port can mutate game state. Native and Scene
owners do not supply this Courier-specific lifecycle gate.

Courier supplies its existing session owner. It re-resolves the current session
and recipient and validates the exact Courier channel/session/subject,
outbound direction, delivered state, non-terminal
state and unconsumed postprocess. Mixed plans are prevalidated before Economy
and continue to the filtered legacy callback. Economy-only plans additionally
set the existing `PostprocessConsumed` flag before Economy replay while leaving
the existing visible/reply fields untouched; raw postprocess is never treated
as reply prose. A different trace or loaded save therefore cannot treat the
session as unconsumed. A rejected/throwing gate never calls Economy.

`PostprocessConsumed` was already part of each CourierSession JSON under
`_af_courier_sessions_v1`; no key, physical SyncData type, parallel receipt or
new save field was added. This is fail-closed, at-most-once owner behavior, not
a disk transaction: a process crash before a later game save can still lose
in-memory state, and a replay failure after reservation remains consumed rather
than being automatically retried.

## Known partial Economy outcomes (LOCAL-7-F)

Hero, Party and Merchant Economy owners now return the appended
`PartiallyApplied` status when at least one planned action was verified and at
least one failed. The enum value is appended, not inserted, so numeric values of
the existing public statuses remain stable. The main-thread port also normalizes
the former `Applied + short count` result for source/binary transition, but
does not promote `Failed/Rejected + positive count` into a trusted outcome.

`IActionPlanExecutionOutcomeReceipt` is an additive optional interface; the
existing receipt interface and six-argument executor constructor remain intact.
It exposes only the Economy owner's verified `AppliedActionCount`, confirmed
facts and structured error. Legacy callbacks still lack per-action receipts and
are never counted merely because they returned `Executed`.

For a known partial, or for full Economy success followed by legacy rejection,
the executor stops with `NonRetryableFailure` and retains Economy facts. The
committer writes the visible exchange plus **only those outcome-owner facts**,
then records `ActionsExecuted=true`. A memory failure keeps that action bit
and the terminal partial error. Duplicate requests return the same receipt
without invoking Economy or memory again. Detached Host does not run
`afterCommit` and cannot select legacy fallback after the commit callback has
started.

This is recovery of truthful evidence, not compensation or success synthesis.
An action helper or domain callback that may mutate and then throw before
incrementing `AppliedCount` is handled by the following `LOCAL-7-G` contract;
known partials still do not make the 512-entry request cache durable across
eviction, restart or save load.

`tools/ProductionOptInEntryReplayTests` includes the production-DLL missing-Campaign
regression (all three channels and repeated attempts), a thread-guard fixture,
public void signature checks and raw-owner publication/sanitizer fixtures. They
do not initialize a Campaign or prove live writes, game scheduling or old saves.

## Unknown-after-start effect state (LOCAL-7-G)

`EconomyRewardDebtReplayStatus.UnknownAfterStart` is appended after the existing
numeric values. `ActionExecutionEffectState` and
`IActionPlanExecutionEffectReceipt` are additive; the old outcome interface,
the executor's six-argument constructor and `InteractionCommitResult`'s sole
public four-argument constructor remain intact.

The main-thread port now treats callback throw/null and malformed post-callback
receipts as unknown, fact-free outcomes. Pre-callback main-thread, target,
capability and plan validation still reject with `NoConfirmedEffect`. A valid
unknown may retain only effects and facts that the owner confirmed before the
uncertain action; count-zero unknowns retain no success facts.

Hero, Party and Merchant owners stop at the first uncertain action. An explicit
`EconomyMutationObservation` carries exceptions that legacy inventory/RP,
fixed-asset and equipment-restore helpers intentionally catch for compatibility.
This includes roster-add failures, RP generation shells, settlement/workshop/
caravan transfer catches, restore-queue failures and rollback exception or
partial/no-op readback. Existing public/internal bool/int helper entry points
still project their original behavior; only the replay-aware path receives the
structured observation. Earlier confirmed count/facts survive, while the
uncertain action creates no fact.

The executor maps Economy, gate and legacy callback uncertainty to
`NonRetryableFailure`. The committer writes the visible exchange once and only
the already-confirmed owner facts. `ActionsExecuted` remains false for a
count-zero unknown; replay prevention comes from the terminal receipt and
request reservation, not from inventing a successful action bit. Memory failure
preserves the unknown terminal state, and duplicate/in-progress/mismatched
requests do not invoke either owner again.

`DetachedInteractionHost` treats the callback's observed receipt as authoritative.
A dispatcher cannot upgrade an actual unknown/rejection with a fake success.
A callback-started throw/null/in-flight return is terminal unknown and never
falls back or runs `afterCommit`; a dispatcher that never starts the callback
cannot claim success and instead takes the still-safe legacy fallback. Late
callbacks are closed, and publication after the dispatcher has already returned
cannot overwrite the host's terminal receipt or fire `afterCommit`.

Offline validation covers Port throw/null/malformed results, gate/owner/legacy
exceptions, known-before-unknown facts, memory success/failure/duplicate,
in-progress/mismatch receipts, dispatcher fake success and all three channels.
Production tests load the final project-local 1.4 DLL. These tests do not inject
faults into live TaleWorlds mutators and are not Campaign/save/load evidence.

## Durable memory-only repair (LOCAL-7-H)

`MyBehaviorMemoryFacade` now always delegates to the distinct internal
`CommitExternalDialogueHistoryRecoverable` owner; the process cache cannot bypass
payload conflict or corrupt/quarantined state. The original public six-parameter
`CommitExternalDialogueHistory` remains unique, avoiding reflection ambiguity,
and all four public void compatibility methods remain unchanged.

The owner projects only visible user/assistant and owner-confirmed facts into a
versioned BCL wire record under `_af_interactionMemoryRecovery_v1`. Raw commit ID,
ActionPlan, postprocess, executor and `afterCommit` are absent. SHA-256 produces an
opaque recovery ID and payload fingerprint; a full-record checksum covers lifecycle,
states, attempts and timestamps. A process nonce in captured trace identity prevents
restart-local session/generation reuse from aliasing an old tombstone.

Each record advances Daily user/fact/assistant, then Recent user/fact/assistant.
Copy-on-write publication includes a hidden marker. On load, matching marker confirms
the step, missing marker makes only that step Pending, and conflicting/missing
already-Applied markers quarantine the receipt. Completed tombstones retain expected
Daily/Recent marker masks; a sealed Daily draft is allowed to retire its marker,
whereas missing Recent evidence cannot return `Duplicate`. Cross-day late writes move
to the current open Daily draft with frozen origin provenance.

The ledger allows 64 pending, 512 oldest-evicted completed tombstones and 64 bounded
quarantine diagnostics. Retry order rotates by last attempt; a component is isolated
after five failures. Load validates schema/checksum/hash/state/size, valid+quarantine
same-ID conflicts, marker ownership and storage caps. non-Hero alias migration retargets
only the projection subject; destroyed-party cleanup includes ledger-only subjects.
Tick uses an O(1) flag and processes at most one memory component; it cannot call an
action owner. Persistence adds one symbolic flattened dictionary key but leaves the
95 literal/121 typed bindings, 99 identity signatures, 35 behaviors and save types
unchanged.

Focused validation: memory contract runner covers six ordered steps, 12 marker-side
faults, restart, long Courier payloads, corruption/capacity/retry/migration and zero
action replay. Production 1.4 reflection replay covers ABI, missing Campaign, marker
rebuild/load reconciliation, trace nonce and Scene provenance. Debug/Release 1.3/1.4/
Bootstrap Stage all build with 0 warning / 0 error; this remains offline evidence.

## Limits and mandatory follow-up

- The request/action guard remains bounded and process-local. The new durable ledger
  covers only Memory/AFEF projection; it is not durable Economy/action idempotency.
  Existing Courier session/consumption flags remain authoritative.
- The legacy void APIs/non-batch `Append` still cannot acknowledge acceptance.
  The batch owner result confirms runtime daily/recent acceptance only, not
  `SyncData`, disk persistence, weekly/notoriety effects or live AFEF acceptance.
  Core Daily/Recent components now have marker-based repair. Weekly material/notoriety
  side effects remain best-effort and may be missing after an interruption; no rollback
  or exactly-once claim is made for those auxiliary effects.
- Courier economy-only now has an offline-verified owner reservation, but live
  save/load and asset evidence remain required. Process-local receipts are still
  not the durable business authority.
- A mixed Economy/legacy plan can partially apply before the later action is
  rejected or becomes unknown. Known Economy facts and the structured effect
  state are retained, but the request-level reservation does not roll back or
  compensate gameplay effects.
- Unknown gameplay effects remain terminal and fact-free for the uncertain action;
  memory repair never compensates or repeats them.
- An `afterCommit` failure is not resumed by the memory ledger. In particular, Courier
  inbound can repair its history yet leave `ReplyGenerated`/session progression stuck.
  A separate Courier-owned persistent completion receipt is the next required slice.

Keep phase 7 at VERIFY and stage 8 destructive cleanup/default cutover blocked
until actual Campaign/Mission, inventory/debt, AFEF and old-save acceptance.
