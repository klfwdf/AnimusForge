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

## Courier inbound durable completion (LOCAL-7-I)

The inbound Courier uses a channel-owned `IInteractionMemoryBatchCommitter` wrapper.
It computes the exact MyBehavior recovery ID and owner payload hash, freezes the
normalized visible letter, and persists an `AFCI1` intent inside the existing
`_af_courier_sessions_v1` session JSON **before** the inner memory owner starts.
The receipt also binds session, direction, sender, current-player recipient and
Courier party. It contains no raw commit ID, ActionPlan, Economy data, postprocess,
executor or callback.

The shared memory ledger exposes only internal prepare/status seams. Status lookup
requires recovery ID, expected subject and expected owner payload hash, so an old
Pending/Completed entry with the same commit ID but a different payload cannot ready
the Courier receipt. `Pending` waits; payload-matched `Completed`, initial `Applied`
or `Duplicate` moves the receipt to `Ready`. Courier then publishes an `Applied`
tombstone, restores `LetterText`, sets `ReplyGenerated=true`, clears
`ReplyGenerationStarted`, and calls the existing `ProcessSessionById` exactly as the
original successful callback did.

Load keeps any nonempty receipt from starting a second LLM. Delivery independently
requires a valid Applied receipt and matching frozen letter. The runtime cursor
handles at most one actionable receipt per Campaign tick; a long-pending entry cannot
starve later receipts. Corrupt, Missing, Disabled, Quarantined, subject/payload/session
mismatch, deterministic pre-owner rejection and commit-without-receipt all terminate
that inbound Courier without delivering its generated letter and release the wait
pause. Outbound `PostprocessConsumed` remains a separate Economy owner flag.

The focused contract dynamically proves arm-before-inner ordering, inner throw,
pending, Applied, Duplicate, payload conflict/mismatch, checksum, 32,768 Unicode
characters and owner status identity. Production reflection covers actual session JSON,
load gating, Applied crash-window repair, fail-closed session gates and one-per-tick
rotation. This is still fixture/offline evidence; live Campaign save/load and Courier
travel remain not run. The Applied receipt is removed with its terminal session; the
absence of that session is the final owner state, not a reusable global tombstone.

## Memory auxiliary recovery boundary (LOCAL-7-J)

The legacy live path and the detached recovery path are not the same call chain.
Legacy still reaches `AppendDailyMemoryLineById`, which performs
`AttachPendingWeeklyMemoryMaterialTriggers → SaveDailyMemoryDraftsById →
NoteConversationLineForExternal`. The detached batch facade enters
`CommitExternalDialogueHistoryRecoverable` directly and publishes equivalent core
Daily/Recent projections through the H journal.

J removes the two auxiliary calls from H's Daily recovery writer. A weekly trigger is
only a process-local pre-action candidate: it has no request/recovery/turn or terminal
effect identity, is cleared on load, and can miss when a sealed origin day is moved to
the current Daily draft. H therefore neither attaches nor consumes that list. This
does not fix the candidate; it prevents the core marker from being misrepresented as
weekly success and avoids deleting a different legacy turn's unidentifiable candidate.

Notoriety is also excluded from recovery. Its public boundary returns void and swallows
failure. The active `LineCount`, session-roll identity and negative outcome are
non-persisted; a positive roll immediately updates aggregate
`KnowsMajorHistory/KnownAtDay`, while finalize later updates session count/bonus/last
day. Neither stage has a per-line/session marker. Replaying from `ExistingPending`,
Duplicate, load or tick could repeat the roll/count/final bonus. To preserve
current-runtime behavior without claiming recovery, only a brand-new `Began` receipt
that completes all core steps in the same call may attempt notoriety. Each
user/assistant component must have an exact published Daily marker matching recovery
ID, owner payload hash and part; AFEF/blank/unpublished components are excluded and each
part is attempted at most once. The attempt is exception-isolated from the already
completed core result and is logged as `attempted_unconfirmed / NOT-RECOVERABLE`.

No new terminal receipt was added because neither missing auxiliary effect currently
has sufficient owner evidence. H schema/seed/components/payload hash/wire, the Courier
`AFCI1` binding, SyncData keys/types and the legacy live path are unchanged. Production
tests load the final Debug 1.4 DLL and provide reflection/decoded-IL structure guards
for call isolation, the `Began` truth table, component eligibility and exact marker
readback. They do not prove live PlayerNotoriety mutation, random-roll behavior, fault
injection or save/load.

## Weekly exact-intent/outcome owner (LOCAL-7-K)

