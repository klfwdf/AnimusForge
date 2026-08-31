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
  Existing capture identity is unchanged: Native/Scene captures have unique
  sessions; Courier retains a stable letter session and inbound/reply direction.
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
  remains the memory facade's separate append guard; it no longer decides
  whether game actions happened.
- Rejected-action history is reported as written only when the memory port
  reports an applied/duplicate result. The old Native opt-in public runner also
  treats callback throw/null/failure as terminal, never as permission to replay
  through legacy. Its public signature is retained for existing external users.

The cache stores at most 512 request receipts. Only completed entries may be
evicted; all-pending capacity rejects new reservations. No game access or owner
callback occurs under its lock. Hashing is once per commit attempt, linear in
payload size; there is no new tick work, scan, background queue or save state.

## Validation

Run `tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj`
for the existing 40 cases, 48 Host callback cases, 4 old Native callback-failure
cases, and 38 request-receipt cases. The receipt cases include fresh traces and
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
No tick work, queue, new save key/type or persistent receipt was introduced.

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

`tools/ProductionOptInEntryReplayTests` includes the production-DLL missing-Campaign
regression (all three channels and repeated attempts), a thread-guard fixture,
public void signature checks and raw-owner publication/sanitizer fixtures. They
do not initialize a Campaign or prove live writes, game scheduling or old saves.

## Limits and mandatory follow-up

- This is a bounded process-local replay guard. Eviction, process restart or
  save generation change is not durable business idempotency. Existing Courier
  session/consumption flags remain authoritative and need separate validation.
- The legacy void APIs/non-batch `Append` still cannot acknowledge acceptance.
  The batch owner result confirms runtime daily/recent acceptance only, not
  `SyncData`, disk persistence, weekly/notoriety effects or live AFEF acceptance.
  Owner writes can partially mutate lists and consume pending material triggers
  before failure. No rollback or safe automatic retry is implied. Scene session
  forwarding in the detached facade remains a separate follow-up (currently -1).
- Courier economy-only now has an offline-verified owner reservation, but live
  save/load and asset evidence remain required. Process-local receipts are still
  not the durable business authority.
- A mixed Economy/legacy plan can partially apply before the later action is
  rejected. The request-level reservation prevents immediate replay, but does
  not roll back the earlier transfer or recover discarded confirmed facts.
- There is no automatic memory-only retry or compensation: an unconfirmed
  owner result can still mean a partial append. Unknown effects remain
  failed, not fabricated as successful facts.
- An `afterCommit` failure is not automatically resumed on a duplicate request.
  The retained commit receipt describes action/history, not completion of every
  notification or delivery hook; hook recovery remains the channel owner's job.

Keep phase 7 at VERIFY and stage 8 destructive cleanup/default cutover blocked
until actual Campaign/Mission, inventory/debt, AFEF and old-save acceptance.
