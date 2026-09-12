# Runtime safety, lifecycle and diagnostics

Use this reference for Harmony/reflection, Bannerlord events, application ticks, Mission/Campaign lifecycles, UI, async/background work, TTS/HTTP, module health, failure isolation and logging.

## Runtime domains

Do not flatten DSH Host/Client concepts onto Bannerlord. Distinguish:

```text
Bootstrap/process startup
Campaign/session lifecycle
Mission lifecycle
Main game thread
Gauntlet/UI interaction
Background worker/HTTP/TTS
External bridge/process
Save/load boundary
```

Each module manifest declares which domains it touches.

## Main-thread rule

Read/mutate TaleWorlds objects, Campaign/Mission collections, UI state and game actions on the game/main thread unless exact current API documentation and project evidence prove otherwise.

Background operations receive detached immutable snapshots. Their completion returns a DTO and generation token. On the main thread:

1. reject stale save/session/interaction generation;
2. re-resolve IDs;
3. re-check target/state/permission;
4. execute once;
5. publish facts/notifications after success.

### Trace the whole asynchronous chain

A method named `OnMainThread` is not a thread witness. Follow `Task.Run`, each await/retry/wave, callbacks and the actual dispatcher. Returning writes to the main thread does not make earlier background reads of Hero/session/owner dictionaries safe.

Capture detached, immutable inputs on the owning thread. Besides owner/save/session generation, bind a source revision or fingerprint whenever the input can change within the same generation (for example, summary blocks or a draft/cursor). On acceptance revalidate the relevant source as well as the current owner. Define rejection, merge or requeue semantics explicitly; stale-result protection must not silently discard new authoritative data or duplicate effects.

Tests should force a genuine asynchronous yield, source mutation during retry, owner replacement and load/reset. State exactly which captured inputs and commit paths are covered; a safe post-await commit is not proof that the entire worker is network-only or that all channels are thread-safe.

## Module lifecycle ownership

Every module owns:

- cancellation source/generation;
- event/listener registrations;
- scheduler task handles;
- service/capability registrations;
- UI contributions;
- background workers/queues;
- health/invariant report;
- Harmony patches it declares.

The foundation can dispose reversible registrations. Module docs must name non-reversible or restart-required effects.

Start is transactional for reversible contributions. A partial start failure disposes started contributions before state becomes `Failed`.

## Harmony and reflection

Business modules should not scatter `AccessTools`, `GetMethod`, `GetField` and private-member fallback logic.

- GameAdapter/Compatibility owns stable ports and reflection caches.
- Every patch has module owner, ID, API-line target/signature, lifecycle class, conflict policy and degradation behavior.
- Resolve/cache reflection outside hot paths.
- Log an important missing member once with version/module/feature context.
- Do not rely on patch application order as an undocumented arbitration system.
- Profile resolution should detect known exclusive/conflicting hooks before campaign load.
- Never promise safe runtime unpatch unless focused lifecycle and in-game tests prove it.

## Tick scheduler

Every task declares:

```text
Task ID/name
Module/bridge owner
Runtime domain/main-thread requirement
Frequency/minimum interval
Maximum work items/time budget
Queue/backpressure bound
Stale/cancel behavior
Save/load/mission transition behavior
Metric/trace name
```

Prefer Bannerlord/Campaign events over polling. Measure before changing frequencies. Avoid full-world/hero/party scans, repeated reflection, repeated JSON parsing, unbounded allocations/queues and lock contention in hot paths.

### Budget the real work, not only envelopes

- Count jobs/records/effects executed inside each queued callback and bound elapsed work as appropriate. A limit of two callbacks is not a two-job limit when one callback loops over every accumulated result.
- Trace upstream batching: provider RPM or a delay between network waves limits request rate, not the size of the later main-thread commit. A scan/enqueue budget does not bound a loaded backlog or its total completion work.
- Specify queue/backpressure limits separately from per-tick drain limits. When a batch must remain atomic, define its maximum size and measured worst-case cost; do not silently split a transaction to satisfy a counter.
- For resumable work, retain a cursor and revalidate owner/generation/source at safe chunk boundaries. Preserve ordering and completion semantics; cancellation cannot undo already executed effects.
- Validate a large backlog, one callback containing many jobs, load/restored queues, and unrelated work progressing. A test of only the dequeue count cannot establish a frame budget. Report an unmeasured performance exposure as such, not as a reproduced freeze.

## Error policy

| Boundary | Policy |
| --- | --- |
| Module start/stop/health | Catch at host boundary, clean reversible effects, set structured state/failure, block dependents, keep unrelated modules. |
| Harmony/GameAdapter | Bounded diagnostic; disable/degrade owning feature when safe; do not fake successful behavior. |
| Pure parser/domain logic | Typed result or precise exception; tests cover invalid input. |
| Save/migration | Never silently lose data; include namespace/key/schema; preserve/export on failure. |
| Optional UI/diagnostic | Degrade with rate-limited message. |
| Action execution | Reject invalid target/state; record no success AFEF. |
| Background operation | Cancellation/stale is distinct from failure; no off-thread game write. |

An empty catch must identify the exact best-effort operation and why no other channel can receive the failure. Keep the try block narrow.

## Module health and inventory

Health is not “method exists.” Check authoritative relationships:

- registered capability belongs to active module generation;
- scheduler tasks are owned by active module;
- declared required provider is present and compatible;
- no orphan listener/queue remains after safe toggle;
- persistence namespace/schema matches manifest;
- patch conflict/target status is known;
- runtime queue/budget/stale counters remain within defined bounds.

State includes `Discovered`, `Disabled`, `Blocked`, `Starting`, `Active`, `Degraded`, `Failed`, and `RestartRequired`.

## Diagnostics

Use one AF diagnostics service and normal mod log ownership, with per-module category/trace—not one unbounded file per module.

Record bounded fields:

```text
traceId
moduleId / bridgeId / run generation
profile / API line / save generation
stage and elapsed time
state transition
capability/provider selection and fallback reason
queue depth/dropped count
error code/summary
```

Do not log API keys, unrestricted player conversations/prompts/model responses, private user paths or complete save payloads by default.

Rate-limit repeated compatibility/tick/UI failures. Preserve the first full stack and aggregate repetitions.

## SafeMode

SafeMode is a recovery profile, not a universal repair engine. It must preserve unknown module data, report failed/disabled modules and avoid optional gameplay. Any destructive repair requires a separate explicit action, backup and owner-specific migration.