K does not upgrade the legacy process-local weekly candidate. It adds a separate
data-only receipt around the detached action commit. The candidate binds request,
trace, channel, session, subject, runtime/save generation, Courier direction, origin
time/location/session/agent, turn fingerprint, ordered Economy action fingerprint and
candidate hash. Neither the candidate nor persistent receipt stores raw postprocess,
`ActionPlan`, an executor, callback, Hero or Economy owner.

The first implementation is deliberately narrow. Candidate projection is allowed only
for an Economy-only whole ActionPlan that the stateless canonical adapter can project
without exclusions. It never invokes the injected gameplay planner. The latter is
called exactly once by `ValidateAndExecute`; the exact fingerprint of the plan actually
passed to the Economy port is exposed only after owner `Applied` with the full count.
The committer can confirm and publish only when that fingerprint equals the candidate,
the effect is `ConfirmedEffect`, applied count equals the whole ActionPlan count, the
commit is `Executed`, and history is written. Once a candidate is durably Prepared,
known partial, unknown, rejected and post-action memory failure become terminal
non-publishing outcomes. Mixed/legacy plans and unsupported or ineligible payloads are
never armed or published. No subset success is inferred from an aggregate count.

`MyBehavior.WeeklyActionOutcomeReceipts.cs` owns an additive `AFWM1` ledger under the
symbolic flattened key `_af_weeklyActionOutcomeReceipts_v1 : Dictionary<string,string>`.
It is independent of H and Courier `AFCI1`, retains at most 64 Prepared/Confirmed and
512 terminal records, validates full-wire SHA-256 and bounds, and never evicts Confirmed
work. A loaded Prepared record becomes Unknown; only Confirmed may retry the idempotent
data attach. Transition times clamp across machine-clock rollback. Invalid journal input
is preserved verbatim and disables this owner instead of replacing it with an empty
ledger. Because the weekly owner is optional, an invalid journal disables K publication
and its cross-restart duplicate proof rather than redefining the core Economy result;
manual repair is required before that protection is available again.

Before reading live debt/value/foothold state, the owner probes durable receipt ID and
candidate hash. An identical prior receipt returns Duplicate and blocks core replay even
when the live debt is already gone or the foothold changed; a changed candidate for the
same request returns Conflict/quarantine. Only NotFound may freeze a payload. Supported
atoms are gold, numeric/pre-action asset estimates, player-directed debt creation and
pre-action debt resolution; values that cannot be frozen safely are conservative false
negatives. Eligible value must be strictly greater than 20,000 denars.

After core memory success, Confirmed publication adds only non-executable
`[WEEKLY:ECONOMY_*]` labels and five exact outcome digests to the existing Daily JSON.
Full field readback is required before MarkApplied. Sanitization drops malformed semantic
triggers rather than downgrading them to legacy. Focused tests cover the receipt state
matrix, planner single-call/mismatch/throw, durable replay preflight, clock rollback,
load/capacity/corruption, fault isolation and data-only DTO. Production reflection/IL
tests run against the fresh staged DLL and cover sanitizer, call order, lifecycle wiring
and forbidden authority. This remains offline/compiled evidence: live Campaign,
Economy mutation, SyncData round-trip, old saves and AFEF are not proven.

## Notoriety exact line/session owner (LOCAL-7-L)

The previous auxiliary call was a swallowed `void`: observer reads could create and roll
a transient hero-only session before any line; a positive roll mutated persistent known
state immediately, while negative outcome and line count were not saved. Finalize removed
the active session before incrementing sessions/bonus/day. Daily/Recent markers and logs
could not distinguish success, no-op or a swallowed failure.

L preserves the legacy public void ABI but gives the detached H completion a separate
typed owner. Session identity binds subject, opaque memory-session digest and runtime/save
generation; each line binds H recovery ID, payload hash, `user`/`assistant` part and origin
clock. `ProbeLine` executes before active creation or RNG. Same identity/payload returns
Duplicate; changed identity conflicts; capacity failure performs no roll. Raw session key,
dialogue text, H payload, Hero, callback or executable authority is never persisted.

The `AFNR1` ledger is embedded as `Dictionary<string,string>` inside the existing
`_af_player_notoriety_state_v1` JSON, bounded at 64 Open/Confirmed, 512 terminal and 260
line IDs per session. This adds no SyncData key/type. A read-path roll is transient until
an actual exact line is accepted; that owner publication places aggregate known state and
the receipt witness into the same JSON owner value. A zero-line roll no longer increments
completed sessions. Different exact sessions for the same observer finalize separately;
a late finalize must match the active memory session, while prior-day sessions may close
through the bounded stale path. Mixing an exact receipt with a legacy line terminates the
exact receipt as Unknown before returning to legacy behavior.

