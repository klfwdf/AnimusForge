# Town Rule Memory AF Bridge

## Scope and ownership

Each town retains up to three ruler tenures. The reusable `SettlementRuleMemoryStore`
owns immutable records, confirmed facts, revision checks and refresh cadence. AF adapters
supply live ruler/culture/personality, post-commit events, auxiliary generation and primitive save data.
The encyclopedia and eligible town dialogue consumers read the same record. Existing dialogue
speaker/channel eligibility remains unchanged; this is separate from personal NPC/AFEF memory.

## Confirmed event sources

- `OnSettlementOwnerChangedEvent` records the new ruler at the event's campaign day.
- `OnClanLeaderChangedEvent` visits only the affected clan's fiefs and records inherited rule.
- Native `OnSiegeAftermathAppliedEvent` records the selected applied aftermath. A pending GCCZ
  operation suppresses its internal native mercy notification; successful GCCZ finalization instead
  records actual loot, deaths and applied relief counters, with the actor identified.
- Successful GCCZ culture replacement records the changed culture.
- The existing local-policy post-commit hook records publication, renewal, abolition and expiry.
  It reads only the referenced record and its explicit source fiefs. Publication does not claim
  that projected economic effects have already occurred. `target_lost` supplies remaining fiefs,
  so that event is deliberately not interpreted as those fiefs losing the policy.

Commands, requests, precommit results and scene speech are not event evidence. The broader
GCCZ scene timeline remains session-only. National/NPC policy histories and unrelated gameplay
systems are not newly connected by this slice. No historical events are fabricated on upgrade.

## Demand and limits

Reading a town's encyclopedia or an already-eligible dialogue context may request generation.
Events only record bounded facts; they do not issue network requests or scan all towns.

- Empty automatic prose: request immediately, subject to network backpressure.
- Existing automatic prose: require dirty source revision, at least **7 campaign days** since
  the last successful generation, and either **3 accumulated source changes** or **1 campaign
  day** since the first pending change. Time alone does not generate more prose.
- Per-town network attempts are at least **1 wall-clock minute** apart. Failed/rejected attempts
  cool down for a minute after completion. At most **2** requests/completions are outstanding.
- Town generation uses the existing auxiliary gateway with an additional **384 output-token ceiling** (a lower user limit is retained), thinking disabled, and no thinking-control compatibility retry. Other auxiliary callers retain their existing settings. Character bounds on input are not an exact tokenizer budget.
- There is no background retry loop. Reopen/refresh the encyclopedia or use an eligible dialogue
  after cooldown to retry. A busy slot leaves pending facts intact for the next demand.
- At most **12** recent confirmed facts (480 characters each) accompany a tenure; prior prose is
  supplied as potentially stale context. The model must prioritize confirmed facts and distinguish
  former/current rulers. Unchanged observations reuse the existing snapshot.

The last good prose remains visible while pending, during generation, or after failure. Manual
prose is never automatically replaced by an event, culture/personality change or elapsed time.
The existing explicit developer regeneration action releases that manual lock while retaining
its text until a successful replacement; clearing text also enables future generation.

## Async/lifecycle acceptance

The existing auxiliary single-attempt API runs in the worker. It receives detached prompt data.
Workers queue completion DTOs only. The existing encyclopedia application tick drains at most
2 completions on the main thread, rejects old save/bridge generations, rereads live game state,
and commits only if ruler, tenure start and source revision still match and prose is not manual.
A new event, culture/personality change, manual edit, same-day repeated ownership change or load
invalidates an older result. New facts remain pending rather than being acknowledged by stale prose.
Reset invalidates old workers; it does not cancel an already-running underlying HTTP request. Retired physical requests still occupy the global two-worker budget until they finish.
No timer or per-frame town/hero/party scan was added.

## Save compatibility and rollback

The existing dictionary key `_gcczTownRuleMemoryRecordsBySettlement_v1` and initialization key
`_gcczTownRuleMemoryStorageInitialized_v1` are unchanged. Values now use codec **v3** with
per-tenure revision, generated revision, last generated day, first pending day and bounded facts.
The existing `CampaignSaveChunkHelper` flattens/restores the dictionary with chunks of at most 12000 UTF-8 bytes; unchunked legacy values remain readable. The reader accepts **v1** and **v2**, preserving prose/manual flags and initializing new metadata.
Old unobserved tenures keep the existing minimum-duration fallback; precise ownership dates are
recorded only for events observed after installation. Malformed records are rejected independently.

Code rollback is a focused inverse commit. An older binary cannot read v3 values: use a save
from before upgrading when rolling back, or implement an explicit v3-to-v2 export first. Do not
rewrite or delete live player saves to perform source rollback.

## Verification

`dotnet run --project tests/modules/AnimusForge.SiegeAftermathIntervention/TownRuleMemory.Tests/TownRuleMemory.Tests.csproj -c Release`

The harness links the production core, generation bridge, runtime bridge, ruler adapter and event
listeners. It uses a controllable auxiliary transport and value-only Bannerlord fixtures, covering
revision/cadence behavior, stale/manual acceptance, bounds, v1/v2/v3 migration, event dates,
registered listener behavior, and host save/load including full Chinese multi-tenure chunks. `verify_auxiliary_budget.ps1` extracts the actual auxiliary methods and validates budget/config behavior against a gateway stub; the existing configured-gateway HTTP replay separately verifies 384 tokens, disabled thinking and exactly one failed request. It is not a live game or real provider test.
Use the unchanged unified build script for both API lines plus Bootstrap. Current evidence,
source coordinates and rollback are in the main ledger's `town-memory-refresh-20261001` entry.