Session finalize freezes absolute known/known-day/bonus/completed-session/last-day targets,
applies monotonic values, performs readback and then records Applied. Repeating finalize
does not add the delta again. A loaded Open receipt becomes Unknown and only retains exact
line tombstones; it cannot roll or finalize. A loaded Confirmed receipt may reconcile only
its frozen absolute data target. Invalid embedded receipt wires preserve the raw dictionary
and disable the L owner rather than silently claiming success. Corruption of the outer
legacy Notoriety JSON still follows the pre-existing full-state reset behavior and remains
a real-save risk.

Pure tests cover 14 receipt/wire/identity/load/capacity/clock/data-only cases. Fresh compiled
guards verify embedded storage, duplicate-before-roll ordering, exact finalize ordering,
load reconciliation, old ABI and H/K isolation. These are not evidence for live MBRandom,
real ConversationEnded ordering, `IDataStore` atomicity, process crashes or old saves.

## Duel actual-session outcome owner (LOCAL-7-M1)

The legacy Duel action is asynchronous: consuming `[ACTION:DUEL]` can reject,
queue, start, abort, or settle later in one of three Mission paths. Therefore the
detached ActionPlan executor no longer promotes a returned legacy callback to
gameplay success. A delegated Duel returns terminal `UnknownAfterStart` with
`duel.outcome_pending`; any already confirmed Economy subset remains attached,
but no Duel fact is synthesized and the host must not fallback or replay it.

`DuelOutcomeOwner` is a pure, bounded process-local ledger. It binds opaque
request/start/result identities, subject, runtime/save generation, channel/session
tokens, Duel session kind and an action/artifact fingerprint. The live legacy route
uses explicitly labelled `Domain / legacy-unbound` provenance because it still lacks
the detached request ID; this is an actual-session receipt, not proof that a specific
ActionPlan request caused the Duel. The host retains exact DuelId readback plus a
bounded latest-by-subject index. At 512 retained terminal receipts, it rolls to a new
owner only when there are zero active sessions; host-generated serial+nonce DuelIds
are never reused within the process.

Meeting, arena/local and wilderness actual-start paths now call the typed owner. For a
successfully bound session, each writer records `ResultIdentity` immediately after
its local one-shot result guard, before Memory, renown, stake, death or UI, then
finalizes one receipt with explicit component states:
`NotApplicable`, `Confirmed`, `Partial`, `AttemptedUnconfirmed` or `Unknown`.
Renown confirmation uses direct loser/winner readback. Stake uses actual transfer
counts or debt-owner success; swallowed/partial operations are never promoted.
Legacy Memory/AFEF and delayed death remain attempted-unconfirmed. An exception after
the result lock moves the same receipt to `UnknownAfterStart` while retaining the
known result; it never retries the Mission or any non-idempotent side effect.

Pending stake, deferred-debt and after-line artifacts are replaced per exact Duel
reply, fingerprinted, bound once to the actual-start DuelId and consumed only by that
DuelId. Rejected/open/team/queue failures clear unbound artifacts; completed or
unknown sessions clear matching bound leftovers. Plain wager language without the
same reply's exact `[ACTION:DUEL]` tag cannot arm a future Duel.

No Duel receipt is serialized. `_duelCooldowns : Dictionary<string,float>`, legacy
public void methods, Saveable type IDs, MCM JSON and Fourberie optional seams remain
unchanged. `SyncData` load may only mark currently referenced process-local sessions
Unknown and clear those references; load/tick never starts/finalizes a Mission,
transfers assets, kills a Hero or writes Memory. Fresh 1.3/1.4 production IL replay
verifies these routes and ABI, but does not replace live Campaign/Mission, old-save,
Fourberie, death, Economy or AFEF evidence.

If process-local reservation fails, legacy gameplay is not reported as typed success:
unbound artifacts are discarded and exact/latest readback stays absent. The current
opt-in sidecar does not cancel an already opening Mission solely because observation
reservation failed; this fail-closed edge still requires live fault validation.

## Duel exact detached dispatch provenance (LOCAL-7-M2)

M2 preserves the public `IActionPlanExecutor` and legacy constructors, but the committer
now detects an internal request-bound executor seam. After stale validation and request
reservation, it supplies the canonical request ID and canonical ActionPlan fingerprint;
the executor recomputes both values and rejects any mismatch before Queue, Economy or a
legacy callback. A data-only `DetachedDuelDispatchContext` binds request, trace, channel,
session/direction, subject, runtime/save generation and fingerprint to one deterministic
process-local DuelId.

The exact owner publishes Queue before Economy/gameplay and returns one typed state:
`Rejected`, `Queued`, `Started` or `UnknownAfterStart`. These states terminate the current
Interaction commit; the underlying owner may still advance the same DuelId without rewriting
that commit. `UnknownAfterStart` is a conservative umbrella and may have no StartIdentity.
Native and Scene carry the same
context through immediate or delayed meeting/arena/wilderness holders to actual start.
Courier has no production `PrepareDuel` owner and is explicitly rejected. Delayed consumers
require both `Queued` and `HostAccepted`; holder publication precedes acceptance. Economy
rejection/throw cancels an unstarted Queue. Duplicate, changed payload, invalid binding,
generation conflict and full process-lifetime 4096-entry exact-ID seen capacity fail closed.
The outcome owner separately retains 64 active / 512 receipts. An accepted, started or
uncertain dispatch is non-retryable for the current commit and never falls back or replays.

`Duel+Mood` remains a supported companion shape, but if the companion effect could already
have occurred its effect is reported as unknown rather than `NoConfirmedEffect`. A second
independent gameplay action, multiple Duel actions, bogus binding or Courier direction are
rejected before side effects. Commit exceptions and request conflicts retain the typed Duel
receipt instead of replacing provenance with a generic failure.

The context contains no game object, callback, raw reply or replay authority and is not
serialized. Load clears all pending exact holders and marks active receipts unknown without
opening/ending a Mission, transferring stake, killing a Hero or writing Memory. Three Duel
settlement paths must first record the matching result receipt before any result-linked
Economy/Memory/death work. Exact artifacts are discarded by DuelId; the old pending-meeting
locks remain only for `context == null`, preserving M1/default gameplay.

M2 offline/compiled evidence is `LOCAL_PASS`: Duel Dispatch 16/16, Duel Outcome 18/18 and
fresh Debug/Release production replay 35/35 with 1.3/1.4 parity. This is not live Campaign,
Mission, old-save, stake, death, Fourberie, Economy or AFEF evidence; phase 7 remains VERIFY.

## Limits and mandatory follow-up

- The generic request/action guard remains bounded and process-local. H's durable ledger
  covers only Memory/AFEF projection. K is not a general Economy transaction journal,
  rollback or compensation manager; for an eligible Economy-only whole plan and a valid
  K journal, its exact receipt does provide a cross-restart Duplicate/Conflict replay
  guard plus idempotent data attach. Existing Courier session/consumption flags remain
  authoritative for their separate channel lifecycle.
- The legacy void APIs/non-batch `Append` still cannot acknowledge acceptance.
  The batch owner result confirms runtime daily/recent acceptance only, not
  `SyncData`, disk persistence or live AFEF acceptance. Core Daily/Recent components now
  have marker-based repair. H never replays weekly or notoriety. K adds an independent
  exact weekly owner only for Economy-only whole-plan success; it does not consume the
  legacy pre-action candidate, recover old records, cover mixed/legacy/subset actions or
  prove live save/load. L adds an exact receipt only for detached lines carrying H
  recovery/session identity. Legacy default lines, missing L receipts and loaded Open
  sessions remain non-recoverable; no marker or aggregate value is promoted into success.
- Courier economy-only now has an offline-verified owner reservation, but live
  save/load and asset evidence remain required. Process-local receipts are still
  not the durable business authority.
- A mixed Economy/legacy plan can partially apply before the later action is
  rejected or becomes unknown. Known Economy facts and the structured effect
  state are retained, but the request-level reservation does not roll back or
  compensate gameplay effects.
- Unknown gameplay effects remain terminal and fact-free for the uncertain action;
  memory repair never compensates or repeats them.
- Exact Duel dispatch receipts are also bounded process-local metadata. They link a detached
  request to one DuelId but do not prove final gameplay outcome, durable persistence or live
  side effects, and they never authorize replay after load/restart.
- The memory ledger still does not resume arbitrary `afterCommit` callbacks. Courier
  inbound is the one explicit channel-owned recovery implemented in I; other channels
  need their own durable owner before any similar completion can be claimed.
- Saves created after H but before I may contain pending memory without an `AFCI1`
  Courier receipt. The original visible reply was never persisted, so I cannot safely
  reconstruct it; those intermediate saves retain the legacy regeneration risk.

Keep phase 7 at VERIFY and stage 8 destructive cleanup/default cutover blocked
until actual Campaign/Mission, inventory/debt, AFEF and old-save acceptance.
